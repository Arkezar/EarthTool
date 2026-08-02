using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EarthTool.MSH.Tests;

internal static class OfficialCorpusQualification
{
  private const string CorpusEnvironmentVariable = "EARTHTOOL_OFFICIAL_MSH_CORPUS";
  private const string EventEnvironmentVariable = "EARTHTOOL_OFFICIAL_MSH_EVIDENCE_EVENT";
  private const string ProgressEnvironmentVariable = "EARTHTOOL_OFFICIAL_MSH_PROGRESS_EVENT";

  internal static async Task RunAsync()
  {
    var corpusRoot = Environment.GetEnvironmentVariable(CorpusEnvironmentVariable);
    if (string.IsNullOrWhiteSpace(corpusRoot))
    {
      return;
    }
    var eventPath = Environment.GetEnvironmentVariable(EventEnvironmentVariable);
    string.IsNullOrWhiteSpace(eventPath).Should().BeFalse(
      "official corpus qualification requires a private aggregate event destination");

    await RunAsync(corpusRoot, eventPath!);
  }

  internal static async Task RunAsync(string corpusRoot, string eventPath)
  {
    var runner = new Runner(
      corpusRoot,
      Environment.GetEnvironmentVariable(ProgressEnvironmentVariable));
    await runner.RunAsync();
    await runner.WriteSummaryAsync(eventPath);

    runner.FailureCount.Should().Be(0,
      $"official corpus qualification failed: {runner.FailureSummary}");
  }

  internal static string ComputeCorpusFingerprint(IEnumerable<byte[]> assets)
  {
    return ComputeCorpusFingerprint(assets.Select(bytes =>
      new ContentFingerprint(bytes.LongLength, SHA256.HashData(bytes))));
  }

  internal static string ComputeSemanticDigest(MeshAsset asset)
  {
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
    writer.Write((int)asset.Kind);
    writer.Write(asset.ArchiveFraming.Declaration);
    writer.Write(asset.ArchiveFraming.ArchiveType.HasValue);
    if (asset.ArchiveFraming.ArchiveType.HasValue)
    {
      writer.Write(asset.ArchiveFraming.ArchiveType.Value);
    }
    writer.Write(asset.ArchiveFraming.CreationGuid.HasValue);
    if (asset.ArchiveFraming.CreationGuid.HasValue)
    {
      writer.Write(asset.ArchiveFraming.CreationGuid.Value.ToByteArray());
    }
    WriteBytes(writer, asset.CommonBaseHeader.SerializedRepresentation);
    WriteBytes(writer, asset.RootTrailingBytes);
    asset.Match(
      staticAsset => WriteStatic(writer, staticAsset),
      dynamicAsset => WriteDynamicObject(writer, dynamicAsset.RootDynamicObject));
    writer.Flush();
    return Convert.ToHexString(SHA256.HashData(stream.GetBuffer().AsSpan(0, (int)stream.Length)))
      .ToLowerInvariant();
  }

  private static string ComputeCorpusFingerprint(IEnumerable<ContentFingerprint> assets)
  {
    var ordered = assets
      .OrderBy(asset => Convert.ToHexString(asset.Digest), StringComparer.Ordinal)
      .ThenBy(asset => asset.Length)
      .ToArray();
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
    writer.Write(Encoding.ASCII.GetBytes("earthtool.msh.corpus.sha256-content-multiset.v1\0"));
    writer.Write(ordered.Length);
    foreach (var asset in ordered)
    {
      writer.Write(asset.Length);
      writer.Write(asset.Digest);
    }
    writer.Flush();
    return Convert.ToHexString(SHA256.HashData(stream.GetBuffer().AsSpan(0, (int)stream.Length)))
      .ToLowerInvariant();
  }

  private static void WriteStatic(BinaryWriter writer, StaticMeshAsset asset)
  {
    writer.Write(asset.StoredTrailingHierarchyUnwindCount);
    writer.Write(asset.ExpectedTrailingHierarchyUnwindCount);
    writer.Write(asset.StaticRenderObjectSequence.Count);
    foreach (var item in asset.StaticRenderObjectSequence)
    {
      writer.Write(item.LocalId);
      writer.Write(item.SourceObjectId.Value);
      writer.Write(item.RenderVertices.Count);
      foreach (var vertex in item.RenderVertices)
      {
        WriteVector3(writer, vertex.Position);
        WriteVector3(writer, vertex.Normal);
        writer.Write(vertex.TextureCoordinate.X);
        writer.Write(vertex.TextureCoordinate.Y);
        writer.Write(vertex.ReservedTextureComponent);
        writer.Write(vertex.NormalSharingIndex);
        writer.Write(vertex.PositionSharingIndex);
      }
      writer.Write(item.Triangles.Count);
      foreach (var triangle in item.Triangles)
      {
        writer.Write(triangle.Vertex0);
        writer.Write(triangle.Vertex1);
        writer.Write(triangle.Vertex2);
        writer.Write(triangle.TriangleRenderPassFlags);
      }
      writer.Write(item.VertexBlockCount);
      WriteBytes(writer, item.VertexBlockPadding);
      writer.Write(item.ObjectFlags);
      WriteBytes(writer, item.TexturePathBytes);
      WriteVectors(writer, item.AnimationTracks.ScaleFrames);
      WriteVectors(writer, item.AnimationTracks.TranslationFrames);
      writer.Write(item.AnimationTracks.Matrices.Count);
      foreach (var matrix in item.AnimationTracks.Matrices)
      {
        writer.Write(matrix.M11);
        writer.Write(matrix.M12);
        writer.Write(matrix.M13);
        writer.Write(matrix.M14);
        writer.Write(matrix.M21);
        writer.Write(matrix.M22);
        writer.Write(matrix.M23);
        writer.Write(matrix.M24);
        writer.Write(matrix.M31);
        writer.Write(matrix.M32);
        writer.Write(matrix.M33);
        writer.Write(matrix.M34);
        writer.Write(matrix.M41);
        writer.Write(matrix.M42);
        writer.Write(matrix.M43);
        writer.Write(matrix.M44);
      }
      writer.Write(item.AnimationClassValue);
      WriteVector3(writer, item.Pivot);
      writer.Write(item.BarrelMaximumAngle);
      writer.Write(item.NextRecordMarker);
    }
    WriteSourceObject(writer, asset.RootSourceObject);
  }

  private static void WriteSourceObject(BinaryWriter writer, StaticSourceObject source)
  {
    writer.Write(source.Id.Value);
    writer.Write(source.StaticRenderObjectIds.Count);
    foreach (var id in source.StaticRenderObjectIds)
    {
      writer.Write(id.Value);
    }
    writer.Write(source.Children.Count);
    foreach (var child in source.Children)
    {
      WriteSourceObject(writer, child);
    }
  }

  private static void WriteDynamicObject(BinaryWriter writer, DynamicObject item)
  {
    WriteBytes(writer, item.CommonBaseHeader.SerializedRepresentation);
    WriteBytes(writer, item.Extension.SerializedRepresentation);
    WriteBytes(writer, item.Extension.MeshNameBytes);
    WriteBytes(writer, item.Extension.TexturePathBytes);
    writer.Write(item.Children.Count);
    foreach (var child in item.Children)
    {
      WriteDynamicObject(writer, child);
    }
  }

  private static void WriteVectors(BinaryWriter writer, IReadOnlyList<System.Numerics.Vector3> values)
  {
    writer.Write(values.Count);
    foreach (var value in values)
    {
      WriteVector3(writer, value);
    }
  }

  private static void WriteVector3(BinaryWriter writer, System.Numerics.Vector3 value)
  {
    writer.Write(value.X);
    writer.Write(value.Y);
    writer.Write(value.Z);
  }

  private static void WriteBytes(BinaryWriter writer, IReadOnlyList<byte> bytes)
  {
    writer.Write(bytes.Count);
    for (var index = 0; index < bytes.Count; index++)
    {
      writer.Write(bytes[index]);
    }
  }

  private sealed class Runner
  {
    private readonly string _corpusRoot;
    private readonly string? _progressPath;
    private readonly MshOperationProfile _mshProfile = MshOperationProfile.Default;
    private readonly GltfOperationProfile _gltfProfile = GltfOperationProfile.Default;
    private readonly MshReader _reader = new();
    private readonly MshValidator _validator = new();
    private readonly MshWriter _writer = new();
    private readonly GltfInterchange _interchange = new();
    private readonly Dictionary<string, OperationCounts> _operations = new(StringComparer.Ordinal);
    private readonly Dictionary<DiagnosticKey, int> _diagnostics = [];
    private readonly Dictionary<string, int> _failures = new(StringComparer.Ordinal);
    private readonly List<ContentFingerprint> _contentFingerprints = [];
    private readonly ValidatorAggregate _khronos = new();
    private long _inputBytes;
    private long _canonicalMshBytes;
    private long _glbBytes;
    private long _gltfManifestBytes;
    private long _gltfSidecarBytes;
    private long _unchangedImportedMshBytes;
    private long _cliPackageBytes;
    private long _cliImportedMshBytes;
    private int _assetCount;
    private int _staticCount;
    private int _dynamicCount;
    private int _discoveredMshFiles;
    private int _excludedNonFramedOrUnsupported;
    private int _excludedByProfile;

    internal int FailureCount => _failures.Values.Sum();
    internal IEnumerable<string> FailureCategories => _failures.Keys.Order(StringComparer.Ordinal);
    internal string FailureSummary => string.Join(", ", FailureCategories)
      + _khronos.DescribeIssues();

    internal Runner(string corpusRoot, string? progressPath)
    {
      _corpusRoot = corpusRoot;
      _progressPath = progressPath;
    }

    internal async Task RunAsync()
    {
      string[] files;
      try
      {
        var discovered = Directory.EnumerateFiles(_corpusRoot, "*", SearchOption.AllDirectories)
          .Where(file => string.Equals(Path.GetExtension(file), ".msh", StringComparison.OrdinalIgnoreCase))
          .OrderBy(file => file, StringComparer.Ordinal)
          .ToArray();
        _discoveredMshFiles = discovered.Length;
        files = discovered.Where(file =>
        {
          if (new FileInfo(file).Length > _mshProfile.MaxInputBytes)
          {
            _excludedByProfile++;
            return false;
          }
          if (!IsFramedVersionOne(file))
          {
            _excludedNonFramedOrUnsupported++;
            return false;
          }
          return true;
        }).ToArray();
      }
      catch
      {
        Fail("corpus-discovery-failure");
        await WriteProgressAsync(0);
        return;
      }
      _assetCount = files.Length;
      await WriteProgressAsync(0);
      if (_assetCount == 0)
      {
        Fail("empty-corpus");
        await WriteProgressAsync(0);
        return;
      }

      var temporaryRoot = Path.Combine(
        Path.GetTempPath(),
        "earthtool-official-corpus-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(temporaryRoot);
      await using var khronos = await KhronosValidatorServer.StartAsync();
      try
      {
        for (var index = 0; index < files.Length; index++)
        {
          await QualifyAssetAsync(files[index], index, temporaryRoot, khronos);
          await WriteProgressAsync(index + 1);
        }
      }
      finally
      {
        try
        {
          Directory.Delete(temporaryRoot, recursive: true);
        }
        catch
        {
          Fail("cleanup-failure");
        }
      }
      await WriteProgressAsync(_assetCount);
    }

    internal async Task WriteSummaryAsync(string eventPath)
    {
      var summary = new
      {
        format = "earthtool.official-msh-corpus-event",
        version = 1,
        corpus = new
        {
          fingerprintAlgorithm = "sha256-content-multiset-v1",
          fingerprint = ComputeCorpusFingerprint(_contentFingerprints),
          discoveredMshFiles = _discoveredMshFiles,
          excludedNonFramedOrUnsupported = _excludedNonFramedOrUnsupported,
          excludedByProfile = _excludedByProfile,
          assets = _assetCount,
          staticAssets = _staticCount,
          dynamicAssets = _dynamicCount,
          inputBytes = _inputBytes
        },
        operations = _operations
          .OrderBy(pair => pair.Key, StringComparer.Ordinal)
          .Select(pair => new
          {
            stage = pair.Key,
            attempted = pair.Value.Attempted,
            passed = pair.Value.Passed,
            failed = pair.Value.Failed
          }),
        bytes = new
        {
          canonicalMsh = _canonicalMshBytes,
          glb = _glbBytes,
          gltfManifest = _gltfManifestBytes,
          gltfSidecars = _gltfSidecarBytes,
          unchangedImportedMsh = _unchangedImportedMshBytes,
          cliPackages = _cliPackageBytes,
          cliImportedMsh = _cliImportedMshBytes
        },
        diagnostics = _diagnostics
          .OrderBy(pair => pair.Key.Stage, StringComparer.Ordinal)
          .ThenBy(pair => pair.Key.Code, StringComparer.Ordinal)
          .ThenBy(pair => pair.Key.EventId)
          .ThenBy(pair => pair.Key.Severity)
          .Select(pair => new
          {
            stage = pair.Key.Stage,
            code = pair.Key.Code,
            eventId = pair.Key.EventId,
            severity = pair.Key.Severity.ToString(),
            count = pair.Value
          }),
        validators = new
        {
          khronos = _khronos.CreateSummary()
        },
        failures = new
        {
          total = FailureCount,
          categories = _failures
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new { category = pair.Key, count = pair.Value })
        },
        profiles = new
        {
          msh = CreateMshProfileSummary(_mshProfile),
          gltf = CreateGltfProfileSummary(_gltfProfile)
        }
      };
      var directory = Path.GetDirectoryName(eventPath);
      if (!string.IsNullOrEmpty(directory))
      {
        Directory.CreateDirectory(directory);
      }
      await File.WriteAllTextAsync(
        eventPath,
        JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    private async Task WriteProgressAsync(int completed)
    {
      if (string.IsNullOrWhiteSpace(_progressPath))
      {
        return;
      }
      await File.WriteAllTextAsync(
        _progressPath,
        JsonSerializer.Serialize(new
        {
          completed,
          total = _assetCount,
          staticAssets = _staticCount,
          dynamicAssets = _dynamicCount,
          failures = FailureCount
        }));
    }

    private async Task QualifyAssetAsync(
      string file,
      int index,
      string temporaryRoot,
      KhronosValidatorServer khronos)
    {
      byte[] source;
      try
      {
        source = await File.ReadAllBytesAsync(file);
      }
      catch
      {
        Fail("corpus-read-failure");
        FailBlockedBinaryStages();
        return;
      }
      _inputBytes += source.LongLength;
      var sourceDigest = SHA256.HashData(source);
      _contentFingerprints.Add(new ContentFingerprint(source.LongLength, sourceDigest));

      Begin("msh.read");
      OperationResult<MeshAsset> read;
      try
      {
        read = await _reader.ReadAsync(new MemoryStream(source), _mshProfile);
      }
      catch
      {
        CompleteFailure("msh.read", "unexpected-operation-failure");
        FailBlockedBinaryStages(after: "msh.read");
        return;
      }
      AddDiagnostics("msh.read", read.Diagnostics);
      if (!read.Succeeded)
      {
        CompleteFailure("msh.read", "msh-read-failure");
        FailBlockedBinaryStages(after: "msh.read");
        return;
      }
      CompleteSuccess("msh.read");
      var asset = read.Value!;
      if (asset is StaticMeshAsset)
      {
        _staticCount++;
      }
      else
      {
        _dynamicCount++;
      }

      Begin("msh.validate");
      var validation = await _validator.ValidateAsync(asset, _mshProfile);
      AddDiagnostics("msh.validate", validation.Diagnostics);
      CompleteFromResult("msh.validate", validation, "msh-validation-failure");

      Begin("msh.write");
      var canonical = new MemoryStream();
      var write = await _writer.WriteAsync(asset, canonical, _mshProfile);
      AddDiagnostics("msh.write", write.Diagnostics);
      byte[]? canonicalBytes = null;
      if (write.Succeeded)
      {
        canonicalBytes = canonical.ToArray();
        _canonicalMshBytes += canonicalBytes.LongLength;
        if (canonicalBytes.AsSpan().SequenceEqual(source))
        {
          CompleteSuccess("msh.write");
        }
        else
        {
          CompleteFailure("msh.write", "msh-byte-divergence");
        }
      }
      else
      {
        CompleteFailure("msh.write", "msh-write-failure");
      }

      MeshAsset? rereadAsset = null;
      Begin("msh.semantic-equivalence");
      if (canonicalBytes is null)
      {
        CompleteFailure("msh.semantic-equivalence", "blocked-oracle");
      }
      else
      {
        var reread = await _reader.ReadAsync(new MemoryStream(canonicalBytes), _mshProfile);
        AddDiagnostics("msh.semantic-equivalence", reread.Diagnostics);
        rereadAsset = reread.Value;
        if (reread.Succeeded
          && ComputeSemanticDigest(asset) == ComputeSemanticDigest(reread.Value!))
        {
          CompleteSuccess("msh.semantic-equivalence");
        }
        else
        {
          CompleteFailure("msh.semantic-equivalence", "semantic-divergence");
        }
      }

      Begin("msh.canonical-idempotence");
      if (canonicalBytes is null || rereadAsset is null)
      {
        CompleteFailure("msh.canonical-idempotence", "blocked-oracle");
      }
      else
      {
        var second = new MemoryStream();
        var secondWrite = await _writer.WriteAsync(rereadAsset, second, _mshProfile);
        AddDiagnostics("msh.canonical-idempotence", secondWrite.Diagnostics);
        if (secondWrite.Succeeded && second.ToArray().AsSpan().SequenceEqual(canonicalBytes))
        {
          CompleteSuccess("msh.canonical-idempotence");
        }
        else
        {
          CompleteFailure("msh.canonical-idempotence", "canonical-idempotence-failure");
        }
      }

      if (asset is StaticMeshAsset staticAsset && canonicalBytes is not null)
      {
        await QualifyStaticAssetAsync(
          staticAsset,
          canonicalBytes,
          sourceDigest,
          index,
          temporaryRoot,
          khronos);
      }
    }

    private async Task QualifyStaticAssetAsync(
      StaticMeshAsset asset,
      byte[] canonicalBytes,
      byte[] sourceDigest,
      int index,
      string temporaryRoot,
      KhronosValidatorServer khronos)
    {
      var directory = Path.Combine(temporaryRoot, $"asset-{index:D4}");
      Directory.CreateDirectory(directory);
      try
      {
        var options = new GltfExportOptions(
          CreateVersion4Guid(sourceDigest, "lineage"),
          CreateVersion4Guid(sourceDigest, "document"),
          [_corpusRoot]);
        await QualifyGlbAsync(asset, canonicalBytes, options, directory, khronos);
        await QualifySeparateGltfAsync(asset, canonicalBytes, options, directory, khronos);
        await QualifyCliPackageAsync("glb", canonicalBytes, directory, khronos);
        await QualifyCliPackageAsync("gltf", canonicalBytes, directory, khronos);
      }
      catch
      {
        Fail("unexpected-static-oracle-failure");
      }
      finally
      {
        try
        {
          Directory.Delete(directory, recursive: true);
        }
        catch
        {
          Fail("cleanup-failure");
        }
      }
    }

    private async Task QualifyGlbAsync(
      StaticMeshAsset asset,
      byte[] canonicalBytes,
      GltfExportOptions options,
      string directory,
      KhronosValidatorServer khronos)
    {
      Begin("glb.export");
      var stream = new MemoryStream();
      var export = await _interchange.ExportGlbAsync(asset, stream, options, _gltfProfile);
      AddDiagnostics("glb.export", export.Diagnostics);
      if (!export.Succeeded)
      {
        CompleteFailure("glb.export", "glb-export-failure");
        FailBlockedPackageStages("glb");
        return;
      }
      CompleteSuccess("glb.export");
      var bytes = stream.ToArray();
      _glbBytes += bytes.LongLength;
      var packagePath = Path.Combine(directory, "package.glb");
      await File.WriteAllBytesAsync(packagePath, bytes);

      Begin("glb.sharp-gltf-validate");
      var sharpValidation = await _interchange.ValidateGlbAsync(
        new MemoryStream(bytes),
        _gltfProfile);
      AddDiagnostics("glb.sharp-gltf-validate", sharpValidation.Diagnostics);
      CompleteFromResult(
        "glb.sharp-gltf-validate",
        sharpValidation,
        "sharp-gltf-validation-failure");

      await ValidateKhronosAsync("glb.khronos-validate", packagePath, khronos);

      Begin("glb.unchanged-import");
      var import = await _interchange.ImportEditGlbAsync(
        new MemoryStream(bytes),
        export.Value!.Baseline,
        _gltfProfile);
      AddDiagnostics("glb.unchanged-import", import.Diagnostics);
      if (!import.Succeeded || HasChangedPreservation(import.Value!))
      {
        CompleteFailure("glb.unchanged-import", "unchanged-import-failure");
        FailStage("glb.canonical-baseline", "blocked-oracle");
        return;
      }
      CompleteSuccess("glb.unchanged-import");
      await ValidateImportedBaselineAsync(
        "glb.canonical-baseline",
        import.Value!.Asset,
        canonicalBytes);
    }

    private async Task QualifySeparateGltfAsync(
      StaticMeshAsset asset,
      byte[] canonicalBytes,
      GltfExportOptions options,
      string directory,
      KhronosValidatorServer khronos)
    {
      var packageDirectory = Path.Combine(directory, "separate");
      Directory.CreateDirectory(packageDirectory);
      var packagePath = Path.Combine(packageDirectory, "package.gltf");
      Begin("gltf.export");
      var export = await _interchange.ExportGltfFileAsync(
        asset,
        packagePath,
        options,
        _gltfProfile);
      AddDiagnostics("gltf.export", export.Diagnostics);
      if (!export.Succeeded)
      {
        CompleteFailure("gltf.export", "gltf-export-failure");
        FailBlockedPackageStages("gltf");
        return;
      }
      CompleteSuccess("gltf.export");
      _gltfManifestBytes += new FileInfo(packagePath).Length;
      _gltfSidecarBytes += Directory.EnumerateFiles(packageDirectory, "*", SearchOption.AllDirectories)
        .Where(path => !string.Equals(path, packagePath, StringComparison.Ordinal))
        .Sum(path => new FileInfo(path).Length);

      Begin("gltf.sharp-gltf-validate");
      var sharpValidation = await _interchange.ValidateGltfFileAsync(packagePath, _gltfProfile);
      AddDiagnostics("gltf.sharp-gltf-validate", sharpValidation.Diagnostics);
      CompleteFromResult(
        "gltf.sharp-gltf-validate",
        sharpValidation,
        "sharp-gltf-validation-failure");

      await ValidateKhronosAsync("gltf.khronos-validate", packagePath, khronos);

      Begin("gltf.unchanged-import");
      var import = await _interchange.ImportEditGltfFileAsync(
        packagePath,
        export.Value!.Baseline,
        _gltfProfile);
      AddDiagnostics("gltf.unchanged-import", import.Diagnostics);
      if (!import.Succeeded || HasChangedPreservation(import.Value!))
      {
        CompleteFailure("gltf.unchanged-import", "unchanged-import-failure");
        FailStage("gltf.canonical-baseline", "blocked-oracle");
        return;
      }
      CompleteSuccess("gltf.unchanged-import");
      await ValidateImportedBaselineAsync(
        "gltf.canonical-baseline",
        import.Value!.Asset,
        canonicalBytes);
    }

    private async Task ValidateKhronosAsync(
      string stage,
      string packagePath,
      KhronosValidatorServer khronos)
    {
      Begin(stage);
      var result = await khronos.ValidateAsync(packagePath);
      _khronos.Add(result);
      if (result.Passed
        && result.Errors == 0
        && result.Warnings == 0)
      {
        CompleteSuccess(stage);
      }
      else
      {
        CompleteFailure(stage, result.Failure ?? "khronos-validator-issue");
      }
    }

    private async Task ValidateImportedBaselineAsync(
      string stage,
      StaticMeshAsset asset,
      byte[] canonicalBytes)
    {
      Begin(stage);
      var stream = new MemoryStream();
      var result = await _writer.WriteAsync(asset, stream, _mshProfile);
      AddDiagnostics(stage, result.Diagnostics);
      var imported = stream.ToArray();
      _unchangedImportedMshBytes += imported.LongLength;
      if (result.Succeeded && imported.AsSpan().SequenceEqual(canonicalBytes))
      {
        CompleteSuccess(stage);
      }
      else
      {
        CompleteFailure(stage, "canonical-baseline-divergence");
      }
    }

    private async Task QualifyCliPackageAsync(
      string package,
      byte[] canonicalBytes,
      string directory,
      KhronosValidatorServer khronos)
    {
      var exportStage = $"{package}.cli-export";
      var sharpValidationStage = $"{package}.cli-sharp-gltf-validate";
      var khronosValidationStage = $"{package}.cli-khronos-validate";
      var importStage = $"{package}.cli-unchanged-import";
      Begin(exportStage);
      Begin(sharpValidationStage);
      Begin(importStage);
      var result = await OfficialCorpusCliOracle.RunAsync(
        canonicalBytes,
        package,
        directory,
        _corpusRoot);
      _cliPackageBytes += result.PackageBytes;
      _cliImportedMshBytes += result.ImportedMshBytes;
      AddDiagnostics(exportStage, result.ExportDiagnostics);
      AddDiagnostics(importStage, result.ImportDiagnostics);
      if (result.ExportSucceeded)
      {
        CompleteSuccess(exportStage);
      }
      else
      {
        CompleteFailure(exportStage, "cli-export-failure");
      }
      if (result.ExportSucceeded && result.PackagePath is not null)
      {
        OperationResult strictValidation;
        if (package == "glb")
        {
          strictValidation = await _interchange.ValidateGlbAsync(
            new MemoryStream(await File.ReadAllBytesAsync(result.PackagePath)),
            _gltfProfile);
        }
        else
        {
          strictValidation = await _interchange.ValidateGltfFileAsync(result.PackagePath, _gltfProfile);
        }
        AddDiagnostics(sharpValidationStage, strictValidation.Diagnostics);
        CompleteFromResult(
          sharpValidationStage,
          strictValidation,
          "cli-sharp-gltf-validation-failure");
        await ValidateKhronosAsync(khronosValidationStage, result.PackagePath, khronos);
      }
      else
      {
        CompleteFailure(sharpValidationStage, "blocked-oracle");
        FailStage(khronosValidationStage, "blocked-oracle");
      }
      if (result.ImportSucceeded)
      {
        CompleteSuccess(importStage);
      }
      else
      {
        CompleteFailure(importStage, result.ExportSucceeded
          ? "cli-unchanged-import-failure"
          : "blocked-oracle");
      }
    }

    private static bool HasChangedPreservation(GltfEditImportResult result)
    {
      return result.Preservation.Changes.Any(change =>
        change.Disposition != PreservationDisposition.Retained);
    }

    private static Guid CreateVersion4Guid(byte[] sourceDigest, string purpose)
    {
      var purposeBytes = Encoding.ASCII.GetBytes(purpose);
      var preimage = new byte[sourceDigest.Length + purposeBytes.Length];
      sourceDigest.CopyTo(preimage, 0);
      purposeBytes.CopyTo(preimage, sourceDigest.Length);
      var bytes = SHA256.HashData(preimage).AsSpan(0, 16).ToArray();
      bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40);
      bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
      return new Guid(bytes);
    }

    private static bool IsFramedVersionOne(string file)
    {
      Span<byte> prefix = stackalloc byte[36];
      using var stream = File.OpenRead(file);
      var length = 0;
      while (length < prefix.Length)
      {
        var read = stream.Read(prefix.Slice(length));
        if (read == 0)
        {
          break;
        }
        length += read;
      }
      if (length < sizeof(uint))
      {
        return false;
      }
      var declaration = BinaryPrimitives.ReadUInt32LittleEndian(prefix);
      if ((declaration & 0x00FFFFFF) != 0x00D0A1FF)
      {
        return false;
      }
      var baseOffset = sizeof(uint)
        + ((declaration & 0x10000000) != 0 ? sizeof(uint) : 0)
        + ((declaration & 0x20000000) != 0 ? 16 : 0);
      if (length < baseOffset + 12
        || !prefix.Slice(baseOffset, 4).SequenceEqual("MESH"u8))
      {
        return false;
      }
      return BinaryPrimitives.ReadUInt32LittleEndian(prefix.Slice(baseOffset + 4, 4)) == 1
        && BinaryPrimitives.ReadUInt32LittleEndian(prefix.Slice(baseOffset + 8, 4)) <= 1;
    }

    private void FailBlockedBinaryStages(string? after = null)
    {
      foreach (var stage in new[]
      {
        "msh.read",
        "msh.validate",
        "msh.write",
        "msh.semantic-equivalence",
        "msh.canonical-idempotence"
      }.SkipWhile(stage => after is not null && stage != after).Skip(after is null ? 0 : 1))
      {
        FailStage(stage, "blocked-oracle");
      }
    }

    private void FailBlockedPackageStages(string package)
    {
      foreach (var suffix in new[]
      {
        "sharp-gltf-validate",
        "khronos-validate",
        "unchanged-import",
        "canonical-baseline"
      })
      {
        FailStage($"{package}.{suffix}", "blocked-oracle");
      }
    }

    private void FailStage(string stage, string category)
    {
      Begin(stage);
      CompleteFailure(stage, category);
    }

    private void Begin(string stage)
    {
      GetOperation(stage).Attempted++;
    }

    private void CompleteSuccess(string stage)
    {
      GetOperation(stage).Passed++;
    }

    private void CompleteFailure(string stage, string category)
    {
      GetOperation(stage).Failed++;
      Fail(category);
    }

    private void CompleteFromResult(string stage, OperationResult result, string category)
    {
      if (result.Succeeded)
      {
        CompleteSuccess(stage);
      }
      else
      {
        CompleteFailure(stage, category);
      }
    }

    private OperationCounts GetOperation(string stage)
    {
      if (!_operations.TryGetValue(stage, out var counts))
      {
        counts = new OperationCounts();
        _operations.Add(stage, counts);
      }
      return counts;
    }

    private void AddDiagnostics(string stage, IEnumerable<OperationDiagnostic> diagnostics)
    {
      foreach (var diagnostic in diagnostics)
      {
        var key = new DiagnosticKey(
          stage,
          diagnostic.Code,
          diagnostic.EventId,
          diagnostic.Severity);
        _diagnostics[key] = _diagnostics.GetValueOrDefault(key) + 1;
      }
    }

    private void AddDiagnostics(string stage, IEnumerable<CliDiagnostic> diagnostics)
    {
      foreach (var diagnostic in diagnostics)
      {
        var key = new DiagnosticKey(
          stage,
          diagnostic.Code,
          diagnostic.EventId,
          diagnostic.Severity);
        _diagnostics[key] = _diagnostics.GetValueOrDefault(key) + 1;
      }
    }

    private void Fail(string category)
    {
      _failures[category] = _failures.GetValueOrDefault(category) + 1;
    }
  }

  private sealed class KhronosValidatorServer : IAsyncDisposable
  {
    private readonly Process _process;

    private KhronosValidatorServer(Process process)
    {
      _process = process;
    }

    internal static Task<KhronosValidatorServer> StartAsync()
    {
      var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
      var startInfo = new ProcessStartInfo("node")
      {
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };
      startInfo.ArgumentList.Add(Path.Combine(root, "test-tools", "validate-glb.mjs"));
      startInfo.ArgumentList.Add("--server");
      startInfo.ArgumentList.Add("true");
      startInfo.ArgumentList.Add("--fail-on");
      startInfo.ArgumentList.Add("errors-and-warnings");
      startInfo.ArgumentList.Add("--summary-only");
      startInfo.ArgumentList.Add("true");
      var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("The private validator process could not start.");
      return Task.FromResult(new KhronosValidatorServer(process));
    }

    internal async Task<ValidatorResult> ValidateAsync(string packagePath)
    {
      using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
      try
      {
        await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new { path = packagePath }));
        await _process.StandardInput.FlushAsync(timeout.Token);
      }
      catch (Exception exception) when (exception is OperationCanceledException or IOException)
      {
        return ValidatorResult.ExecutionFailure();
      }
      string? line;
      try
      {
        line = await _process.StandardOutput.ReadLineAsync(timeout.Token);
      }
      catch (OperationCanceledException)
      {
        return ValidatorResult.ExecutionFailure();
      }
      if (line is null)
      {
        return ValidatorResult.ExecutionFailure();
      }
      try
      {
        return JsonSerializer.Deserialize<ValidatorResult>(
          line,
          new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
          ?? ValidatorResult.ExecutionFailure();
      }
      catch (JsonException)
      {
        return ValidatorResult.ExecutionFailure();
      }
    }

    public async ValueTask DisposeAsync()
    {
      try
      {
        _process.StandardInput.Close();
      }
      catch (IOException)
      {
        if (!_process.HasExited)
        {
          _process.Kill(entireProcessTree: true);
        }
      }
      using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
      try
      {
        await _process.WaitForExitAsync(timeout.Token);
      }
      catch (OperationCanceledException)
      {
        _process.Kill(entireProcessTree: true);
        await _process.WaitForExitAsync();
      }
      await _process.StandardError.ReadToEndAsync();
      _process.Dispose();
    }
  }

  private sealed class ValidatorAggregate
  {
    private readonly Dictionary<string, int> _codes = new(StringComparer.Ordinal);
    private string? _version;
    private int _packages;
    private int _errors;
    private int _warnings;
    private int _infos;
    private int _hints;

    internal void Add(ValidatorResult result)
    {
      _version ??= result.ValidatorVersion;
      _packages++;
      _errors += result.Errors;
      _warnings += result.Warnings;
      _infos += result.Infos;
      _hints += result.Hints;
      foreach (var code in result.Codes)
      {
        _codes[code.Code] = _codes.GetValueOrDefault(code.Code) + code.Count;
      }
    }

    internal object CreateSummary()
    {
      return new
      {
        validatorVersion = _version,
        packages = _packages,
        errors = _errors,
        warnings = _warnings,
        infos = _infos,
        hints = _hints,
        codes = _codes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
          .Select(pair => new { code = pair.Key, count = pair.Value })
      };
    }

    internal string DescribeIssues()
    {
      return _errors + _warnings + _infos + _hints == 0
        ? string.Empty
        : $"; validator counts={_errors}/{_warnings}/{_infos}/{_hints}, codes="
          + string.Join(",", _codes.Keys.Order(StringComparer.Ordinal));
    }
  }

  private sealed class ValidatorResult
  {
    public int Errors { get; init; }
    public int Warnings { get; init; }
    public int Infos { get; init; }
    public int Hints { get; init; }
    public List<ValidatorCode> Codes { get; init; } = [];
    public string? ValidatorVersion { get; init; }
    public bool Passed { get; init; }
    public string? Failure { get; init; }

    internal static ValidatorResult ExecutionFailure()
    {
      return new ValidatorResult { Failure = "validator-execution" };
    }
  }

  private sealed class ValidatorCode
  {
    public string Code { get; init; } = string.Empty;
    public int Count { get; init; }
  }

  private sealed class OperationCounts
  {
    internal int Attempted { get; set; }
    internal int Passed { get; set; }
    internal int Failed { get; set; }
  }

  private sealed record ContentFingerprint(long Length, byte[] Digest);

  private sealed record DiagnosticKey(
    string Stage,
    string Code,
    int EventId,
    DiagnosticSeverity Severity);

  private static object CreateMshProfileSummary(MshOperationProfile profile)
  {
    return new
    {
      profile.MaxInputBytes,
      profile.MaxOutputBytes,
      profile.MaxDiagnostics,
      profile.MaxRootTrailingBytes,
      profile.MaxDynamicDepth,
      profile.MaxDynamicObjects,
      profile.MaxDynamicChildrenPerObject,
      profile.MaxDynamicStringBytes,
      profile.MaxStaticRenderObjects,
      profile.MaxStaticVerticesPerObject,
      profile.MaxStaticTrianglesPerObject,
      profile.MaxStaticVertexBlocksPerObject,
      profile.MaxStaticAnimationFramesPerTrack,
      profile.MaxStaticTexturePathBytes,
      profile.MaxStaticHierarchyDepth
    };
  }

  private static object CreateGltfProfileSummary(GltfOperationProfile profile)
  {
    return new
    {
      profile.MaxInputBytes,
      profile.MaxOutputBytes,
      profile.MaxMetadataBytes,
      profile.MaxJsonDepth,
      profile.MaxActiveRenderVertices,
      profile.MaxNodes,
      profile.MaxHierarchyDepth,
      profile.MaxTextureBytes,
      profile.MaxPreviewPixels,
      profile.MaxTextureSearchRoots,
      profile.MaxTextureDirectoryEntries,
      profile.MaxTotalMetadataBytes,
      profile.MaxMetadataEnvelopes,
      profile.MaxMetadataElements,
      profile.MaxUnknownMetadataMembers,
      profile.MaxMetadataGuards,
      profile.MaxMetadataConflicts
    };
  }
}
