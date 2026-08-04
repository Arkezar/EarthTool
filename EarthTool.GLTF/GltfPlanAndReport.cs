#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.GLTF
{
  /// <summary>Describes the independently versioned import-plan protocol.</summary>
  public static class GltfImportPlanFormat
  {
    /// <summary>Gets the import-plan protocol identifier.</summary>
    public const string Identifier = "earthtool.msh.import-plan";
    /// <summary>Gets the current import-plan protocol version.</summary>
    public const int Version = 1;
    /// <summary>Gets every import-plan version accepted by this build.</summary>
    public static IReadOnlyList<int> SupportedVersions { get; } = Array.AsReadOnly(new[] { Version });
  }

  /// <summary>Describes the independently versioned CLI machine-report protocol.</summary>
  public static class GltfCliReportFormat
  {
    /// <summary>Gets the CLI-report protocol identifier.</summary>
    public const string Identifier = "earthtool.msh.cli-report";
    /// <summary>Gets the current CLI-report protocol version.</summary>
    public const int Version = 1;
    /// <summary>Gets every CLI-report version emitted by this build.</summary>
    public static IReadOnlyList<int> SupportedVersions { get; } = Array.AsReadOnly(new[] { Version });
  }

  /// <summary>Names the two supported glTF package forms.</summary>
  public enum GltfPackageKind
  {
    /// <summary>One self-contained binary glTF file.</summary>
    Glb = 0,
    /// <summary>One JSON manifest with external sidecars.</summary>
    Gltf = 1
  }

  /// <summary>Names the closed import intents represented by a version-1 plan.</summary>
  public enum GltfImportPlanKind
  {
    /// <summary>Canonical admission of metadata-free native content.</summary>
    NewModel = 0,
    /// <summary>Reconciliation against an expected interchange baseline.</summary>
    Edit = 1
  }

  /// <summary>Contains one validated, immutable import plan.</summary>
  public sealed class GltfImportPlan
  {
    /// <summary>Gets the independent import-plan format identifier.</summary>
    public string Format => GltfImportPlanFormat.Identifier;
    /// <summary>Gets the independent import-plan protocol version.</summary>
    public int Version => GltfImportPlanFormat.Version;
    /// <summary>Gets the import intent.</summary>
    public GltfImportPlanKind Kind { get; }
    /// <summary>Gets the source package form.</summary>
    public GltfPackageKind PackageKind { get; }
    /// <summary>Gets the lowercase source-package SHA-256 binding.</summary>
    public string SourceSha256 { get; }
    /// <summary>Gets the required edit baseline, or null for new-model import.</summary>
    public InterchangeBaseline? ExpectedBaseline { get; }
    /// <summary>Gets typed new-model overrides, or null for edit import.</summary>
    public GltfNewModelImportOptions? NewModelOptions { get; }
    /// <summary>Gets exact conflict actions, or null for new-model import.</summary>
    public GltfEditImportOptions? EditOptions { get; }

    private GltfImportPlan(
      GltfImportPlanKind kind,
      GltfPackageKind packageKind,
      string sourceSha256,
      InterchangeBaseline? expectedBaseline,
      GltfNewModelImportOptions? newModelOptions,
      GltfEditImportOptions? editOptions)
    {
      if (!Enum.IsDefined(typeof(GltfPackageKind), packageKind))
      {
        throw new ArgumentOutOfRangeException(nameof(packageKind));
      }
      ValidateSha256(sourceSha256, nameof(sourceSha256));
      Kind = kind;
      PackageKind = packageKind;
      SourceSha256 = sourceSha256;
      ExpectedBaseline = expectedBaseline;
      NewModelOptions = newModelOptions;
      EditOptions = editOptions;
    }

    /// <summary>Creates one source-bound new-model plan from typed semantic overrides.</summary>
    public static GltfImportPlan CreateNewModel(
      GltfPackageKind packageKind,
      string sourceSha256,
      GltfNewModelImportOptions? options = null)
    {
      return new GltfImportPlan(
        GltfImportPlanKind.NewModel,
        packageKind,
        sourceSha256,
        null,
        options ?? new GltfNewModelImportOptions(),
        null);
    }

    /// <summary>Creates one source- and baseline-bound edit plan from exact conflict actions.</summary>
    public static GltfImportPlan CreateEdit(
      GltfPackageKind packageKind,
      string sourceSha256,
      InterchangeBaseline expectedBaseline,
      GltfEditImportOptions? options = null)
    {
      var editOptions = options ?? new GltfEditImportOptions();
      if (editOptions.ConflictResolutions.Any(resolution =>
        !GltfImportPlanSerializer.IsConflictKey(resolution.ConflictKey)))
      {
        throw new ArgumentException("Every conflict action must use a canonical version-1 conflict key.", nameof(options));
      }
      return new GltfImportPlan(
        GltfImportPlanKind.Edit,
        packageKind,
        sourceSha256,
        expectedBaseline ?? throw new ArgumentNullException(nameof(expectedBaseline)),
        null,
        editOptions);
    }

    internal OperationDiagnostic? ValidateProfile(GltfOperationProfile profile)
    {
      if (EditOptions?.ConflictResolutions.Count > profile.MaxMetadataConflicts)
      {
        return Limit(
          "conflictActions",
          EditOptions.ConflictResolutions.Count,
          profile.MaxMetadataConflicts);
      }
      var elementCount = Kind == GltfImportPlanKind.Edit
        ? EditOptions!.ConflictResolutions.Count * 3L
        : CountSemanticOverrideElements(NewModelOptions!);
      if (elementCount > profile.MaxMetadataElements)
      {
        return Limit("$", elementCount, profile.MaxMetadataElements);
      }
      var serializedLength = GltfImportPlanSerializer.GetSerializedLength(this);
      return serializedLength > profile.MaxMetadataBytes
        ? Limit("$", serializedLength, profile.MaxMetadataBytes)
        : null;
    }

    private static long CountSemanticOverrideElements(GltfNewModelImportOptions options)
    {
      return checked(
        (options.TextureResourceBindings.Count * 2L)
        + (options.ObjectRoles.Count * 3L)
        + (options.HelperBindings.Count * 3L)
        + (options.StaticLightOptions.Count * 3L)
        + (options.AnimationClasses.Count * 2L)
        + (options.Footprint is null ? 0 : 33)
        + (options.HorizontalExtents is null ? 0 : 4));
    }

    private static OperationDiagnostic Limit(string path, long actual, int maximum)
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.ImportPlanResourceLimitExceeded,
        3002,
        DiagnosticSeverity.Error,
        path,
        "The import plan exceeds its finite operation profile.",
        data: new Dictionary<string, string>
        {
          ["actual"] = actual.ToString(CultureInfo.InvariantCulture),
          ["maximum"] = maximum.ToString(CultureInfo.InvariantCulture)
        });
    }

    private static void ValidateSha256(string value, string parameterName)
    {
      if (value is null)
      {
        throw new ArgumentNullException(parameterName);
      }
      if (value.Length != 64 || value.Any(character =>
        character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
      {
        throw new ArgumentException("The source digest must be lowercase SHA-256 hexadecimal.", parameterName);
      }
    }
  }

  /// <summary>Reads and writes the strict version-1 import-plan protocol.</summary>
  public sealed class GltfImportPlanSerializer
  {
    /// <summary>Reads one bounded strict import plan without exposing mutable wire DTOs.</summary>
    public async Task<OperationResult<GltfImportPlan>> DeserializeAsync(
      Stream source,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }
      profile ??= GltfOperationProfile.Default;
      try
      {
        var bytes = await ReadBoundedAsync(source, profile.MaxMetadataBytes, cancellationToken)
          .ConfigureAwait(false);
        ValidateJsonStructure(bytes, profile);
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions
        {
          AllowTrailingCommas = false,
          CommentHandling = JsonCommentHandling.Disallow,
          MaxDepth = profile.MaxJsonDepth
        });
        return new OperationResult<GltfImportPlan>(
          OperationStatus.Succeeded,
          Parse(document.RootElement, profile));
      }
      catch (OperationCanceledException)
      {
        return Failed(GltfDiagnosticCodes.Cancelled, 1105, "$", "Import-plan reading was cancelled.");
      }
      catch (ImportPlanException ex)
      {
        return Failed(ex.Code, ex.EventId, ex.Path, ex.Message, ex.DiagnosticData);
      }
      catch (Exception ex) when (ex is JsonException
        || ex is InvalidOperationException
        || ex is ArgumentException
        || ex is FormatException
        || ex is OverflowException)
      {
        return Failed(
          GltfDiagnosticCodes.MalformedImportPlan,
          3000,
          "$",
          "The import plan is malformed: " + ex.Message);
      }
      catch (Exception ex)
      {
        return Failed(
          GltfDiagnosticCodes.IoFailure,
          1104,
          "$",
          ex.Message);
      }
    }

    /// <summary>Writes one validated import plan with deterministic JSON ordering.</summary>
    public async Task<OperationResult> SerializeAsync(
      GltfImportPlan plan,
      Stream destination,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (plan is null)
      {
        throw new ArgumentNullException(nameof(plan));
      }
      if (destination is null)
      {
        throw new ArgumentNullException(nameof(destination));
      }
      profile ??= GltfOperationProfile.Default;
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        var planLimit = plan.ValidateProfile(profile);
        if (planLimit is not null)
        {
          return new OperationResult(OperationStatus.Failed, new[] { planLimit });
        }
        var bytes = WritePlan(plan);
        if (bytes.Length > profile.MaxMetadataBytes)
        {
          return Limit(bytes.Length, profile.MaxMetadataBytes);
        }
        await destination.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
        return new OperationResult(OperationStatus.Succeeded);
      }
      catch (OperationCanceledException)
      {
        return new OperationResult(OperationStatus.Cancelled, new[]
        {
          Diagnostic(GltfDiagnosticCodes.Cancelled, 1105, "$", "Import-plan writing was cancelled.")
        });
      }
      catch (Exception ex)
      {
        return new OperationResult(OperationStatus.Failed, new[]
        {
          Diagnostic(GltfDiagnosticCodes.IoFailure, 1104, "$", ex.Message)
        });
      }
    }

    /// <summary>Computes the lowercase SHA-256 source binding for exact GLB bytes.</summary>
    public async Task<OperationResult<string>> ComputeGlbSourceSha256Async(
      Stream source,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }
      profile ??= GltfOperationProfile.Default;
      try
      {
        var bytes = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken)
          .ConfigureAwait(false);
        return new OperationResult<string>(OperationStatus.Succeeded, Hash(bytes));
      }
      catch (OperationCanceledException)
      {
        return new OperationResult<string>(
          OperationStatus.Cancelled,
          diagnostics: new[] { Diagnostic(GltfDiagnosticCodes.Cancelled, 1105, "$", "Source hashing was cancelled.") });
      }
      catch (ImportPlanException ex)
      {
        return FailedString(ex.Code, ex.EventId, ex.Path, ex.Message, ex.DiagnosticData);
      }
      catch (Exception ex)
      {
        return FailedString(GltfDiagnosticCodes.IoFailure, 1104, "$", ex.Message);
      }
    }

    /// <summary>Computes the lowercase SHA-256 source binding for a complete separate glTF package.</summary>
    public async Task<OperationResult<string>> ComputeGltfSourceSha256Async(
      string sourcePath,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (sourcePath is null)
      {
        throw new ArgumentNullException(nameof(sourcePath));
      }
      profile ??= GltfOperationProfile.Default;
      try
      {
        var package = await GltfInterchange.ReadSeparatePackageAsync(
          sourcePath,
          profile,
          cancellationToken).ConfigureAwait(false);
        return new OperationResult<string>(OperationStatus.Succeeded, HashSeparate(package));
      }
      catch (OperationCanceledException)
      {
        return new OperationResult<string>(
          OperationStatus.Cancelled,
          diagnostics: new[] { Diagnostic(GltfDiagnosticCodes.Cancelled, 1105, "$", "Source hashing was cancelled.") });
      }
      catch (Exception ex)
      {
        return FailedString(GltfDiagnosticCodes.IoFailure, 1104, sourcePath, ex.Message);
      }
    }

    internal static bool MatchesSeparateSource(
      GltfInterchange.SeparateGltfPackage package,
      string expectedSha256)
    {
      return string.Equals(HashSeparate(package), expectedSha256, StringComparison.Ordinal);
    }

    internal static string Hash(byte[] bytes)
    {
      using var sha256 = SHA256.Create();
      return ToHex(sha256.ComputeHash(bytes));
    }

    internal static int GetSerializedLength(GltfImportPlan plan)
    {
      return WritePlan(plan).Length;
    }

    private static string HashSeparate(GltfInterchange.SeparateGltfPackage package)
    {
      using var sha256 = SHA256.Create();
      using (var sink = new CryptoStream(Stream.Null, sha256, CryptoStreamMode.Write))
      {
        var prefix = Encoding.UTF8.GetBytes("earthtool.msh.import-plan:gltf:1\n");
        sink.Write(prefix, 0, prefix.Length);
        WriteDigestEntry(sink, "$manifest", package.Json);
        WriteDigestEntry(sink, package.BufferUri, package.Binary);
        foreach (var image in package.Images.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
          WriteDigestEntry(sink, image.Key, image.Value);
        }
        sink.FlushFinalBlock();
      }
      return ToHex(sha256.Hash!);
    }

    private static void WriteDigestEntry(Stream destination, string name, byte[] content)
    {
      var nameBytes = Encoding.UTF8.GetBytes(name);
      var lengths = new byte[sizeof(int) + sizeof(long)];
      BinaryPrimitives.WriteInt32LittleEndian(lengths.AsSpan(0, sizeof(int)), nameBytes.Length);
      BinaryPrimitives.WriteInt64LittleEndian(lengths.AsSpan(sizeof(int), sizeof(long)), content.Length);
      destination.Write(lengths, 0, sizeof(int));
      destination.Write(nameBytes, 0, nameBytes.Length);
      destination.Write(lengths, sizeof(int), sizeof(long));
      destination.Write(content, 0, content.Length);
    }

    private static string ToHex(byte[] value)
    {
      return string.Concat(value.Select(item =>
        item.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static GltfImportPlan Parse(JsonElement root, GltfOperationProfile profile)
    {
      RequireKind(root, JsonValueKind.Object, "$", "The import-plan root must be an object.");
      var format = RequiredString(root, "format", "format");
      if (format != GltfImportPlanFormat.Identifier)
      {
        throw Malformed("format", "The import-plan format identifier is invalid.");
      }
      var version = RequiredInt32(root, "version", "version");
      if (!GltfImportPlanFormat.SupportedVersions.Contains(version))
      {
        throw new ImportPlanException(
          GltfDiagnosticCodes.UnsupportedImportPlanVersion,
          3001,
          "version",
          "The import-plan version is unsupported.",
          new Dictionary<string, string>
          {
            ["actual"] = version.ToString(CultureInfo.InvariantCulture),
            ["supported"] = string.Join(",", GltfImportPlanFormat.SupportedVersions)
          });
      }
      var mode = RequiredString(root, "mode", "mode");
      var packageKind = RequiredString(root, "package", "package") switch
      {
        "glb" => GltfPackageKind.Glb,
        "gltf" => GltfPackageKind.Gltf,
        _ => throw Malformed("package", "The import-plan package kind is invalid.")
      };
      var sourceSha256 = RequiredString(root, "sourceSha256", "sourceSha256");

      if (mode == "newModel")
      {
        EnsureProperties(root, "$", "format", "version", "mode", "package", "sourceSha256", "semanticOverrides");
        return GltfImportPlan.CreateNewModel(
          packageKind,
          sourceSha256,
          ParseOverrides(Required(root, "semanticOverrides", "semanticOverrides")));
      }
      if (mode == "edit")
      {
        EnsureProperties(root, "$", "format", "version", "mode", "package", "sourceSha256", "expectedBaseline", "conflictActions");
        return GltfImportPlan.CreateEdit(
          packageKind,
          sourceSha256,
          ParseBaseline(Required(root, "expectedBaseline", "expectedBaseline"), "expectedBaseline"),
          ParseConflictActions(Required(root, "conflictActions", "conflictActions"), profile));
      }
      throw Malformed("mode", "The import-plan mode is invalid.");
    }

    private static GltfNewModelImportOptions ParseOverrides(JsonElement value)
    {
      RequireKind(value, JsonValueKind.Object, "semanticOverrides", "Semantic overrides must be an object.");
      EnsureProperties(value, "semanticOverrides",
        "textureResourceBindings", "footprint", "horizontalExtents", "objectRoles",
        "helperBindings", "staticLightOptions", "animationClasses");
      var textures = new Dictionary<GltfMaterialHandle, string?>();
      foreach (var item in RequiredArray(value, "textureResourceBindings", "semanticOverrides.textureResourceBindings"))
      {
        EnsureProperties(item, "semanticOverrides.textureResourceBindings[]", "material", "resourceKey");
        var handle = new GltfMaterialHandle(RequiredInt32(item, "material", "semanticOverrides.textureResourceBindings[].material"));
        if (!textures.TryAdd(handle, OptionalNullableString(item, "resourceKey", "semanticOverrides.textureResourceBindings[].resourceKey")))
        {
          throw Malformed("semanticOverrides.textureResourceBindings", "A material handle is duplicated.");
        }
      }

      var roles = new Dictionary<GltfNodeHandle, GltfNewModelObjectRole>();
      foreach (var item in RequiredArray(value, "objectRoles", "semanticOverrides.objectRoles"))
      {
        EnsureProperties(item, "semanticOverrides.objectRoles[]", "node", "roles", "barrelMaximumAngle");
        var handle = new GltfNodeHandle(RequiredInt32(item, "node", "semanticOverrides.objectRoles[].node"));
        var roleValue = GltfStaticObjectRoles.None;
        foreach (var role in RequiredArray(item, "roles", "semanticOverrides.objectRoles[].roles"))
        {
          RequireKind(role, JsonValueKind.String, "semanticOverrides.objectRoles[].roles[]", "A role must be a string.");
          roleValue |= role.GetString() switch
          {
            "viewerFaced" => GltfStaticObjectRoles.ViewerFaced,
            "barrel" => GltfStaticObjectRoles.Barrel,
            "rotor" => GltfStaticObjectRoles.Rotor,
            _ => throw Malformed("semanticOverrides.objectRoles[].roles", "An object role is invalid.")
          };
        }
        var barrelAngle = RequiredByte(item, "barrelMaximumAngle", "semanticOverrides.objectRoles[].barrelMaximumAngle");
        if (!roles.TryAdd(handle, new GltfNewModelObjectRole(roleValue, barrelAngle)))
        {
          throw Malformed("semanticOverrides.objectRoles", "A node role handle is duplicated.");
        }
      }

      var helpers = new Dictionary<GltfNodeHandle, GltfNewModelHelperBinding>();
      foreach (var item in RequiredArray(value, "helperBindings", "semanticOverrides.helperBindings"))
      {
        EnsureProperties(item, "semanticOverrides.helperBindings[]", "node", "kind", "physicalNumber");
        var handle = new GltfNodeHandle(RequiredInt32(item, "node", "semanticOverrides.helperBindings[].node"));
        var kind = RequiredString(item, "kind", "semanticOverrides.helperBindings[].kind") switch
        {
          "attachment" => GltfNewModelHelperKind.Attachment,
          "cannon" => GltfNewModelHelperKind.Cannon,
          "spotLight" => GltfNewModelHelperKind.SpotLight,
          "omniLight" => GltfNewModelHelperKind.OmniLight,
          _ => throw Malformed("semanticOverrides.helperBindings[].kind", "A helper kind is invalid.")
        };
        var binding = new GltfNewModelHelperBinding(
          kind,
          RequiredInt32(item, "physicalNumber", "semanticOverrides.helperBindings[].physicalNumber"));
        if (!helpers.TryAdd(handle, binding))
        {
          throw Malformed("semanticOverrides.helperBindings", "A helper node handle is duplicated.");
        }
      }

      var lights = new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>();
      foreach (var item in RequiredArray(value, "staticLightOptions", "semanticOverrides.staticLightOptions"))
      {
        EnsureProperties(item, "semanticOverrides.staticLightOptions[]", "light", "targetDistance", "terrainLightAmplitude");
        var handle = new GltfLightHandle(RequiredInt32(item, "light", "semanticOverrides.staticLightOptions[].light"));
        var light = new GltfNewModelStaticLightOptions(
          OptionalNullableSingle(item, "targetDistance", "semanticOverrides.staticLightOptions[].targetDistance"),
          OptionalNullableSingle(item, "terrainLightAmplitude", "semanticOverrides.staticLightOptions[].terrainLightAmplitude"));
        if (!lights.TryAdd(handle, light))
        {
          throw Malformed("semanticOverrides.staticLightOptions", "A light handle is duplicated.");
        }
      }

      var animations = new Dictionary<GltfAnimationHandle, GltfNewModelAnimationClass>();
      foreach (var item in RequiredArray(value, "animationClasses", "semanticOverrides.animationClasses"))
      {
        EnsureProperties(item, "semanticOverrides.animationClasses[]", "animation", "class");
        var handle = new GltfAnimationHandle(RequiredInt32(item, "animation", "semanticOverrides.animationClasses[].animation"));
        var animationClass = RequiredString(item, "class", "semanticOverrides.animationClasses[].class") switch
        {
          "a" => GltfNewModelAnimationClass.A,
          "b" => GltfNewModelAnimationClass.B,
          "c" => GltfNewModelAnimationClass.C,
          "d" => GltfNewModelAnimationClass.D,
          _ => throw Malformed("semanticOverrides.animationClasses[].class", "An animation class is invalid.")
        };
        if (!animations.TryAdd(handle, animationClass))
        {
          throw Malformed("semanticOverrides.animationClasses", "An animation handle is duplicated.");
        }
      }

      GltfNewModelFootprint? footprint = null;
      var footprintValue = Required(value, "footprint", "semanticOverrides.footprint");
      if (footprintValue.ValueKind != JsonValueKind.Null)
      {
        EnsureProperties(footprintValue, "semanticOverrides.footprint", "presenceMask", "topElevations", "cornerPassageFlags");
        footprint = new GltfNewModelFootprint(
          RequiredUInt16(footprintValue, "presenceMask", "semanticOverrides.footprint.presenceMask"),
          RequiredArray(footprintValue, "topElevations", "semanticOverrides.footprint.topElevations")
            .Select((item, index) => RequiredSingle(item, $"semanticOverrides.footprint.topElevations[{index}]")),
          RequiredArray(footprintValue, "cornerPassageFlags", "semanticOverrides.footprint.cornerPassageFlags")
            .Select((item, index) => RequiredByte(item, $"semanticOverrides.footprint.cornerPassageFlags[{index}]")));
      }

      GltfNewModelHorizontalExtents? extents = null;
      var extentsValue = Required(value, "horizontalExtents", "semanticOverrides.horizontalExtents");
      if (extentsValue.ValueKind != JsonValueKind.Null)
      {
        EnsureProperties(extentsValue, "semanticOverrides.horizontalExtents", "positiveY", "negativeY", "positiveX", "negativeX");
        extents = new GltfNewModelHorizontalExtents(
          RequiredSingle(extentsValue, "positiveY", "semanticOverrides.horizontalExtents.positiveY"),
          RequiredSingle(extentsValue, "negativeY", "semanticOverrides.horizontalExtents.negativeY"),
          RequiredSingle(extentsValue, "positiveX", "semanticOverrides.horizontalExtents.positiveX"),
          RequiredSingle(extentsValue, "negativeX", "semanticOverrides.horizontalExtents.negativeX"));
      }

      return new GltfNewModelImportOptions(textures, footprint, extents, roles, helpers, lights, animations);
    }

    private static GltfEditImportOptions ParseConflictActions(JsonElement value, GltfOperationProfile profile)
    {
      RequireKind(value, JsonValueKind.Array, "conflictActions", "Conflict actions must be an array.");
      if (value.GetArrayLength() > profile.MaxMetadataConflicts)
      {
        throw LimitException("conflictActions", value.GetArrayLength(), profile.MaxMetadataConflicts);
      }
      var resolutions = new List<GltfMetadataConflictResolution>();
      var keys = new HashSet<string>(StringComparer.Ordinal);
      var index = 0;
      foreach (var item in value.EnumerateArray())
      {
        var path = $"conflictActions[{index}]";
        EnsureProperties(item, path, "conflictKey", "action", "targetNativePath");
        var key = RequiredString(item, "conflictKey", path + ".conflictKey");
        if (!IsConflictKey(key) || !keys.Add(key))
        {
          throw Malformed(path + ".conflictKey", "A conflict key is invalid or duplicated.");
        }
        var action = RequiredString(item, "action", path + ".action");
        var target = OptionalNullableString(item, "targetNativePath", path + ".targetNativePath");
        resolutions.Add(new GltfMetadataConflictResolution(key, action, target));
        index++;
      }
      return new GltfEditImportOptions(resolutions);
    }

    private static InterchangeBaseline ParseBaseline(JsonElement value, string path)
    {
      EnsureProperties(value, path, "assetLineageId", "documentId");
      return new InterchangeBaseline(
        Guid.ParseExact(RequiredString(value, "assetLineageId", path + ".assetLineageId"), "D"),
        Guid.ParseExact(RequiredString(value, "documentId", path + ".documentId"), "D"));
    }

    private static byte[] WritePlan(GltfImportPlan plan)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
      {
        writer.WriteStartObject();
        writer.WriteString("format", GltfImportPlanFormat.Identifier);
        writer.WriteNumber("version", GltfImportPlanFormat.Version);
        writer.WriteString("mode", plan.Kind == GltfImportPlanKind.NewModel ? "newModel" : "edit");
        writer.WriteString("package", PackageName(plan.PackageKind));
        writer.WriteString("sourceSha256", plan.SourceSha256);
        if (plan.Kind == GltfImportPlanKind.NewModel)
        {
          WriteOverrides(writer, plan.NewModelOptions!);
        }
        else
        {
          writer.WritePropertyName("expectedBaseline");
          WriteBaseline(writer, plan.ExpectedBaseline!);
          writer.WritePropertyName("conflictActions");
          writer.WriteStartArray();
          foreach (var resolution in plan.EditOptions!.ConflictResolutions)
          {
            writer.WriteStartObject();
            writer.WriteString("conflictKey", resolution.ConflictKey);
            writer.WriteString("action", resolution.Action);
            if (resolution.TargetNativePath is null)
            {
              writer.WriteNull("targetNativePath");
            }
            else
            {
              writer.WriteString("targetNativePath", resolution.TargetNativePath);
            }
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
        }
        writer.WriteEndObject();
      }
      stream.WriteByte((byte)'\n');
      return stream.ToArray();
    }

    private static void WriteOverrides(Utf8JsonWriter writer, GltfNewModelImportOptions options)
    {
      writer.WritePropertyName("semanticOverrides");
      writer.WriteStartObject();
      writer.WritePropertyName("textureResourceBindings");
      writer.WriteStartArray();
      foreach (var binding in options.TextureResourceBindings.OrderBy(item => item.Key.Value))
      {
        writer.WriteStartObject();
        writer.WriteNumber("material", binding.Key.Value);
        if (binding.Value is null)
        {
          writer.WriteNull("resourceKey");
        }
        else
        {
          writer.WriteString("resourceKey", binding.Value);
        }
        writer.WriteEndObject();
      }
      writer.WriteEndArray();
      writer.WritePropertyName("footprint");
      if (options.Footprint is null)
      {
        writer.WriteNullValue();
      }
      else
      {
        writer.WriteStartObject();
        writer.WriteNumber("presenceMask", options.Footprint.PresenceMask);
        WriteNumbers(writer, "topElevations", options.Footprint.TopElevations);
        WriteNumbers(writer, "cornerPassageFlags", options.Footprint.CornerPassageFlags);
        writer.WriteEndObject();
      }
      writer.WritePropertyName("horizontalExtents");
      if (options.HorizontalExtents is null)
      {
        writer.WriteNullValue();
      }
      else
      {
        writer.WriteStartObject();
        writer.WriteNumber("positiveY", options.HorizontalExtents.PositiveY);
        writer.WriteNumber("negativeY", options.HorizontalExtents.NegativeY);
        writer.WriteNumber("positiveX", options.HorizontalExtents.PositiveX);
        writer.WriteNumber("negativeX", options.HorizontalExtents.NegativeX);
        writer.WriteEndObject();
      }
      writer.WritePropertyName("objectRoles");
      writer.WriteStartArray();
      foreach (var role in options.ObjectRoles.OrderBy(item => item.Key.Value))
      {
        writer.WriteStartObject();
        writer.WriteNumber("node", role.Key.Value);
        writer.WritePropertyName("roles");
        writer.WriteStartArray();
        WriteRole(writer, role.Value.Roles, GltfStaticObjectRoles.ViewerFaced, "viewerFaced");
        WriteRole(writer, role.Value.Roles, GltfStaticObjectRoles.Barrel, "barrel");
        WriteRole(writer, role.Value.Roles, GltfStaticObjectRoles.Rotor, "rotor");
        writer.WriteEndArray();
        writer.WriteNumber("barrelMaximumAngle", role.Value.BarrelMaximumAngle);
        writer.WriteEndObject();
      }
      writer.WriteEndArray();
      writer.WritePropertyName("helperBindings");
      writer.WriteStartArray();
      foreach (var helper in options.HelperBindings.OrderBy(item => item.Key.Value))
      {
        writer.WriteStartObject();
        writer.WriteNumber("node", helper.Key.Value);
        writer.WriteString("kind", HelperName(helper.Value.Kind));
        writer.WriteNumber("physicalNumber", helper.Value.PhysicalNumber);
        writer.WriteEndObject();
      }
      writer.WriteEndArray();
      writer.WritePropertyName("staticLightOptions");
      writer.WriteStartArray();
      foreach (var light in options.StaticLightOptions.OrderBy(item => item.Key.Value))
      {
        writer.WriteStartObject();
        writer.WriteNumber("light", light.Key.Value);
        if (light.Value.TargetDistance.HasValue)
        {
          writer.WriteNumber("targetDistance", light.Value.TargetDistance.Value);
        }
        else
        {
          writer.WriteNull("targetDistance");
        }
        if (light.Value.TerrainLightAmplitude.HasValue)
        {
          writer.WriteNumber("terrainLightAmplitude", light.Value.TerrainLightAmplitude.Value);
        }
        else
        {
          writer.WriteNull("terrainLightAmplitude");
        }
        writer.WriteEndObject();
      }
      writer.WriteEndArray();
      writer.WritePropertyName("animationClasses");
      writer.WriteStartArray();
      foreach (var animation in options.AnimationClasses.OrderBy(item => item.Key.Value))
      {
        writer.WriteStartObject();
        writer.WriteNumber("animation", animation.Key.Value);
        writer.WriteString("class", animation.Value.ToString().ToLowerInvariant());
        writer.WriteEndObject();
      }
      writer.WriteEndArray();
      writer.WriteEndObject();
    }

    private static void WriteRole(Utf8JsonWriter writer, GltfStaticObjectRoles roles, GltfStaticObjectRoles role, string name)
    {
      if ((roles & role) != 0)
      {
        writer.WriteStringValue(name);
      }
    }

    private static void WriteNumbers(Utf8JsonWriter writer, string name, IEnumerable<float> values)
    {
      writer.WritePropertyName(name);
      writer.WriteStartArray();
      foreach (var value in values)
      {
        writer.WriteNumberValue(value);
      }
      writer.WriteEndArray();
    }

    private static void WriteNumbers(Utf8JsonWriter writer, string name, IEnumerable<byte> values)
    {
      writer.WritePropertyName(name);
      writer.WriteStartArray();
      foreach (var value in values)
      {
        writer.WriteNumberValue(value);
      }
      writer.WriteEndArray();
    }

    private static void ValidateJsonStructure(byte[] bytes, GltfOperationProfile profile)
    {
      if (bytes.Length > profile.MaxMetadataBytes)
      {
        throw LimitException("$", bytes.Length, profile.MaxMetadataBytes);
      }
      var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
      {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = int.MaxValue
      });
      var objects = new Stack<HashSet<string>>();
      var elements = 0;
      while (reader.Read())
      {
        if (reader.CurrentDepth > profile.MaxJsonDepth)
        {
          throw LimitException("$", reader.CurrentDepth, profile.MaxJsonDepth);
        }
        elements++;
        if (elements > profile.MaxMetadataElements)
        {
          throw LimitException("$", elements, profile.MaxMetadataElements);
        }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
          objects.Push(new HashSet<string>(StringComparer.Ordinal));
        }
        else if (reader.TokenType == JsonTokenType.EndObject)
        {
          objects.Pop();
        }
        else if (reader.TokenType == JsonTokenType.PropertyName)
        {
          var name = reader.GetString()!;
          if (objects.Count == 0 || !objects.Peek().Add(name))
          {
            throw Malformed("$", "The import plan contains a duplicate property.");
          }
        }
      }
    }

    private static async Task<byte[]> ReadBoundedAsync(Stream source, int maximum, CancellationToken cancellationToken)
    {
      using var output = new MemoryStream();
      var buffer = new byte[maximum == int.MaxValue ? 81920 : Math.Min(81920, maximum + 1)];
      while (true)
      {
        var read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
        if (read == 0)
        {
          break;
        }
        if (output.Length + read > maximum)
        {
          throw LimitException("$", output.Length + read, maximum);
        }
        output.Write(buffer, 0, read);
      }
      return output.ToArray();
    }

    private static JsonElement Required(JsonElement value, string property, string path)
    {
      if (!value.TryGetProperty(property, out var result))
      {
        throw Malformed(path, "A required member is absent.");
      }
      return result;
    }

    private static string RequiredString(JsonElement value, string property, string path)
    {
      var item = Required(value, property, path);
      RequireKind(item, JsonValueKind.String, path, "A string value is required.");
      return item.GetString()!;
    }

    private static string? OptionalNullableString(JsonElement value, string property, string path)
    {
      var item = Required(value, property, path);
      if (item.ValueKind == JsonValueKind.Null)
      {
        return null;
      }
      RequireKind(item, JsonValueKind.String, path, "A string or null value is required.");
      return item.GetString();
    }

    private static int RequiredInt32(JsonElement value, string property, string path)
    {
      var item = Required(value, property, path);
      if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var result))
      {
        throw Malformed(path, "An integer value is required.");
      }
      return result;
    }

    private static byte RequiredByte(JsonElement value, string property, string path) =>
      RequiredByte(Required(value, property, path), path);

    private static byte RequiredByte(JsonElement value, string path)
    {
      if (value.ValueKind != JsonValueKind.Number || !value.TryGetByte(out var result))
      {
        throw Malformed(path, "A byte value is required.");
      }
      return result;
    }

    private static ushort RequiredUInt16(JsonElement value, string property, string path)
    {
      var item = Required(value, property, path);
      if (item.ValueKind != JsonValueKind.Number || !item.TryGetUInt16(out var result))
      {
        throw Malformed(path, "An unsigned 16-bit value is required.");
      }
      return result;
    }

    private static float RequiredSingle(JsonElement value, string property, string path) =>
      RequiredSingle(Required(value, property, path), path);

    private static float RequiredSingle(JsonElement value, string path)
    {
      if (value.ValueKind != JsonValueKind.Number || !value.TryGetSingle(out var result) || !float.IsFinite(result))
      {
        throw Malformed(path, "A finite binary32 value is required.");
      }
      return result;
    }

    private static float? OptionalNullableSingle(JsonElement value, string property, string path)
    {
      var item = Required(value, property, path);
      return item.ValueKind == JsonValueKind.Null ? null : RequiredSingle(item, path);
    }

    private static IEnumerable<JsonElement> RequiredArray(JsonElement value, string property, string path)
    {
      var item = Required(value, property, path);
      RequireKind(item, JsonValueKind.Array, path, "An array value is required.");
      return item.EnumerateArray().ToArray();
    }

    private static void EnsureProperties(JsonElement value, string path, params string[] allowed)
    {
      RequireKind(value, JsonValueKind.Object, path, "An object value is required.");
      var names = new HashSet<string>(allowed, StringComparer.Ordinal);
      foreach (var property in value.EnumerateObject())
      {
        if (!names.Contains(property.Name))
        {
          throw Malformed(path + "." + property.Name, "The import plan contains an unsupported member.");
        }
      }
      foreach (var name in allowed)
      {
        if (!value.TryGetProperty(name, out _))
        {
          throw Malformed(path + "." + name, "A required member is absent.");
        }
      }
    }

    private static void RequireKind(JsonElement value, JsonValueKind kind, string path, string message)
    {
      if (value.ValueKind != kind)
      {
        throw Malformed(path, message);
      }
    }

    internal static bool IsConflictKey(string value)
    {
      return value.Length == 46
        && value.StartsWith("v1:", StringComparison.Ordinal)
        && value.Skip(3).All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    }

    private static ImportPlanException Malformed(string path, string message) =>
      new(GltfDiagnosticCodes.MalformedImportPlan, 3000, path, message);

    private static ImportPlanException LimitException(string path, long actual, int maximum) =>
      new(
        GltfDiagnosticCodes.ImportPlanResourceLimitExceeded,
        3002,
        path,
        "The import plan exceeds its finite operation profile.",
        new Dictionary<string, string>
        {
          ["actual"] = actual.ToString(CultureInfo.InvariantCulture),
          ["maximum"] = maximum.ToString(CultureInfo.InvariantCulture)
        });

    private static OperationResult<GltfImportPlan> Failed(
      string code,
      int eventId,
      string path,
      string message,
      IReadOnlyDictionary<string, string>? data = null)
    {
      return new OperationResult<GltfImportPlan>(
        code == GltfDiagnosticCodes.Cancelled ? OperationStatus.Cancelled : OperationStatus.Failed,
        diagnostics: new[] { Diagnostic(code, eventId, path, message, data) });
    }

    private static OperationResult<string> FailedString(
      string code,
      int eventId,
      string path,
      string message,
      IReadOnlyDictionary<string, string>? data = null)
    {
      return new OperationResult<string>(
        code == GltfDiagnosticCodes.Cancelled ? OperationStatus.Cancelled : OperationStatus.Failed,
        diagnostics: new[] { Diagnostic(code, eventId, path, message, data) });
    }

    private static OperationResult Limit(long actual, int maximum) =>
      new(OperationStatus.Failed, new[]
      {
        Diagnostic(
          GltfDiagnosticCodes.ImportPlanResourceLimitExceeded,
          3002,
          "$",
          "The import plan exceeds its finite operation profile.",
          new Dictionary<string, string>
          {
            ["actual"] = actual.ToString(CultureInfo.InvariantCulture),
            ["maximum"] = maximum.ToString(CultureInfo.InvariantCulture)
          })
      });

    private static OperationDiagnostic Diagnostic(
      string code,
      int eventId,
      string path,
      string message,
      IReadOnlyDictionary<string, string>? data = null) =>
      new(code, eventId, DiagnosticSeverity.Error, path, message, data: data);

    private static string PackageName(GltfPackageKind packageKind) =>
      packageKind == GltfPackageKind.Glb ? "glb" : "gltf";

    private static string HelperName(GltfNewModelHelperKind kind) => kind switch
    {
      GltfNewModelHelperKind.Attachment => "attachment",
      GltfNewModelHelperKind.Cannon => "cannon",
      GltfNewModelHelperKind.SpotLight => "spotLight",
      GltfNewModelHelperKind.OmniLight => "omniLight",
      _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static void WriteBaseline(Utf8JsonWriter writer, InterchangeBaseline baseline)
    {
      writer.WriteStartObject();
      writer.WriteString("assetLineageId", baseline.AssetLineageId.ToString("D"));
      writer.WriteString("documentId", baseline.DocumentId.ToString("D"));
      writer.WriteEndObject();
    }

    private sealed class ImportPlanException : Exception
    {
      internal string Code { get; }
      internal int EventId { get; }
      internal string Path { get; }
      internal IReadOnlyDictionary<string, string>? DiagnosticData { get; }

      internal ImportPlanException(
        string code,
        int eventId,
        string path,
        string message,
        IReadOnlyDictionary<string, string>? data = null)
        : base(message)
      {
        Code = code;
        EventId = eventId;
        Path = path;
        DiagnosticData = data;
      }
    }
  }

  /// <summary>Names operation kinds recorded by the CLI report protocol.</summary>
  public enum GltfCliReportOperationKind
  {
    /// <summary>MSH to glTF export.</summary>
    Export = 0,
    /// <summary>Expected-baseline edit import.</summary>
    ImportEdit = 1,
    /// <summary>Metadata-free new-model import.</summary>
    ImportNewModel = 2,
    /// <summary>Package validation without MSH materialization.</summary>
    Validate = 3
  }

  /// <summary>Contains one complete operation outcome for a machine report.</summary>
  public sealed class GltfCliReportOperation
  {
    /// <summary>Gets the caller-provided input identifier.</summary>
    public string Input { get; }
    /// <summary>Gets the requested destination, or null when the operation has none.</summary>
    public string? Destination { get; }
    /// <summary>Gets the operation kind.</summary>
    public GltfCliReportOperationKind Kind { get; }
    /// <summary>Gets the glTF package form.</summary>
    public GltfPackageKind PackageKind { get; }
    /// <summary>Gets the source or produced MSH asset kind, when available.</summary>
    public MeshAssetKind? AssetKind { get; }
    /// <summary>Gets the terminal operation status.</summary>
    public OperationStatus Status { get; }
    /// <summary>Gets every operation diagnostic in result order.</summary>
    public IReadOnlyList<OperationDiagnostic> Diagnostics { get; }
    /// <summary>Gets the produced or source MSH asset lineage identity.</summary>
    public Guid? MeshAssetLineageId { get; }
    /// <summary>Gets the produced or source MSH archive creation identity.</summary>
    public Guid? MeshCreationGuid { get; }
    /// <summary>Gets the caller-authorized edit baseline.</summary>
    public InterchangeBaseline? ExpectedBaseline { get; }
    /// <summary>Gets an emitted or initial interchange baseline.</summary>
    public InterchangeBaseline? Baseline { get; }
    /// <summary>Gets the rotated baseline after edit reconciliation.</summary>
    public InterchangeBaseline? NextBaseline { get; }
    /// <summary>Gets the emitted or applied native projection fingerprint.</summary>
    public NativeProjectionFingerprint? Fingerprint { get; }
    /// <summary>Gets how a successful edit treated its lineage.</summary>
    public GltfMetadataLineageDisposition? LineageDisposition { get; }
    /// <summary>Gets every conflict action committed by the operation.</summary>
    public IReadOnlyList<GltfMetadataConflictResolution> AppliedConflictActions { get; }
    /// <summary>Gets exact serialized paths restored from applicable metadata.</summary>
    public IReadOnlyList<string> RestoredSerializedRepresentationPaths { get; }
    /// <summary>Gets every preservation effect in operation order.</summary>
    public IReadOnlyList<PreservationChange> PreservationChanges { get; }

    private GltfCliReportOperation(
      string input,
      string? destination,
      GltfCliReportOperationKind kind,
      GltfPackageKind packageKind,
      OperationResult result,
      MeshAsset? meshAsset = null,
      InterchangeBaseline? expectedBaseline = null,
      InterchangeBaseline? baseline = null,
      InterchangeBaseline? nextBaseline = null,
      NativeProjectionFingerprint? fingerprint = null,
      GltfMetadataLineageDisposition? lineageDisposition = null,
      IEnumerable<GltfMetadataConflictResolution>? appliedConflictActions = null,
      IEnumerable<string>? restoredPaths = null,
      IEnumerable<PreservationChange>? preservationChanges = null)
    {
      Input = input ?? throw new ArgumentNullException(nameof(input));
      Destination = destination;
      if (!Enum.IsDefined(typeof(GltfPackageKind), packageKind))
      {
        throw new ArgumentOutOfRangeException(nameof(packageKind));
      }
      Kind = kind;
      PackageKind = packageKind;
      Status = result.Status;
      Diagnostics = Array.AsReadOnly(result.Diagnostics.ToArray());
      MeshAssetLineageId = meshAsset?.LineageId.Value;
      MeshCreationGuid = meshAsset?.ArchiveFraming.CreationGuid;
      AssetKind = meshAsset?.Kind;
      ExpectedBaseline = expectedBaseline;
      Baseline = baseline;
      NextBaseline = nextBaseline;
      Fingerprint = fingerprint;
      LineageDisposition = lineageDisposition;
      AppliedConflictActions = Array.AsReadOnly(appliedConflictActions?.ToArray() ?? Array.Empty<GltfMetadataConflictResolution>());
      RestoredSerializedRepresentationPaths = Array.AsReadOnly(restoredPaths?.ToArray() ?? Array.Empty<string>());
      PreservationChanges = Array.AsReadOnly(preservationChanges?.ToArray() ?? Array.Empty<PreservationChange>());
    }

    /// <summary>Captures one complete export outcome.</summary>
    public static GltfCliReportOperation ForExport(
      string input,
      string destination,
      GltfPackageKind packageKind,
      StaticMeshAsset asset,
      OperationResult<GltfExportReceipt> result)
    {
      if (result is null)
      {
        throw new ArgumentNullException(nameof(result));
      }
      return new GltfCliReportOperation(
        input,
        destination ?? throw new ArgumentNullException(nameof(destination)),
        GltfCliReportOperationKind.Export,
        packageKind,
        result,
        asset ?? throw new ArgumentNullException(nameof(asset)),
        baseline: result.Value?.Baseline,
        fingerprint: result.Value?.Fingerprint);
    }

    /// <summary>Captures one complete dynamic export outcome.</summary>
    public static GltfCliReportOperation ForExport(
      string input,
      string destination,
      GltfPackageKind packageKind,
      DynamicMeshAsset asset,
      OperationResult<GltfExportReceipt> result)
    {
      if (result is null)
      {
        throw new ArgumentNullException(nameof(result));
      }
      return new GltfCliReportOperation(
        input,
        destination ?? throw new ArgumentNullException(nameof(destination)),
        GltfCliReportOperationKind.Export,
        packageKind,
        result,
        asset ?? throw new ArgumentNullException(nameof(asset)),
        baseline: result.Value?.Baseline,
        fingerprint: result.Value?.Fingerprint);
    }

    /// <summary>Captures an export that failed before a static source asset was available.</summary>
    public static GltfCliReportOperation ForFailedExport(
      string input,
      string destination,
      GltfPackageKind packageKind,
      OperationResult<GltfExportReceipt> result)
    {
      if (result is null)
      {
        throw new ArgumentNullException(nameof(result));
      }
      if (result.Succeeded)
      {
        throw new ArgumentException("A failed export report requires a non-success result.", nameof(result));
      }
      return new GltfCliReportOperation(
        input,
        destination ?? throw new ArgumentNullException(nameof(destination)),
        GltfCliReportOperationKind.Export,
        packageKind,
        result);
    }

    /// <summary>Captures one complete new-model import outcome.</summary>
    public static GltfCliReportOperation ForNewModelImport(
      string input,
      string destination,
      GltfPackageKind packageKind,
      OperationResult<GltfNewModelImportResult> result)
    {
      if (result is null)
      {
        throw new ArgumentNullException(nameof(result));
      }
      return new GltfCliReportOperation(
        input,
        destination ?? throw new ArgumentNullException(nameof(destination)),
        GltfCliReportOperationKind.ImportNewModel,
        packageKind,
        result,
        result.Value?.Asset,
        baseline: result.Value?.Baseline,
        preservationChanges: result.Value?.Preservation.Changes);
    }

    /// <summary>Captures one complete expected-baseline edit-import outcome.</summary>
    public static GltfCliReportOperation ForEditImport(
      string input,
      string destination,
      GltfPackageKind packageKind,
      InterchangeBaseline expectedBaseline,
      OperationResult<GltfEditImportResult> result)
    {
      if (result is null)
      {
        throw new ArgumentNullException(nameof(result));
      }
      return new GltfCliReportOperation(
        input,
        destination ?? throw new ArgumentNullException(nameof(destination)),
        GltfCliReportOperationKind.ImportEdit,
        packageKind,
        result,
        result.Value?.Asset,
        expectedBaseline ?? throw new ArgumentNullException(nameof(expectedBaseline)),
        nextBaseline: result.Value?.NextBaseline,
        fingerprint: result.Value?.AppliedFingerprint,
        lineageDisposition: result.Value?.LineageDisposition,
        appliedConflictActions: result.Value?.AppliedConflictResolutions,
        restoredPaths: result.Value?.RestoredSerializedRepresentationPaths,
        preservationChanges: result.Value?.Preservation.Changes);
    }

    /// <summary>Captures one complete kind-neutral expected-baseline edit-import outcome.</summary>
    public static GltfCliReportOperation ForEditImport(
      string input,
      string destination,
      GltfPackageKind packageKind,
      InterchangeBaseline expectedBaseline,
      OperationResult<GltfMeshEditImportResult> result)
    {
      if (result is null)
      {
        throw new ArgumentNullException(nameof(result));
      }
      return new GltfCliReportOperation(
        input,
        destination ?? throw new ArgumentNullException(nameof(destination)),
        GltfCliReportOperationKind.ImportEdit,
        packageKind,
        result,
        result.Value?.Asset,
        expectedBaseline ?? throw new ArgumentNullException(nameof(expectedBaseline)),
        nextBaseline: result.Value?.NextBaseline,
        fingerprint: result.Value?.AppliedFingerprint,
        lineageDisposition: result.Value?.LineageDisposition,
        appliedConflictActions: result.Value?.AppliedConflictResolutions,
        restoredPaths: result.Value?.RestoredSerializedRepresentationPaths,
        preservationChanges: result.Value?.Preservation.Changes);
    }

    /// <summary>Captures one complete package-validation outcome.</summary>
    public static GltfCliReportOperation ForValidation(
      string input,
      GltfPackageKind packageKind,
      OperationResult result)
    {
      return new GltfCliReportOperation(
        input,
        null,
        GltfCliReportOperationKind.Validate,
        packageKind,
        result ?? throw new ArgumentNullException(nameof(result)));
    }
  }

  /// <summary>Contains one complete deterministic CLI machine report.</summary>
  public sealed class GltfCliReport
  {
    /// <summary>Gets the independent CLI-report format identifier.</summary>
    public string Format => GltfCliReportFormat.Identifier;
    /// <summary>Gets the independent CLI-report protocol version.</summary>
    public int Version => GltfCliReportFormat.Version;
    /// <summary>Gets the aggregate terminal status.</summary>
    public OperationStatus Status { get; }
    /// <summary>Gets every complete operation outcome in execution order.</summary>
    public IReadOnlyList<GltfCliReportOperation> Operations { get; }

    /// <summary>Creates one complete invocation report.</summary>
    public GltfCliReport(IEnumerable<GltfCliReportOperation> operations)
    {
      var values = operations?.ToArray() ?? throw new ArgumentNullException(nameof(operations));
      if (values.Any(operation => operation is null))
      {
        throw new ArgumentException("Report operations cannot contain null values.", nameof(operations));
      }
      Operations = Array.AsReadOnly(values);
      Status = values.Any(operation => operation.Status == OperationStatus.Failed)
        ? OperationStatus.Failed
        : values.Any(operation => operation.Status == OperationStatus.Cancelled)
          ? OperationStatus.Cancelled
          : OperationStatus.Succeeded;
    }
  }

  /// <summary>Writes deterministic version-1 CLI machine reports.</summary>
  public sealed class GltfCliReportSerializer
  {
    /// <summary>Writes one bounded deterministic version-1 machine report.</summary>
    public async Task<OperationResult> SerializeAsync(
      GltfCliReport report,
      Stream destination,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (report is null)
      {
        throw new ArgumentNullException(nameof(report));
      }
      if (destination is null)
      {
        throw new ArgumentNullException(nameof(destination));
      }
      profile ??= GltfOperationProfile.Default;
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = WriteReport(report);
        if (bytes.Length > profile.MaxOutputBytes)
        {
          return new OperationResult(OperationStatus.Failed, new[]
          {
            new OperationDiagnostic(
              GltfDiagnosticCodes.ResourceLimitExceeded,
              1101,
              DiagnosticSeverity.Error,
              "$",
              "The CLI report exceeds the configured output limit.",
              data: new Dictionary<string, string>
              {
                ["actual"] = bytes.Length.ToString(CultureInfo.InvariantCulture),
                ["maximum"] = profile.MaxOutputBytes.ToString(CultureInfo.InvariantCulture)
              })
          });
        }
        await destination.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
        return new OperationResult(OperationStatus.Succeeded);
      }
      catch (OperationCanceledException)
      {
        return new OperationResult(OperationStatus.Cancelled, new[]
        {
          new OperationDiagnostic(
            GltfDiagnosticCodes.Cancelled,
            1105,
            DiagnosticSeverity.Error,
            "$",
            "CLI report writing was cancelled.")
        });
      }
      catch (Exception ex)
      {
        return new OperationResult(OperationStatus.Failed, new[]
        {
          new OperationDiagnostic(
            GltfDiagnosticCodes.IoFailure,
            1104,
            DiagnosticSeverity.Error,
            "$",
            ex.Message)
        });
      }
    }

    private static byte[] WriteReport(GltfCliReport report)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
      {
        writer.WriteStartObject();
        writer.WriteString("format", GltfCliReportFormat.Identifier);
        writer.WriteNumber("version", GltfCliReportFormat.Version);
        writer.WriteString("status", StatusName(report.Status));
        writer.WritePropertyName("operations");
        writer.WriteStartArray();
        for (var index = 0; index < report.Operations.Count; index++)
        {
          WriteOperation(writer, index, report.Operations[index]);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
      }
      stream.WriteByte((byte)'\n');
      return stream.ToArray();
    }

    private static void WriteOperation(Utf8JsonWriter writer, int index, GltfCliReportOperation operation)
    {
      writer.WriteStartObject();
      writer.WriteNumber("index", index);
      writer.WriteString("input", operation.Input);
      writer.WriteString("kind", OperationName(operation.Kind));
      writer.WriteString("package", operation.PackageKind == GltfPackageKind.Glb ? "glb" : "gltf");
      if (operation.AssetKind.HasValue)
      {
        writer.WriteString(
          "assetKind",
          operation.AssetKind == MeshAssetKind.Static ? "static" : "dynamic");
      }
      else
      {
        writer.WriteNull("assetKind");
      }
      writer.WriteString("status", StatusName(operation.Status));
      if (operation.Destination is null)
      {
        writer.WriteNull("destination");
      }
      else
      {
        writer.WriteString("destination", operation.Destination);
      }
      writer.WritePropertyName("diagnostics");
      writer.WriteStartArray();
      foreach (var diagnostic in operation.Diagnostics)
      {
        WriteDiagnostic(writer, diagnostic);
      }
      writer.WriteEndArray();
      writer.WritePropertyName("identities");
      writer.WriteStartObject();
      WriteNullableGuid(writer, "meshAssetLineageId", operation.MeshAssetLineageId);
      WriteNullableGuid(writer, "meshCreationGuid", operation.MeshCreationGuid);
      WriteNullableBaseline(writer, "expectedBaseline", operation.ExpectedBaseline);
      WriteNullableBaseline(writer, "baseline", operation.Baseline);
      WriteNullableBaseline(writer, "nextBaseline", operation.NextBaseline);
      writer.WritePropertyName("fingerprint");
      if (operation.Fingerprint is null)
      {
        writer.WriteNullValue();
      }
      else
      {
        writer.WriteStartObject();
        writer.WriteString("name", operation.Fingerprint.Name);
        writer.WriteNumber("version", operation.Fingerprint.Version);
        writer.WriteString("sha256", operation.Fingerprint.Sha256);
        writer.WriteEndObject();
      }
      writer.WriteEndObject();
      if (operation.LineageDisposition.HasValue)
      {
        writer.WriteString("lineageDisposition", LineageName(operation.LineageDisposition.Value));
      }
      else
      {
        writer.WriteNull("lineageDisposition");
      }
      writer.WritePropertyName("appliedConflictActions");
      writer.WriteStartArray();
      foreach (var action in operation.AppliedConflictActions)
      {
        writer.WriteStartObject();
        writer.WriteString("conflictKey", action.ConflictKey);
        writer.WriteString("action", action.Action);
        if (action.TargetNativePath is null)
        {
          writer.WriteNull("targetNativePath");
        }
        else
        {
          writer.WriteString("targetNativePath", action.TargetNativePath);
        }
        writer.WriteEndObject();
      }
      writer.WriteEndArray();
      writer.WritePropertyName("preservation");
      writer.WriteStartObject();
      writer.WritePropertyName("restoredSerializedRepresentationPaths");
      writer.WriteStartArray();
      foreach (var path in operation.RestoredSerializedRepresentationPaths)
      {
        writer.WriteStringValue(path);
      }
      writer.WriteEndArray();
      writer.WritePropertyName("changes");
      writer.WriteStartArray();
      foreach (var change in operation.PreservationChanges)
      {
        writer.WriteStartObject();
        writer.WriteString("fieldPath", change.FieldPath);
        writer.WriteString("disposition", PreservationName(change.Disposition));
        writer.WriteString("reason", change.Reason);
        writer.WriteEndObject();
      }
      writer.WriteEndArray();
      writer.WriteEndObject();
      writer.WriteEndObject();
    }

    private static void WriteDiagnostic(Utf8JsonWriter writer, OperationDiagnostic diagnostic)
    {
      writer.WriteStartObject();
      writer.WriteString("code", diagnostic.Code);
      writer.WriteNumber("eventId", diagnostic.EventId);
      writer.WriteString("severity", diagnostic.Severity.ToString().ToLowerInvariant());
      writer.WriteString("path", diagnostic.Path);
      if (diagnostic.ByteOffset.HasValue)
      {
        writer.WriteNumber("byteOffset", diagnostic.ByteOffset.Value);
      }
      else
      {
        writer.WriteNull("byteOffset");
      }
      writer.WritePropertyName("data");
      writer.WriteStartObject();
      foreach (var item in diagnostic.Data.OrderBy(item => item.Key, StringComparer.Ordinal))
      {
        writer.WriteString(item.Key, item.Value);
      }
      writer.WriteEndObject();
      writer.WriteString("message", diagnostic.Message);
      writer.WriteEndObject();
    }

    private static void WriteNullableBaseline(Utf8JsonWriter writer, string name, InterchangeBaseline? baseline)
    {
      writer.WritePropertyName(name);
      if (baseline is null)
      {
        writer.WriteNullValue();
        return;
      }
      writer.WriteStartObject();
      writer.WriteString("assetLineageId", baseline.AssetLineageId.ToString("D"));
      writer.WriteString("documentId", baseline.DocumentId.ToString("D"));
      writer.WriteEndObject();
    }

    private static void WriteNullableGuid(Utf8JsonWriter writer, string name, Guid? value)
    {
      if (value.HasValue)
      {
        writer.WriteString(name, value.Value.ToString("D"));
      }
      else
      {
        writer.WriteNull(name);
      }
    }

    private static string StatusName(OperationStatus status) => status switch
    {
      OperationStatus.Succeeded => "succeeded",
      OperationStatus.Failed => "failed",
      OperationStatus.Cancelled => "cancelled",
      _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static string OperationName(GltfCliReportOperationKind kind) => kind switch
    {
      GltfCliReportOperationKind.Export => "export",
      GltfCliReportOperationKind.ImportEdit => "importEdit",
      GltfCliReportOperationKind.ImportNewModel => "importNewModel",
      GltfCliReportOperationKind.Validate => "validate",
      _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string LineageName(GltfMetadataLineageDisposition disposition) => disposition switch
    {
      GltfMetadataLineageDisposition.Retained => "retained",
      GltfMetadataLineageDisposition.BranchAccepted => "branchAccepted",
      GltfMetadataLineageDisposition.AdoptedAsNew => "adoptedAsNew",
      GltfMetadataLineageDisposition.Discarded => "discarded",
      _ => throw new ArgumentOutOfRangeException(nameof(disposition))
    };

    private static string PreservationName(PreservationDisposition disposition) => disposition switch
    {
      PreservationDisposition.Retained => "retained",
      PreservationDisposition.Regenerated => "regenerated",
      PreservationDisposition.Invalidated => "invalidated",
      PreservationDisposition.Canonicalized => "canonicalized",
      _ => throw new ArgumentOutOfRangeException(nameof(disposition))
    };
  }
}
