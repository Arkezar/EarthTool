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
using System.Threading.Channels;

namespace EarthTool.MSH.Tests;

internal static class OfficialCorpusQualification
{
  private const string CorpusEnvironmentVariable = "EARTHTOOL_OFFICIAL_MSH_CORPUS";
  private const string EventEnvironmentVariable = "EARTHTOOL_OFFICIAL_MSH_EVIDENCE_EVENT";
  private const string ProgressEnvironmentVariable = "EARTHTOOL_OFFICIAL_MSH_PROGRESS_EVENT";
  private const string ProfileEnvironmentVariable = "EARTHTOOL_OFFICIAL_MSH_PROFILE_EVENT";
  private const string WorkersEnvironmentVariable = "EARTHTOOL_OFFICIAL_MSH_WORKERS";

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

    var workerValue = Environment.GetEnvironmentVariable(WorkersEnvironmentVariable);
    var parsedWorkers = int.TryParse(workerValue, out var configuredWorkers);
    var workerCount = string.IsNullOrWhiteSpace(workerValue)
      ? Math.Max(1, Environment.ProcessorCount / 2)
      : configuredWorkers;
    (string.IsNullOrWhiteSpace(workerValue) || parsedWorkers).Should().BeTrue(
      "official corpus worker count must be an integer");
    workerCount.Should().BeGreaterThan(0, "official corpus worker count must be positive");

    await RunAsync(
      corpusRoot,
      eventPath!,
      workerCount,
      Environment.GetEnvironmentVariable(ProfileEnvironmentVariable));
  }

  internal static async Task RunAsync(
    string corpusRoot,
    string eventPath,
    int workerCount = 1,
    string? profilePath = null)
  {
    workerCount.Should().BeGreaterThan(0);
    var runner = new Runner(
      corpusRoot,
      Environment.GetEnvironmentVariable(ProgressEnvironmentVariable),
      workerCount,
      profilePath);
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

  internal static string ComputeSemanticDigest(MeshAsset asset, bool includeCreationGuid = true)
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
    var creationGuid = includeCreationGuid ? asset.ArchiveFraming.CreationGuid : null;
    writer.Write(creationGuid.HasValue);
    if (creationGuid.HasValue)
    {
      writer.Write(creationGuid.Value.ToByteArray());
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
    var renderObjectOrdinals = asset.StaticRenderObjectSequence
      .Select((renderObject, ordinal) => (renderObject, ordinal))
      .ToDictionary(item => item.renderObject, item => item.ordinal);
    WriteSourceObject(writer, asset.RootSourceObject, renderObjectOrdinals);
  }

  private static void WriteSourceObject(
    BinaryWriter writer,
    StaticSourceObject source,
    IReadOnlyDictionary<StaticRenderObject, int> renderObjectOrdinals
  )
  {
    writer.Write(source.StaticRenderObjects.Count);
    foreach (var renderObject in source.StaticRenderObjects)
    {
      writer.Write(renderObjectOrdinals[renderObject]);
    }
    writer.Write(source.Children.Count);
    foreach (var child in source.Children)
    {
      WriteSourceObject(writer, child, renderObjectOrdinals);
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
    private readonly int _workerCount;
    private readonly QualificationProfiler _profiler;
    private readonly MshOperationProfile _mshProfile = MshOperationProfile.Default;
    private readonly GltfOperationProfile _gltfProfile = GltfOperationProfile.Default;
    private readonly Dictionary<string, OperationCounts> _operations = new(StringComparer.Ordinal);
    private readonly Dictionary<DiagnosticKey, int> _diagnostics = [];
    private readonly Dictionary<string, int> _failures = new(StringComparer.Ordinal);
    private readonly List<ContentFingerprint> _contentFingerprints = [];
    private readonly ValidatorAggregate _khronos = new();
    private readonly DynamicCoverage _dynamicCoverage = new();
    private CliBatchOracleResult? _exportAllMeshes;
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

    internal Runner(
      string corpusRoot,
      string? progressPath,
      int workerCount,
      string? profilePath)
    {
      _corpusRoot = corpusRoot;
      _progressPath = progressPath;
      _workerCount = workerCount;
      _profiler = new QualificationProfiler(profilePath, workerCount);
    }

    internal async Task RunAsync()
    {
      var wallClockStarted = Stopwatch.GetTimestamp();
      string[] files;
      try
      {
        using (_profiler.Measure("corpus.discovery"))
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
      }
      catch
      {
        Fail("corpus-discovery-failure");
        await WriteProgressAsync(0);
        await _profiler.WriteAsync(Stopwatch.GetElapsedTime(wallClockStarted));
        return;
      }
      _assetCount = files.Length;
      await WriteProgressAsync(0);
      if (_assetCount == 0)
      {
        Fail("empty-corpus");
        await WriteProgressAsync(0);
        await _profiler.WriteAsync(Stopwatch.GetElapsedTime(wallClockStarted));
        return;
      }

      var temporaryRoot = Path.Combine(
        Path.GetTempPath(),
        "earthtool-official-corpus-" + Guid.NewGuid().ToString("N"));
      using (_profiler.Measure("io.temporary-root-create"))
      {
        Directory.CreateDirectory(temporaryRoot);
      }
      try
      {
        await QualifyAssetsAsync(files, temporaryRoot);
        _exportAllMeshes = await OfficialCorpusCliOracle.RunExportAllMeshesAsync(
          _corpusRoot,
          temporaryRoot);
        if (!_exportAllMeshes.Succeeded
          || _exportAllMeshes.FailedOperations != 0
          || _exportAllMeshes.CancelledOperations != 0
          || _exportAllMeshes.UnsupportedDomainDiagnostics != 0
          || _exportAllMeshes.SuccessfulOperations != _exportAllMeshes.Assets
          || _exportAllMeshes.OutputFiles != _exportAllMeshes.Assets)
        {
          Fail("export-all-meshes-failure");
        }
      }
      finally
      {
        try
        {
          using (_profiler.Measure("io.temporary-root-delete"))
          {
            Directory.Delete(temporaryRoot, recursive: true);
          }
        }
        catch
        {
          Fail("cleanup-failure");
        }
      }
      await WriteProgressAsync(_assetCount);
      await _profiler.WriteAsync(Stopwatch.GetElapsedTime(wallClockStarted));
    }

    private async Task QualifyAssetsAsync(string[] files, string temporaryRoot)
    {
      var workers = Math.Min(_workerCount, files.Length);
      var jobs = Channel.CreateBounded<int>(new BoundedChannelOptions(workers)
      {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = false,
        SingleWriter = true
      });
      var completions = Channel.CreateUnbounded<AssetResult>(new UnboundedChannelOptions
      {
        SingleReader = true,
        SingleWriter = false
      });
      var results = new AssetResult[files.Length];
      var workerTasks = Enumerable.Range(0, workers)
        .Select(_ => RunWorkerAsync(files, temporaryRoot, jobs.Reader, completions.Writer, results))
        .ToArray();
      var progressTask = ReportProgressAsync(completions.Reader);

      for (var index = 0; index < files.Length; index++)
      {
        await jobs.Writer.WriteAsync(index);
      }
      jobs.Writer.Complete();
      try
      {
        await Task.WhenAll(workerTasks);
      }
      finally
      {
        completions.Writer.Complete();
      }
      await progressTask;

      foreach (var result in results)
      {
        Merge(result);
      }
    }

    private async Task RunWorkerAsync(
      string[] files,
      string temporaryRoot,
      ChannelReader<int> jobs,
      ChannelWriter<AssetResult> completions,
      AssetResult[] results)
    {
      await using var worker = new WorkerContext(_profiler);
      await foreach (var index in jobs.ReadAllAsync())
      {
        AssetResult result;
        try
        {
          result = await QualifyAssetAsync(files[index], index, temporaryRoot, worker);
        }
        catch
        {
          result = new AssetResult(_profiler);
          result.Fail("unexpected-asset-failure");
        }
        results[index] = result;
        await completions.WriteAsync(result);
      }
    }

    private async Task ReportProgressAsync(ChannelReader<AssetResult> completions)
    {
      var completed = 0;
      var staticAssets = 0;
      var dynamicAssets = 0;
      var failures = 0;
      await foreach (var result in completions.ReadAllAsync())
      {
        completed++;
        staticAssets += result.StaticCount;
        dynamicAssets += result.DynamicCount;
        failures += result.FailureCount;
        await WriteProgressAsync(completed, staticAssets, dynamicAssets, failures);
      }
    }

    internal async Task WriteSummaryAsync(string eventPath)
    {
      var summary = new
      {
        format = "earthtool.official-msh-corpus-event",
        version = 2,
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
        dynamicCoverage = _dynamicCoverage.CreateSummary(),
        exportAllMeshes = _exportAllMeshes is null ? null : new
        {
          assets = _exportAllMeshes.Assets,
          staticAssets = _exportAllMeshes.StaticAssets,
          dynamicAssets = _exportAllMeshes.DynamicAssets,
          succeeded = _exportAllMeshes.SuccessfulOperations,
          failed = _exportAllMeshes.FailedOperations,
          cancelled = _exportAllMeshes.CancelledOperations,
          unsupportedDomainDiagnostics = _exportAllMeshes.UnsupportedDomainDiagnostics,
          outputFiles = _exportAllMeshes.OutputFiles
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

    private async Task WriteProgressAsync(
      int completed,
      int? staticAssets = null,
      int? dynamicAssets = null,
      int? failures = null)
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
          staticAssets = staticAssets ?? _staticCount,
          dynamicAssets = dynamicAssets ?? _dynamicCount,
          failures = failures ?? FailureCount
        }));
    }

    private async Task<AssetResult> QualifyAssetAsync(
      string file,
      int index,
      string temporaryRoot,
      WorkerContext worker)
    {
      var result = new AssetResult(_profiler);
      byte[] source;
      try
      {
        using (_profiler.Measure("io.source-read"))
        {
          source = await File.ReadAllBytesAsync(file);
        }
      }
      catch
      {
        result.Fail("corpus-read-failure");
        result.FailBlockedBinaryStages();
        return result;
      }
      result.InputBytes += source.LongLength;
      var sourceDigest = SHA256.HashData(source);
      result.ContentFingerprint = new ContentFingerprint(source.LongLength, sourceDigest);

      result.Begin("msh.read");
      OperationResult<MeshAsset> read;
      try
      {
        read = await worker.Reader.ReadAsync(new MemoryStream(source), _mshProfile);
      }
      catch
      {
        result.CompleteFailure("msh.read", "unexpected-operation-failure");
        result.FailBlockedBinaryStages(after: "msh.read");
        return result;
      }
      result.AddDiagnostics("msh.read", read.Diagnostics);
      if (!read.Succeeded)
      {
        result.CompleteFailure("msh.read", "msh-read-failure");
        result.FailBlockedBinaryStages(after: "msh.read");
        return result;
      }
      result.CompleteSuccess("msh.read");
      var asset = read.Value!;
      if (asset is StaticMeshAsset)
      {
        result.StaticCount++;
      }
      else
      {
        result.DynamicCount++;
        result.DynamicCoverage.Add((DynamicMeshAsset)asset);
      }

      result.Begin("msh.validate");
      var validation = await worker.Validator.ValidateAsync(asset, _mshProfile);
      result.AddDiagnostics("msh.validate", validation.Diagnostics);
      result.CompleteFromResult("msh.validate", validation, "msh-validation-failure");

      result.Begin("msh.write");
      var canonical = new MemoryStream();
      var write = await worker.Writer.WriteAsync(asset, canonical, _mshProfile);
      result.AddDiagnostics("msh.write", write.Diagnostics);
      byte[]? canonicalBytes = null;
      if (write.Succeeded)
      {
        canonicalBytes = canonical.ToArray();
        result.CanonicalMshBytes += canonicalBytes.LongLength;
        if (canonicalBytes.AsSpan().SequenceEqual(source))
        {
          result.CompleteSuccess("msh.write");
        }
        else
        {
          result.CompleteFailure("msh.write", "msh-byte-divergence");
        }
      }
      else
      {
        result.CompleteFailure("msh.write", "msh-write-failure");
      }

      MeshAsset? rereadAsset = null;
      result.Begin("msh.semantic-equivalence");
      if (canonicalBytes is null)
      {
        result.CompleteFailure("msh.semantic-equivalence", "blocked-oracle");
      }
      else
      {
        var reread = await worker.Reader.ReadAsync(new MemoryStream(canonicalBytes), _mshProfile);
        result.AddDiagnostics("msh.semantic-equivalence", reread.Diagnostics);
        rereadAsset = reread.Value;
        if (reread.Succeeded
          && ComputeSemanticDigest(asset) == ComputeSemanticDigest(reread.Value!))
        {
          result.CompleteSuccess("msh.semantic-equivalence");
        }
        else
        {
          result.CompleteFailure("msh.semantic-equivalence", "semantic-divergence");
        }
      }

      result.Begin("msh.canonical-idempotence");
      if (canonicalBytes is null || rereadAsset is null)
      {
        result.CompleteFailure("msh.canonical-idempotence", "blocked-oracle");
      }
      else
      {
        var second = new MemoryStream();
        var secondWrite = await worker.Writer.WriteAsync(rereadAsset, second, _mshProfile);
        result.AddDiagnostics("msh.canonical-idempotence", secondWrite.Diagnostics);
        if (secondWrite.Succeeded && second.ToArray().AsSpan().SequenceEqual(canonicalBytes))
        {
          result.CompleteSuccess("msh.canonical-idempotence");
        }
        else
        {
          result.CompleteFailure("msh.canonical-idempotence", "canonical-idempotence-failure");
        }
      }

      if (canonicalBytes is not null)
      {
        await QualifyInterchangeAsync(
          asset,
          canonicalBytes,
          index,
          temporaryRoot,
          worker,
          result);
      }
      return result;
    }

    private async Task QualifyInterchangeAsync(
      MeshAsset asset,
      byte[] canonicalBytes,
      int index,
      string temporaryRoot,
      WorkerContext worker,
      AssetResult result)
    {
      var directory = Path.Combine(temporaryRoot, $"asset-{index:D4}");
      using (_profiler.Measure("io.asset-directory-create"))
      {
        Directory.CreateDirectory(directory);
      }
      try
      {
        var options = new GltfExportOptions([_corpusRoot], [_corpusRoot]);
        var khronos = await worker.GetKhronosAsync();
        await QualifyGlbAsync(asset, options, directory, worker, khronos, result);
        await QualifySeparateGltfAsync(asset, options, directory, worker, khronos, result);
        await QualifyCliPackageAsync(
          GltfPackageKind.Glb,
          asset,
          canonicalBytes,
          directory,
          worker,
          khronos,
          result);
        await QualifyCliPackageAsync(
          GltfPackageKind.Gltf,
          asset,
          canonicalBytes,
          directory,
          worker,
          khronos,
          result);
      }
      catch
      {
        result.Fail("unexpected-interchange-oracle-failure");
      }
      finally
      {
        try
        {
          using (_profiler.Measure("io.asset-directory-delete"))
          {
            Directory.Delete(directory, recursive: true);
          }
        }
        catch
        {
          result.Fail("cleanup-failure");
        }
      }
    }

    private async Task QualifyGlbAsync(
      MeshAsset asset,
      GltfExportOptions options,
      string directory,
      WorkerContext worker,
      KhronosValidatorServer khronos,
      AssetResult result)
    {
      result.Begin("glb.export");
      var stream = new MemoryStream();
      var export = await asset.Match(
        onStatic: staticAsset => worker.Interchange.ExportGlbAsync(
          staticAsset,
          stream,
          options,
          _gltfProfile),
        onDynamic: dynamicAsset => worker.Interchange.ExportGlbAsync(
          dynamicAsset,
          stream,
          options,
          _gltfProfile));
      result.AddDiagnostics("glb.export", export.Diagnostics);
      if (!export.Succeeded)
      {
        result.CompleteFailure("glb.export", "glb-export-failure");
        result.FailBlockedPackageStages("glb");
        return;
      }
      result.CompleteSuccess("glb.export");
      var bytes = stream.ToArray();
      result.GlbBytes += bytes.LongLength;
      var packagePath = Path.Combine(directory, "package.glb");
      using (_profiler.Measure("io.glb-package-write"))
      {
        await File.WriteAllBytesAsync(packagePath, bytes);
      }

      result.Begin("glb.sharp-gltf-validate");
      var sharpValidation = await worker.Interchange.ValidateGlbAsync(
        new MemoryStream(bytes),
        _gltfProfile);
      result.AddDiagnostics("glb.sharp-gltf-validate", sharpValidation.Diagnostics);
      result.CompleteFromResult(
        "glb.sharp-gltf-validate",
        sharpValidation,
        "sharp-gltf-validation-failure");

      await ValidateKhronosAsync("glb.khronos-validate", packagePath, khronos, result);

      result.Begin("glb.unchanged-import");
      var importOptions = OfficialCorpusCliOracle.CreateImportOptions(asset);
      await using var canonicalPackage = File.OpenRead(packagePath);
      var import = await worker.Interchange.CreateMeshAsync(
        canonicalPackage,
        importOptions,
        profile: _gltfProfile);
      result.AddDiagnostics("glb.unchanged-import", import.Diagnostics);
      if (!import.Succeeded)
      {
        result.CompleteFailure("glb.unchanged-import", "unchanged-import-failure");
        result.FailStage("glb.canonical-baseline", "blocked-oracle");
        return;
      }
      result.CompleteSuccess("glb.unchanged-import");
      await ValidateImportedBaselineAsync(
        "glb.canonical-baseline",
        import.Value!,
        worker,
        result);
    }

    private async Task QualifySeparateGltfAsync(
      MeshAsset asset,
      GltfExportOptions options,
      string directory,
      WorkerContext worker,
      KhronosValidatorServer khronos,
      AssetResult result)
    {
      var packageDirectory = Path.Combine(directory, "separate");
      Directory.CreateDirectory(packageDirectory);
      var packagePath = Path.Combine(packageDirectory, "package.gltf");
      result.Begin("gltf.export");
      var export = await asset.Match(
        onStatic: staticAsset => worker.Interchange.ExportGltfFileAsync(
          staticAsset,
          packagePath,
          options,
          _gltfProfile),
        onDynamic: dynamicAsset => worker.Interchange.ExportGltfFileAsync(
          dynamicAsset,
          packagePath,
          options,
          _gltfProfile));
      result.AddDiagnostics("gltf.export", export.Diagnostics);
      if (!export.Succeeded)
      {
        result.CompleteFailure("gltf.export", "gltf-export-failure");
        result.FailBlockedPackageStages("gltf");
        return;
      }
      result.CompleteSuccess("gltf.export");
      using (_profiler.Measure("io.gltf-package-inventory"))
      {
        result.GltfManifestBytes += new FileInfo(packagePath).Length;
        result.GltfSidecarBytes += Directory.EnumerateFiles(packageDirectory, "*", SearchOption.AllDirectories)
          .Where(path => !string.Equals(path, packagePath, StringComparison.Ordinal))
          .Sum(path => new FileInfo(path).Length);
      }

      result.Begin("gltf.sharp-gltf-validate");
      var sharpValidation = await worker.Interchange.ValidateGltfFileAsync(packagePath, _gltfProfile);
      result.AddDiagnostics("gltf.sharp-gltf-validate", sharpValidation.Diagnostics);
      result.CompleteFromResult(
        "gltf.sharp-gltf-validate",
        sharpValidation,
        "sharp-gltf-validation-failure");

      await ValidateKhronosAsync("gltf.khronos-validate", packagePath, khronos, result);

      result.Begin("gltf.unchanged-import");
      var importOptions = OfficialCorpusCliOracle.CreateImportOptions(asset);
      var import = await worker.Interchange.CreateMeshFileAsync(
        packagePath,
        importOptions,
        profile: _gltfProfile);
      result.AddDiagnostics("gltf.unchanged-import", import.Diagnostics);
      if (!import.Succeeded)
      {
        result.CompleteFailure("gltf.unchanged-import", "unchanged-import-failure");
        result.FailStage("gltf.canonical-baseline", "blocked-oracle");
        return;
      }
      result.CompleteSuccess("gltf.unchanged-import");
      await ValidateImportedBaselineAsync(
        "gltf.canonical-baseline",
        import.Value!,
        worker,
        result);
    }

    private async Task ValidateKhronosAsync(
      string stage,
      string packagePath,
      KhronosValidatorServer khronos,
      AssetResult aggregate)
    {
      aggregate.Begin(stage);
      var validation = await khronos.ValidateAsync(packagePath);
      aggregate.Khronos.Add(validation);
      if (validation.Passed
        && validation.Errors == 0
        && validation.Warnings == 0)
      {
        aggregate.CompleteSuccess(stage);
      }
      else
      {
        aggregate.CompleteFailure(stage, validation.Failure ?? "khronos-validator-issue");
      }
    }

    private async Task ValidateImportedBaselineAsync(
      string stage,
      MeshAsset importedAsset,
      WorkerContext worker,
      AssetResult aggregate)
    {
      aggregate.Begin(stage);
      var stream = new MemoryStream();
      var result = await worker.Writer.WriteAsync(importedAsset, stream, _mshProfile);
      aggregate.AddDiagnostics(stage, result.Diagnostics);
      var imported = stream.ToArray();
      aggregate.UnchangedImportedMshBytes += imported.LongLength;
      var validation = await worker.Validator.ValidateAsync(importedAsset, _mshProfile);
      aggregate.AddDiagnostics(stage, validation.Diagnostics);
      var reread = await worker.Reader.ReadAsync(new MemoryStream(imported), _mshProfile);
      aggregate.AddDiagnostics(stage, reread.Diagnostics);
      var semanticMatch = reread.Succeeded
        && ComputeSemanticDigest(importedAsset) == ComputeSemanticDigest(reread.Value!);
      if (result.Succeeded && validation.Succeeded && semanticMatch)
      {
        aggregate.CompleteSuccess(stage);
      }
      else
      {
        aggregate.CompleteFailure(stage, "canonical-baseline-divergence");
      }
    }

    private async Task QualifyCliPackageAsync(
      GltfPackageKind packageKind,
      MeshAsset asset,
      byte[] canonicalBytes,
      string directory,
      WorkerContext worker,
      KhronosValidatorServer khronos,
      AssetResult aggregate)
    {
      var package = packageKind == GltfPackageKind.Glb ? "glb" : "gltf";
      var exportStage = $"{package}.cli-export";
      var sharpValidationStage = $"{package}.cli-sharp-gltf-validate";
      var khronosValidationStage = $"{package}.cli-khronos-validate";
      var importStage = $"{package}.cli-unchanged-import";
      aggregate.Begin(exportStage, measure: false);
      aggregate.Begin(importStage, measure: false);
      var result = await OfficialCorpusCliOracle.RunAsync(
        canonicalBytes,
        packageKind,
        directory,
        _corpusRoot);
      _profiler.Add(exportStage, result.ExportDuration);
      _profiler.Add(importStage, result.ImportDuration);
      _profiler.Add("io.cli-temporary-package", result.TemporaryIoDuration);
      aggregate.CliPackageBytes += result.PackageBytes;
      aggregate.CliImportedMshBytes += result.ImportedMshBytes;
      aggregate.AddDiagnostics(exportStage, result.ExportDiagnostics);
      aggregate.AddDiagnostics(importStage, result.ImportDiagnostics);
      aggregate.Begin(sharpValidationStage);
      var publicParity = result.ExportSucceeded
        && HasPublicCliPackageParity(asset, result);
      if (publicParity)
      {
        aggregate.CompleteSuccess(exportStage);
      }
      else
      {
        aggregate.CompleteFailure(
          exportStage,
          result.ExportSucceeded ? "cli-public-parity-failure" : "cli-export-failure");
      }
      if (result.ExportSucceeded && result.PackagePath is not null)
      {
        OperationResult strictValidation;
        if (packageKind == GltfPackageKind.Glb)
        {
          strictValidation = await worker.Interchange.ValidateGlbAsync(
            new MemoryStream(await File.ReadAllBytesAsync(result.PackagePath)),
            _gltfProfile);
        }
        else
        {
          strictValidation = await worker.Interchange.ValidateGltfFileAsync(result.PackagePath, _gltfProfile);
        }
        aggregate.AddDiagnostics(sharpValidationStage, strictValidation.Diagnostics);
        aggregate.CompleteFromResult(
          sharpValidationStage,
          strictValidation,
          "cli-sharp-gltf-validation-failure");
        await ValidateKhronosAsync(khronosValidationStage, result.PackagePath, khronos, aggregate);
      }
      else
      {
        aggregate.CompleteFailure(sharpValidationStage, "blocked-oracle");
        aggregate.FailStage(khronosValidationStage, "blocked-oracle");
      }
      if (result.ImportSucceeded)
      {
        aggregate.CompleteSuccess(importStage);
      }
      else
      {
        aggregate.CompleteFailure(importStage, result.ExportSucceeded
          ? "cli-unchanged-import-failure"
          : "blocked-oracle");
      }
    }

    private static bool HasPublicCliPackageParity(
      MeshAsset asset,
      CliOracleResult cli)
    {
      var expectedAssetKind = asset.Kind == MeshAssetKind.Static ? "static" : "dynamic";
      return cli.PackagePath is not null
        && File.Exists(cli.PackagePath)
        && cli.AssetKind == expectedAssetKind;
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

    private void Merge(AssetResult result)
    {
      if (result.ContentFingerprint is not null)
      {
        _contentFingerprints.Add(result.ContentFingerprint);
      }
      _inputBytes += result.InputBytes;
      _canonicalMshBytes += result.CanonicalMshBytes;
      _glbBytes += result.GlbBytes;
      _gltfManifestBytes += result.GltfManifestBytes;
      _gltfSidecarBytes += result.GltfSidecarBytes;
      _unchangedImportedMshBytes += result.UnchangedImportedMshBytes;
      _cliPackageBytes += result.CliPackageBytes;
      _cliImportedMshBytes += result.CliImportedMshBytes;
      _staticCount += result.StaticCount;
      _dynamicCount += result.DynamicCount;
      _dynamicCoverage.Merge(result.DynamicCoverage);
      foreach (var operation in result.Operations)
      {
        var target = GetOperation(operation.Key);
        target.Attempted += operation.Value.Attempted;
        target.Passed += operation.Value.Passed;
        target.Failed += operation.Value.Failed;
      }
      foreach (var diagnostic in result.Diagnostics)
      {
        _diagnostics[diagnostic.Key] = _diagnostics.GetValueOrDefault(diagnostic.Key)
          + diagnostic.Value;
      }
      foreach (var failure in result.Failures)
      {
        _failures[failure.Key] = _failures.GetValueOrDefault(failure.Key) + failure.Value;
      }
      _khronos.Merge(result.Khronos);
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

    private void Fail(string category)
    {
      _failures[category] = _failures.GetValueOrDefault(category) + 1;
    }
  }

  private sealed class WorkerContext : IAsyncDisposable
  {
    private readonly QualificationProfiler _profiler;
    private KhronosValidatorServer? _khronos;

    internal MshReader Reader { get; } = new();
    internal MshValidator Validator { get; } = new();
    internal MshWriter Writer { get; } = new();
    internal GltfInterchange Interchange { get; } = new();

    internal WorkerContext(QualificationProfiler profiler)
    {
      _profiler = profiler;
    }

    internal async Task<KhronosValidatorServer> GetKhronosAsync()
    {
      if (_khronos is null)
      {
        using (_profiler.Measure("khronos.process-start"))
        {
          _khronos = await KhronosValidatorServer.StartAsync();
        }
      }
      return _khronos;
    }

    public async ValueTask DisposeAsync()
    {
      if (_khronos is not null)
      {
        await _khronos.DisposeAsync();
      }
    }
  }

  private sealed class AssetResult
  {
    private readonly QualificationProfiler _profiler;
    private readonly Dictionary<string, long> _timingStarts = new(StringComparer.Ordinal);

    internal Dictionary<string, OperationCounts> Operations { get; } = new(StringComparer.Ordinal);
    internal Dictionary<DiagnosticKey, int> Diagnostics { get; } = [];
    internal Dictionary<string, int> Failures { get; } = new(StringComparer.Ordinal);
    internal ValidatorAggregate Khronos { get; } = new();
    internal DynamicCoverage DynamicCoverage { get; } = new();
    internal ContentFingerprint? ContentFingerprint { get; set; }
    internal long InputBytes { get; set; }
    internal long CanonicalMshBytes { get; set; }
    internal long GlbBytes { get; set; }
    internal long GltfManifestBytes { get; set; }
    internal long GltfSidecarBytes { get; set; }
    internal long UnchangedImportedMshBytes { get; set; }
    internal long CliPackageBytes { get; set; }
    internal long CliImportedMshBytes { get; set; }
    internal int StaticCount { get; set; }
    internal int DynamicCount { get; set; }
    internal int FailureCount => Failures.Values.Sum();

    internal AssetResult(QualificationProfiler profiler)
    {
      _profiler = profiler;
    }

    internal void Begin(string stage, bool measure = true)
    {
      GetOperation(stage).Attempted++;
      if (measure && _profiler.Enabled)
      {
        _timingStarts[stage] = Stopwatch.GetTimestamp();
      }
    }

    internal void CompleteSuccess(string stage)
    {
      GetOperation(stage).Passed++;
      CompleteTiming(stage);
    }

    internal void CompleteFailure(string stage, string category)
    {
      GetOperation(stage).Failed++;
      CompleteTiming(stage);
      Fail(category);
    }

    internal void CompleteFromResult(string stage, OperationResult result, string category)
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

    internal void FailBlockedBinaryStages(string? after = null)
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

    internal void FailBlockedPackageStages(string package)
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

    internal void FailStage(string stage, string category)
    {
      Begin(stage);
      CompleteFailure(stage, category);
    }

    internal void AddDiagnostics(string stage, IEnumerable<OperationDiagnostic> diagnostics)
    {
      foreach (var diagnostic in diagnostics)
      {
        AddDiagnostic(stage, diagnostic.Code, diagnostic.EventId, diagnostic.Severity);
      }
    }

    internal void AddDiagnostics(string stage, IEnumerable<CliDiagnostic> diagnostics)
    {
      foreach (var diagnostic in diagnostics)
      {
        AddDiagnostic(stage, diagnostic.Code, diagnostic.EventId, diagnostic.Severity);
      }
    }

    internal void Fail(string category)
    {
      Failures[category] = Failures.GetValueOrDefault(category) + 1;
    }

    private void AddDiagnostic(
      string stage,
      string code,
      int eventId,
      DiagnosticSeverity severity)
    {
      var key = new DiagnosticKey(stage, code, eventId, severity);
      Diagnostics[key] = Diagnostics.GetValueOrDefault(key) + 1;
    }

    private OperationCounts GetOperation(string stage)
    {
      if (!Operations.TryGetValue(stage, out var counts))
      {
        counts = new OperationCounts();
        Operations.Add(stage, counts);
      }
      return counts;
    }

    private void CompleteTiming(string stage)
    {
      if (_timingStarts.Remove(stage, out var started))
      {
        _profiler.Add(stage, Stopwatch.GetElapsedTime(started));
      }
    }
  }

  private sealed class DynamicCoverage
  {
    private readonly Dictionary<DynamicEffectType, int> _effectTypes = [];
    private readonly Dictionary<DynamicAlphaTiming, int> _alphaTimingModes = [];
    private readonly Dictionary<DynamicLightType, int> _terrainLightModes = [];

    private int _assets;
    private int _objects;
    private int _maximumDepth;
    private int _nestedAssets;
    private int _mixedEffectAssets;
    private int _unknownEffectObjects;
    private int _meshResourceBindings;
    private int _textureResourceBindings;
    private int _positiveRibbonHalfWidths;
    private int _negativeRibbonHalfWidths;
    private int _zeroRibbonHalfWidths;
    private int _frameDeclarations;
    private int _atlasDeclarations;
    private int _unknownAlphaTimingObjects;
    private int _unknownTerrainLightObjects;
    private int _additiveObjects;
    private int _nonAdditiveObjects;
    private int _translatedObjects;
    private int _scaledObjects;
    private int _metadataOnlyObjects;

    internal void Add(DynamicMeshAsset asset)
    {
      _assets++;
      if (asset.RootDynamicObject.Children.Count > 0)
      {
        _nestedAssets++;
      }
      var assetEffectTypes = new HashSet<DynamicEffectType>();
      AddObject(asset.RootDynamicObject, 1, assetEffectTypes);
      if (assetEffectTypes.Count > 1)
      {
        _mixedEffectAssets++;
      }
    }

    internal void Merge(DynamicCoverage other)
    {
      _assets += other._assets;
      _objects += other._objects;
      _maximumDepth = Math.Max(_maximumDepth, other._maximumDepth);
      _nestedAssets += other._nestedAssets;
      _mixedEffectAssets += other._mixedEffectAssets;
      _unknownEffectObjects += other._unknownEffectObjects;
      _meshResourceBindings += other._meshResourceBindings;
      _textureResourceBindings += other._textureResourceBindings;
      _positiveRibbonHalfWidths += other._positiveRibbonHalfWidths;
      _negativeRibbonHalfWidths += other._negativeRibbonHalfWidths;
      _zeroRibbonHalfWidths += other._zeroRibbonHalfWidths;
      _frameDeclarations += other._frameDeclarations;
      _atlasDeclarations += other._atlasDeclarations;
      _unknownAlphaTimingObjects += other._unknownAlphaTimingObjects;
      _unknownTerrainLightObjects += other._unknownTerrainLightObjects;
      _additiveObjects += other._additiveObjects;
      _nonAdditiveObjects += other._nonAdditiveObjects;
      _translatedObjects += other._translatedObjects;
      _scaledObjects += other._scaledObjects;
      _metadataOnlyObjects += other._metadataOnlyObjects;
      MergeCounts(_effectTypes, other._effectTypes);
      MergeCounts(_alphaTimingModes, other._alphaTimingModes);
      MergeCounts(_terrainLightModes, other._terrainLightModes);
    }

    internal object CreateSummary()
    {
      return new
      {
        assets = _assets,
        objects = _objects,
        maximumDepth = _maximumDepth,
        nestedAssets = _nestedAssets,
        mixedEffectAssets = _mixedEffectAssets,
        effectTypes = _effectTypes
          .OrderBy(pair => pair.Key)
          .Select(pair => new { effectType = pair.Key.ToString(), count = pair.Value }),
        unknownEffectObjects = _unknownEffectObjects,
        meshResourceBindings = _meshResourceBindings,
        textureResourceBindings = _textureResourceBindings,
        ribbonHalfWidths = new
        {
          positive = _positiveRibbonHalfWidths,
          negative = _negativeRibbonHalfWidths,
          zero = _zeroRibbonHalfWidths
        },
        frameDeclarations = _frameDeclarations,
        atlasDeclarations = _atlasDeclarations,
        alphaTimingModes = _alphaTimingModes
          .OrderBy(pair => pair.Key)
          .Select(pair => new { mode = pair.Key.ToString(), count = pair.Value }),
        unknownAlphaTimingObjects = _unknownAlphaTimingObjects,
        terrainLightModes = _terrainLightModes
          .OrderBy(pair => pair.Key)
          .Select(pair => new { mode = pair.Key.ToString(), count = pair.Value }),
        unknownTerrainLightObjects = _unknownTerrainLightObjects,
        additiveObjects = _additiveObjects,
        nonAdditiveObjects = _nonAdditiveObjects,
        translatedObjects = _translatedObjects,
        scaledObjects = _scaledObjects,
        metadataOnlyObjects = _metadataOnlyObjects
      };
    }

    private void AddObject(
      DynamicObject item,
      int depth,
      ISet<DynamicEffectType> assetEffectTypes)
    {
      _objects++;
      _maximumDepth = Math.Max(_maximumDepth, depth);
      var extension = item.Extension;
      if (extension.KnownEffectType is { } effectType)
      {
        _effectTypes[effectType] = _effectTypes.GetValueOrDefault(effectType) + 1;
        assetEffectTypes.Add(effectType);
        if (effectType == DynamicEffectType.Group)
        {
          _metadataOnlyObjects++;
        }
        if (effectType == DynamicEffectType.ScalableObject
          && (extension.StartModelScale != 1
            || extension.EndModelScale != 1
            || extension.StartModelScale != extension.EndModelScale))
        {
          _scaledObjects++;
        }
        if (effectType is DynamicEffectType.Laser
          or DynamicEffectType.LaserWall
          or DynamicEffectType.ElectricalCannon
          or DynamicEffectType.Lightning)
        {
          if (extension.RibbonHalfWidth > 0)
          {
            _positiveRibbonHalfWidths++;
          }
          else if (extension.RibbonHalfWidth < 0)
          {
            _negativeRibbonHalfWidths++;
          }
          else
          {
            _zeroRibbonHalfWidths++;
          }
        }
      }
      else
      {
        _unknownEffectObjects++;
      }
      if (extension.MeshNameBytes.Count > 0)
      {
        _meshResourceBindings++;
      }
      if (extension.TexturePathBytes.Count > 0)
      {
        _textureResourceBindings++;
      }
      if (extension.FirstSourceFrame != 0
        || extension.FrameCount != 0
        || extension.FramePeriodTicks != 0)
      {
        _frameDeclarations++;
      }
      if (extension.SpriteSheetColumnCount != 0
        || extension.SpriteSheetRowCount != 0
        || extension.ReciprocalColumnCount != 0
        || extension.ReciprocalRowCount != 0)
      {
        _atlasDeclarations++;
      }
      if (extension.KnownAlphaTiming is { } alphaTiming)
      {
        _alphaTimingModes[alphaTiming] = _alphaTimingModes.GetValueOrDefault(alphaTiming) + 1;
      }
      else
      {
        _unknownAlphaTimingObjects++;
      }
      if (extension.KnownLightType is { } lightType)
      {
        _terrainLightModes[lightType] = _terrainLightModes.GetValueOrDefault(lightType) + 1;
      }
      else
      {
        _unknownTerrainLightObjects++;
      }
      if (extension.UsesAdditiveBlending)
      {
        _additiveObjects++;
      }
      else
      {
        _nonAdditiveObjects++;
      }
      if (extension.ChildStartTranslation != System.Numerics.Vector3.Zero
        || extension.ChildEndTranslation != System.Numerics.Vector3.Zero)
      {
        _translatedObjects++;
      }
      foreach (var child in item.Children)
      {
        AddObject(child, depth + 1, assetEffectTypes);
      }
    }

    private static void MergeCounts<TKey>(
      IDictionary<TKey, int> target,
      IReadOnlyDictionary<TKey, int> source)
      where TKey : notnull
    {
      foreach (var item in source)
      {
        target.TryGetValue(item.Key, out var current);
        target[item.Key] = current + item.Value;
      }
    }
  }

  private sealed class QualificationProfiler
  {
    private readonly object _gate = new();
    private readonly string? _path;
    private readonly int _workers;
    private readonly Dictionary<string, TimingAggregate> _timings = new(StringComparer.Ordinal);

    internal QualificationProfiler(string? path, int workers)
    {
      _path = path;
      _workers = workers;
    }

    internal bool Enabled => !string.IsNullOrWhiteSpace(_path);

    internal ProfileScope Measure(string stage)
    {
      return new ProfileScope(this, stage);
    }

    internal void Add(string stage, TimeSpan elapsed)
    {
      if (string.IsNullOrWhiteSpace(_path))
      {
        return;
      }
      lock (_gate)
      {
        if (!_timings.TryGetValue(stage, out var timing))
        {
          timing = new TimingAggregate();
          _timings.Add(stage, timing);
        }
        timing.Count++;
        timing.Elapsed += elapsed;
      }
    }

    internal async Task WriteAsync(TimeSpan wallClock)
    {
      if (string.IsNullOrWhiteSpace(_path))
      {
        return;
      }
      object[] stages;
      lock (_gate)
      {
        stages = _timings
          .OrderBy(pair => pair.Key, StringComparer.Ordinal)
          .Select(pair => (object)new
          {
            stage = pair.Key,
            count = pair.Value.Count,
            totalMilliseconds = Math.Round(pair.Value.Elapsed.TotalMilliseconds, 3),
            averageMilliseconds = Math.Round(
              pair.Value.Elapsed.TotalMilliseconds / pair.Value.Count,
              3)
          })
          .ToArray();
      }
      var directory = Path.GetDirectoryName(_path);
      if (!string.IsNullOrEmpty(directory))
      {
        Directory.CreateDirectory(directory);
      }
      var profile = new
      {
        format = "earthtool.official-msh-corpus-profile-event",
        version = 1,
        workers = _workers,
        wallClockMilliseconds = Math.Round(wallClock.TotalMilliseconds, 3),
        stages
      };
      await File.WriteAllTextAsync(
        _path,
        JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    internal sealed class ProfileScope : IDisposable
    {
      private readonly QualificationProfiler _owner;
      private readonly string _stage;
      private readonly long _started = Stopwatch.GetTimestamp();

      internal ProfileScope(QualificationProfiler owner, string stage)
      {
        _owner = owner;
        _stage = stage;
      }

      public void Dispose()
      {
        _owner.Add(_stage, Stopwatch.GetElapsedTime(_started));
      }
    }

    private sealed class TimingAggregate
    {
      internal int Count { get; set; }
      internal TimeSpan Elapsed { get; set; }
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

    internal void Merge(ValidatorAggregate aggregate)
    {
      _version ??= aggregate._version;
      _packages += aggregate._packages;
      _errors += aggregate._errors;
      _warnings += aggregate._warnings;
      _infos += aggregate._infos;
      _hints += aggregate._hints;
      foreach (var code in aggregate._codes)
      {
        _codes[code.Key] = _codes.GetValueOrDefault(code.Key) + code.Value;
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
      profile.MaxMeshResourceBytes,
      profile.MaxMeshResourceSearchRoots,
      profile.MaxMeshResourceDirectoryEntries,
      profile.MaxMeshResources,
      profile.MaxMeshPreviewVertices,
      profile.MaxMeshResourceDepth,
      profile.MaxTotalMetadataBytes,
      profile.MaxMetadataEnvelopes,
      profile.MaxMetadataElements,
      profile.MaxUnknownMetadataMembers
    };
  }
}
