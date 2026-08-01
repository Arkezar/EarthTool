#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace EarthTool.MSH.Internal
{
  internal sealed class MshContentException : Exception
  {
    internal OperationDiagnostic Diagnostic { get; }

    internal MshContentException(OperationDiagnostic diagnostic)
      : base(diagnostic.Message)
    {
      Diagnostic = diagnostic;
    }
  }

  internal sealed class MshDecodeResult
  {
    internal MeshAsset Asset { get; }
    internal IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    internal MshDecodeResult(MeshAsset asset, IReadOnlyList<OperationDiagnostic> diagnostics)
    {
      Asset = asset;
      Diagnostics = diagnostics;
    }
  }

  internal static class MshV1Decoder
  {
    private const uint ArchiveSignature = 0x00D0A1FF;
    private const uint ArchiveTypeFlag = 0x10000000;
    private const uint CreationGuidFlag = 0x20000000;
    private const uint KnownDeclarationBits = 0x30FFFFFF;
    private const int BaseHeaderSize = 0x368;
    private const int StaticRecordSize = 0xDD;
    private const int DynamicFixedSize = 0x404;
    private const int MinimumDynamicRecordSize = 0x410;

    internal static MshDecodeResult Decode(
      byte[] source,
      MshOperationProfile profile,
      CancellationToken cancellationToken,
      MeshAssetLineageId? lineageId = null,
      MeshAssetOrigin origin = MeshAssetOrigin.Loaded,
      int staticRenderObjectLocalId = 1,
      int rootSourceObjectLocalId = 1,
      IReadOnlyList<int>? staticRenderObjectLocalIds = null,
      IReadOnlyList<int>? sourceObjectLocalIds = null,
      int? nextStaticRenderObjectLocalId = null,
      int? nextSourceObjectLocalId = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var data = source.AsSpan();
      if (data.Length < sizeof(uint))
      {
        throw Failure(
          MshDiagnosticCodes.InvalidFraming,
          1000,
          "ArchiveFraming.Declaration",
          0,
          "The archive framing declaration is truncated.");
      }

      var diagnostics = new List<OperationDiagnostic>();
      var declaration = ReadUInt32(data, 0);
      if ((declaration & 0x00FFFFFF) != ArchiveSignature)
      {
        throw Failure(
          MshDiagnosticCodes.InvalidFraming,
          1000,
          "ArchiveFraming.Declaration",
          0,
          "The archive framing signature is invalid.");
      }

      var unknownDeclarationBits = declaration & ~KnownDeclarationBits;
      if (unknownDeclarationBits != 0)
      {
        diagnostics.Add(Compatibility(
          "ArchiveFraming.Declaration",
          0,
          "Unknown archive declaration bits were preserved.",
          new Dictionary<string, string>
          {
            ["unknownBits"] = $"0x{unknownDeclarationBits:X8}"
          }));
      }

      var cursor = sizeof(uint);
      uint? archiveType = null;
      if ((declaration & ArchiveTypeFlag) != 0)
      {
        Ensure(data, cursor, sizeof(uint), "ArchiveFraming.ArchiveType");
        archiveType = ReadUInt32(data, cursor);
        cursor += sizeof(uint);
      }

      Guid? creationGuid = null;
      if ((declaration & CreationGuidFlag) != 0)
      {
        Ensure(data, cursor, 16, "ArchiveFraming.CreationGuid");
        creationGuid = new Guid(data.Slice(cursor, 16).ToArray());
        cursor += 16;
      }

      var baseOffset = cursor;
      Ensure(data, baseOffset, BaseHeaderSize, "BaseHeader");
      var baseHeader = data.Slice(baseOffset, BaseHeaderSize);
      if (!baseHeader.Slice(0, 4).SequenceEqual(new byte[] { (byte)'M', (byte)'E', (byte)'S', (byte)'H' }))
      {
        throw Structural("BaseHeader.Magic", baseOffset, "Expected MESH.");
      }

      var version = ReadUInt32(baseHeader, 4);
      if (version != 1)
      {
        throw Failure(
          MshDiagnosticCodes.UnsupportedVersion,
          1001,
          "BaseHeader.Version",
          baseOffset + 4,
          $"Unsupported MSH version {version}.");
      }

      var meshKind = ReadUInt32(baseHeader, 8);
      if (meshKind > 1)
      {
        throw Failure(
          MshDiagnosticCodes.UnsupportedMeshKind,
          1002,
          "BaseHeader.MeshKind",
          baseOffset + 8,
          $"Unsupported root mesh kind {meshKind}.");
      }

      var archiveSelectsDynamic = archiveType.GetValueOrDefault() != 0;
      var meshKindIsDynamic = meshKind == 1;
      if (archiveSelectsDynamic != meshKindIsDynamic)
      {
        diagnostics.Add(Compatibility(
          "ArchiveFraming.ArchiveType",
          sizeof(uint),
          "Archive type and root mesh kind select different payload shapes.",
          new Dictionary<string, string>
          {
            ["archiveType"] = archiveType.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
            ["meshKind"] = meshKind.ToString(CultureInfo.InvariantCulture)
          }));
      }

      var assetLineageId = lineageId ?? new MeshAssetLineageId(Guid.NewGuid());
      if (meshKindIsDynamic)
      {
        return DecodeDynamic(
          source,
          data,
          profile,
          cancellationToken,
          diagnostics,
          declaration,
          archiveType,
          creationGuid,
          baseOffset,
          assetLineageId,
          origin);
      }

      cursor = baseOffset + BaseHeaderSize;
      Ensure(data, cursor, sizeof(uint), "StoredTrailingHierarchyUnwindCount");
      var storedTrailingUnwind = ReadUInt32(data, cursor);
      cursor += sizeof(uint);
      var commonBaseHeader = new CommonMeshBaseHeader(baseHeader.ToArray());
      var decodedRecords = new List<DecodedStaticRecord>();
      var absoluteVertexCount = 0;
      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (decodedRecords.Count == profile.MaxStaticRenderObjects)
        {
          throw ResourceLimit(
            "StaticRenderObjectSequence",
            cursor,
            (long)decodedRecords.Count + 1,
            profile.MaxStaticRenderObjects);
        }

        var record = DecodeRenderObject(
          data,
          cursor,
          decodedRecords.Count,
          absoluteVertexCount,
          commonBaseHeader,
          profile,
          diagnostics,
          out cursor);
        decodedRecords.Add(record);
        absoluteVertexCount = checked(absoluteVertexCount + record.RenderVertices.Count);
        if (record.NextRecordMarker == 0)
        {
          break;
        }
      }

      var hierarchy = ReconstructHierarchy(
        decodedRecords,
        assetLineageId,
        rootSourceObjectLocalId,
        sourceObjectLocalIds,
        profile);
      var expectedTrailingUnwind = checked((uint)hierarchy.FinalDepth + 1);
      if (storedTrailingUnwind != expectedTrailingUnwind)
      {
        throw Structural(
          "StoredTrailingHierarchyUnwindCount",
          baseOffset + BaseHeaderSize,
          $"Expected {expectedTrailingUnwind}, found {storedTrailingUnwind}.");
      }

      if (staticRenderObjectLocalIds is not null
        && staticRenderObjectLocalIds.Count != decodedRecords.Count)
      {
        throw new ArgumentException(
          "Static render-object identities must match the decoded sequence.",
          nameof(staticRenderObjectLocalIds));
      }

      var renderObjects = decodedRecords.Select((record, index) => new StaticRenderObject(
        new StaticRenderObjectId(
          assetLineageId,
          staticRenderObjectLocalIds?[index] ?? checked(staticRenderObjectLocalId + index)),
        hierarchy.RecordSourceIds[index],
        record.RenderVertices,
        record.Triangles,
        record.VertexBlockCount,
        record.VertexBlockPadding,
        record.ObjectFlags,
        record.TexturePathBytes,
        record.AnimationTracks,
        record.AnimationClassValue,
        record.Pivot,
        record.BarrelMaximumAngle,
        record.NextRecordMarker,
        record.SerializedRepresentation)).ToArray();
      hierarchy.AssignRenderObjectIds(renderObjects);
      var nextRenderObjectId = ResolveNextLocalId(
        renderObjects.Select(record => record.LocalId),
        nextStaticRenderObjectLocalId,
        nameof(nextStaticRenderObjectLocalId));
      var nextSourceId = ResolveNextLocalId(
        GetSourceObjectLocalIds(hierarchy.BuildRoot()),
        nextSourceObjectLocalId,
        nameof(nextSourceObjectLocalId));
      var payloadEnd = cursor;
      var trailingLength = data.Length - payloadEnd;
      if (trailingLength > profile.MaxRootTrailingBytes)
      {
        throw ResourceLimit(
          "RootTrailingBytes",
          payloadEnd,
          trailingLength,
          profile.MaxRootTrailingBytes);
      }

      var rootTrailingBytes = data.Slice(payloadEnd, trailingLength).ToArray();
      if (trailingLength != 0)
      {
        diagnostics.Add(Compatibility(
          "RootTrailingBytes",
          payloadEnd,
          "Opaque bytes after the complete root payload were preserved.",
          new Dictionary<string, string>
          {
            ["length"] = trailingLength.ToString(CultureInfo.InvariantCulture)
          }));
      }

      var asset = new StaticMeshAsset(
        assetLineageId,
        new MeshArchiveFraming(declaration, archiveType, creationGuid),
        commonBaseHeader,
        rootTrailingBytes,
        renderObjects,
        source,
        origin,
        hierarchy.BuildRoot(),
        storedTrailingUnwind,
        expectedTrailingUnwind,
        nextRenderObjectId,
        nextSourceId);
      return new MshDecodeResult(asset, CapDiagnostics(diagnostics, profile.MaxDiagnostics));
    }

    private static int? ResolveNextLocalId(
      IEnumerable<int> localIds,
      int? requested,
      string parameterName)
    {
      var maximum = localIds.Max();
      if (requested.HasValue && requested.Value <= maximum)
      {
        throw new ArgumentOutOfRangeException(
          parameterName,
          "The next lineage-local identity must exceed every allocated identity.");
      }
      return requested ?? (maximum == int.MaxValue ? null : maximum + 1);
    }

    private static IEnumerable<int> GetSourceObjectLocalIds(StaticSourceObject source)
    {
      yield return source.Id.Value;
      foreach (var child in source.Children)
      {
        foreach (var id in GetSourceObjectLocalIds(child))
        {
          yield return id;
        }
      }
    }

    private static MshDecodeResult DecodeDynamic(
      byte[] source,
      ReadOnlySpan<byte> data,
      MshOperationProfile profile,
      CancellationToken cancellationToken,
      List<OperationDiagnostic> diagnostics,
      uint declaration,
      uint? archiveType,
      Guid? creationGuid,
      int baseOffset,
      MeshAssetLineageId lineageId,
      MeshAssetOrigin origin)
    {
      var objectCount = 0;
      var stringBytes = 0;
      var rootDynamicObject = DecodeDynamicObject(
        data,
        baseOffset,
        1,
        "RootDynamicObject",
        profile,
        cancellationToken,
        diagnostics,
        ref objectCount,
        ref stringBytes,
        out var payloadEnd);
      var trailingLength = data.Length - payloadEnd;
      if (trailingLength > profile.MaxRootTrailingBytes)
      {
        throw ResourceLimit(
          "RootTrailingBytes",
          payloadEnd,
          trailingLength,
          profile.MaxRootTrailingBytes);
      }

      var rootTrailingBytes = data.Slice(payloadEnd, trailingLength).ToArray();
      if (trailingLength != 0)
      {
        diagnostics.Add(Compatibility(
          "RootTrailingBytes",
          payloadEnd,
          "Opaque bytes after the complete root payload were preserved.",
          new Dictionary<string, string>
          {
            ["length"] = trailingLength.ToString(CultureInfo.InvariantCulture)
          }));
      }

      var asset = new DynamicMeshAsset(
        lineageId,
        new MeshArchiveFraming(declaration, archiveType, creationGuid),
        rootDynamicObject.CommonBaseHeader,
        rootDynamicObject,
        rootTrailingBytes,
        source,
        origin);
      return new MshDecodeResult(asset, CapDiagnostics(diagnostics, profile.MaxDiagnostics));
    }

    private static DynamicObject DecodeDynamicObject(
      ReadOnlySpan<byte> data,
      int objectOffset,
      int depth,
      string path,
      MshOperationProfile profile,
      CancellationToken cancellationToken,
      List<OperationDiagnostic> diagnostics,
      ref int objectCount,
      ref int stringBytes,
      out int payloadEnd)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (depth > profile.MaxDynamicDepth)
      {
        throw ResourceLimit(path, objectOffset, depth, profile.MaxDynamicDepth);
      }

      objectCount++;
      if (objectCount > profile.MaxDynamicObjects)
      {
        throw ResourceLimit(path, objectOffset, objectCount, profile.MaxDynamicObjects);
      }

      var headerPath = path + ".CommonBaseHeader";
      Ensure(data, objectOffset, BaseHeaderSize, headerPath);
      if (!data.Slice(objectOffset, 4).SequenceEqual(new byte[] { (byte)'M', (byte)'E', (byte)'S', (byte)'H' }))
      {
        throw Structural(headerPath + ".Magic", objectOffset, "Expected MESH.");
      }

      var version = ReadUInt32(data, objectOffset + 4);
      if (version != 1)
      {
        throw Structural(
          headerPath + ".Version",
          objectOffset + 4,
          "A dynamic child must use MSH version 1.");
      }

      var meshKind = ReadUInt32(data, objectOffset + 8);
      if (meshKind != 1)
      {
        throw Structural(
          headerPath + ".MeshKind",
          objectOffset + 8,
          "A declared dynamic child must have dynamic mesh kind.");
      }

      Ensure(data, objectOffset, DynamicFixedSize, path + ".Extension");
      var fixedExtension = data.Slice(objectOffset + BaseHeaderSize, 0x9C).ToArray();
      var cursor = objectOffset + DynamicFixedSize;
      var meshName = ReadDynamicBytes(
        data,
        ref cursor,
        path + ".Extension.MeshNameBytes",
        profile,
        ref stringBytes);
      var texturePath = ReadDynamicBytes(
        data,
        ref cursor,
        path + ".Extension.TexturePathBytes",
        profile,
        ref stringBytes);
      var extension = new DynamicEffectExtension(fixedExtension, meshName, texturePath);
      AddDynamicCompatibilityDiagnostics(
        data.Slice(objectOffset, BaseHeaderSize),
        extension,
        path,
        objectOffset,
        depth == 1,
        profile,
        diagnostics);

      var childrenPath = path + ".Children";
      Ensure(data, cursor, sizeof(uint), childrenPath);
      var childCount = ReadUInt32(data, cursor);
      var childCountOffset = cursor;
      cursor += sizeof(uint);
      if (childCount > profile.MaxDynamicChildrenPerObject)
      {
        throw ResourceLimit(childrenPath, childCountOffset, childCount, profile.MaxDynamicChildrenPerObject);
      }

      var remainingObjectCount = profile.MaxDynamicObjects - objectCount;
      if (childCount > remainingObjectCount)
      {
        throw ResourceLimit(
          childrenPath,
          childCountOffset,
          (long)objectCount + childCount,
          profile.MaxDynamicObjects);
      }

      var minimumChildBytes = (long)childCount * MinimumDynamicRecordSize;
      if (minimumChildBytes > data.Length - cursor)
      {
        throw Structural(
          childCount == 0 ? childrenPath : childrenPath + "[0]",
          cursor,
          "The declared dynamic children do not fit in the serialized representation.");
      }

      var children = new DynamicObject[(int)childCount];
      for (var index = 0; index < children.Length; index++)
      {
        var childPath = childrenPath + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        children[index] = DecodeDynamicObject(
          data,
          cursor,
          depth + 1,
          childPath,
          profile,
          cancellationToken,
          diagnostics,
          ref objectCount,
          ref stringBytes,
          out cursor);
      }

      payloadEnd = cursor;
      return new DynamicObject(
        new CommonMeshBaseHeader(data.Slice(objectOffset, BaseHeaderSize).ToArray()),
        extension,
        children);
    }

    private static void AddDynamicCompatibilityDiagnostics(
      ReadOnlySpan<byte> commonHeader,
      DynamicEffectExtension extension,
      string path,
      int objectOffset,
      bool isRoot,
      MshOperationProfile profile,
      List<OperationDiagnostic> diagnostics)
    {
      var extensionOffset = objectOffset + BaseHeaderSize;
      var canonicalHeader = MshCanonicalSerializer.CreateCanonicalCommonHeader(
        1,
        new AnimationClassBytes(),
        Array.Empty<Authoring.CanonicalStaticVertex>());
      if (!commonHeader.SequenceEqual(canonicalHeader))
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".CommonBaseHeader",
          objectOffset,
          "A noncanonical inherited dynamic base header was preserved.",
          new Dictionary<string, string>()));
      }

      if (!extension.KnownEffectType.HasValue)
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension.EffectType",
          extensionOffset,
          "An unrecognized dynamic effect value was preserved.",
          new Dictionary<string, string>
          {
            ["actual"] = $"0x{extension.EffectType:X8}"
          }));
      }

      if (!extension.KnownLightType.HasValue)
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension.LightType",
          extensionOffset + 4,
          "An unrecognized dynamic light value was preserved.",
          new Dictionary<string, string>
          {
            ["actual"] = $"0x{extension.LightType:X8}"
          }));
      }

      if (extension.ReservedWord != 0)
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension.ReservedWord",
          extensionOffset + 0x4C,
          "A nonzero reserved dynamic word was preserved.",
          new Dictionary<string, string>
          {
            ["actual"] = $"0x{extension.ReservedWord:X8}",
            ["expected"] = "0x00000000"
          }));
      }

      if (extension.AdditiveFlag is not 0 and not 1)
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension.AdditiveFlag",
          extensionOffset + 0x50,
          "A noncanonical additive representation was preserved.",
          new Dictionary<string, string>
          {
            ["actual"] = extension.AdditiveFlag.ToString(CultureInfo.InvariantCulture)
          }));
      }

      if (extension.AlphaTimingMode is not 0 and not 1)
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension.AlphaTimingMode",
          extensionOffset + 0x70,
          "A noncanonical alpha timing representation was preserved.",
          new Dictionary<string, string>
          {
            ["actual"] = extension.AlphaTimingMode.ToString(CultureInfo.InvariantCulture)
          }));
      }

      if (HasUnsafeFrameDeclaration(extension))
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension.Frames",
          extensionOffset + 0x08,
          "A dynamic frame declaration outside the safe semantic-helper domain was preserved.",
          new Dictionary<string, string>()));
      }

      if (!HasCanonicalReciprocal(extension.SpriteSheetColumnCount, extension.ReciprocalColumnCount)
        || !HasCanonicalReciprocal(extension.SpriteSheetRowCount, extension.ReciprocalRowCount))
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension.SpriteSheet",
          extensionOffset + 0x10,
          "Independent sprite dimensions and reciprocal values disagree with canonical authoring.",
          new Dictionary<string, string>()));
      }

      if (HasNonFiniteSemanticValue(extension))
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension",
          extensionOffset,
          "A non-finite dynamic semantic representation was preserved.",
          new Dictionary<string, string>()));
      }

      if (HasNondefaultInertRepresentation(extension))
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension.InertRepresentations",
          extensionOffset,
          "Nondefault representations ignored by the selected effect were preserved.",
          new Dictionary<string, string>()));
      }

      if (isRoot && (extension.ChildStartTranslation != Vector3.Zero
        || extension.ChildEndTranslation != Vector3.Zero))
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".Extension.ChildTranslation",
          extensionOffset + 0x84,
          "A root child-translation representation that is not applied by the renderer was preserved.",
          new Dictionary<string, string>()));
      }
    }

    private static bool HasUnsafeFrameDeclaration(DynamicEffectExtension extension)
    {
      var allZero = extension.FirstSourceFrame == 0
        && extension.FrameCount == 0
        && extension.SpriteSheetColumnCount == 0
        && extension.SpriteSheetRowCount == 0
        && extension.FramePeriodTicks == 0;
      return !allZero && (extension.FirstSourceFrame < 0
        || extension.FrameCount <= 0
        || extension.FramePeriodTicks < 0);
    }

    private static bool HasCanonicalReciprocal(int count, float reciprocal)
    {
      var expected = count == 0 ? 0 : 1f / count;
      return BitConverter.SingleToInt32Bits(reciprocal) == BitConverter.SingleToInt32Bits(expected);
    }

    private static bool HasNonFiniteSemanticValue(DynamicEffectExtension extension)
    {
      return !IsFinite(extension.ReciprocalColumnCount)
        || !IsFinite(extension.ReciprocalRowCount)
        || !IsFinite(extension.StartEffectRectangle)
        || !IsFinite(extension.EndEffectRectangle)
        || !IsFinite(extension.EffectDepthOffset)
        || !IsFinite(extension.RibbonHalfWidth)
        || !IsFinite(extension.TerrainLightColor)
        || !IsFinite(extension.VisibleEffectColor)
        || !IsFinite(extension.VisibleTerrainLightGain)
        || !IsFinite(extension.StartAlpha)
        || !IsFinite(extension.EndAlpha)
        || !IsFinite(extension.StartModelScale)
        || !IsFinite(extension.EndModelScale)
        || !IsFinite(extension.ChildStartTranslation)
        || !IsFinite(extension.ChildEndTranslation);
    }

    private static bool HasNondefaultInertRepresentation(DynamicEffectExtension extension)
    {
      if (!extension.KnownEffectType.HasValue)
      {
        return false;
      }

      var effect = extension.KnownEffectType.Value;
      var inertLightType = effect is DynamicEffectType.Group
        or DynamicEffectType.Track
        or DynamicEffectType.LaserWall
        or DynamicEffectType.Shockwave
        or DynamicEffectType.Line
        or DynamicEffectType.Sphere
        or DynamicEffectType.ElectricalCannon
        or DynamicEffectType.Smoke
        or DynamicEffectType.Keelwater;
      var inertTerrainLightColor = effect is DynamicEffectType.Group
        or DynamicEffectType.Track
        or DynamicEffectType.Shockwave
        or DynamicEffectType.Line
        or DynamicEffectType.Sphere
        or DynamicEffectType.ElectricalCannon
        or DynamicEffectType.Smoke
        or DynamicEffectType.Keelwater;
      var inertSprite = effect is DynamicEffectType.Group or DynamicEffectType.Sphere;
      var inertAtlas = effect is DynamicEffectType.Group
        or DynamicEffectType.Track
        or DynamicEffectType.ScalableObject
        or DynamicEffectType.MappedExplosion
        or DynamicEffectType.Sphere;
      var inertRectangles = effect is DynamicEffectType.Group
        or DynamicEffectType.ScalableObject
        or DynamicEffectType.Laser
        or DynamicEffectType.LaserWall
        or DynamicEffectType.Sphere
        or DynamicEffectType.ElectricalCannon
        or DynamicEffectType.Lightning;
      var inertDepth = inertRectangles
        || effect is DynamicEffectType.Track or DynamicEffectType.MappedExplosion;
      var inertRibbon = effect is not DynamicEffectType.Laser
        and not DynamicEffectType.LaserWall
        and not DynamicEffectType.ElectricalCannon
        and not DynamicEffectType.Lightning;
      var inertVisibleColor = effect is DynamicEffectType.Group
        or DynamicEffectType.Track
        or DynamicEffectType.Keelwater;
      var inertVisibleGain = effect is not DynamicEffectType.Shockwave
        and not DynamicEffectType.Line
        and not DynamicEffectType.Smoke;
      var inertAlphaEndpoints = effect is DynamicEffectType.Group or DynamicEffectType.Sphere;
      var inertAlphaTiming = effect is DynamicEffectType.Group
        or DynamicEffectType.Shockwave
        or DynamicEffectType.Line
        or DynamicEffectType.Sphere
        or DynamicEffectType.Keelwater;
      var inertScale = effect != DynamicEffectType.ScalableObject;
      var inertMesh = effect != DynamicEffectType.ScalableObject;
      var defaultRectangle = new EffectRectangle(-0.25f, 0.25f, 0.25f, -0.25f);

      return (inertLightType && extension.LightType != 0)
        || (inertTerrainLightColor && extension.TerrainLightColor != Vector3.Zero)
        || (inertSprite && (extension.FirstSourceFrame != 0
          || extension.FrameCount != 0
          || extension.FramePeriodTicks != 0))
        || (inertAtlas && (extension.SpriteSheetColumnCount != 0
          || extension.SpriteSheetRowCount != 0
          || extension.ReciprocalColumnCount != 0
          || extension.ReciprocalRowCount != 0))
        || (inertRectangles && (!extension.StartEffectRectangle.Equals(defaultRectangle)
          || !extension.EndEffectRectangle.Equals(defaultRectangle)))
        || (inertDepth && extension.EffectDepthOffset != 0.25f)
        || (inertRibbon && extension.RibbonHalfWidth != 0.25f)
        || (inertVisibleColor && extension.VisibleEffectColor != Vector3.One)
        || (inertVisibleGain && extension.VisibleTerrainLightGain != 1f)
        || (inertAlphaTiming && extension.AlphaTimingMode != 0)
        || (inertAlphaEndpoints && (extension.StartAlpha != 1f
          || extension.EndAlpha != 1f))
        || (inertScale && (extension.StartModelScale != 0 || extension.EndModelScale != 0))
        || (inertMesh && extension.MeshNameBytes.Count != 0)
        || (effect == DynamicEffectType.Group && (extension.AdditiveFlag != 0
          || extension.TexturePathBytes.Count != 0));
    }

    private static bool IsFinite(EffectRectangle value)
    {
      return IsFinite(value.X0) && IsFinite(value.Y1) && IsFinite(value.X1) && IsFinite(value.Y0);
    }

    private static void AddCompatibilityBounded(
      List<OperationDiagnostic> diagnostics,
      MshOperationProfile profile,
      OperationDiagnostic diagnostic)
    {
      if (diagnostics.Count < profile.MaxDiagnostics)
      {
        diagnostics.Add(diagnostic);
      }
      else if (diagnostics.Count == profile.MaxDiagnostics
        && diagnostics[^1].Code != MshDiagnosticCodes.DiagnosticsTruncated)
      {
        diagnostics[^1] = new OperationDiagnostic(
          MshDiagnosticCodes.DiagnosticsTruncated,
          1010,
          DiagnosticSeverity.Warning,
          "$",
          "Additional diagnostics were suppressed by the operation profile.");
      }
    }

    private static byte[] ReadDynamicBytes(
      ReadOnlySpan<byte> data,
      ref int cursor,
      string path,
      MshOperationProfile profile,
      ref int stringBytes)
    {
      Ensure(data, cursor, sizeof(uint), path);
      var lengthOffset = cursor;
      var length = ReadUInt32(data, cursor);
      cursor += sizeof(uint);
      var remainingStringBytes = profile.MaxDynamicStringBytes - stringBytes;
      if (length > remainingStringBytes)
      {
        throw ResourceLimit(path, lengthOffset, (long)stringBytes + length, profile.MaxDynamicStringBytes);
      }

      if (length > int.MaxValue)
      {
        throw ResourceLimit(path, lengthOffset, length, profile.MaxDynamicStringBytes);
      }

      Ensure(data, cursor, (int)length, path);
      var result = data.Slice(cursor, (int)length).ToArray();
      cursor += (int)length;
      stringBytes += (int)length;
      return result;
    }

    private static DecodedStaticRecord DecodeRenderObject(
      ReadOnlySpan<byte> data,
      int recordOffset,
      int recordIndex,
      int absoluteVertexStart,
      CommonMeshBaseHeader commonHeader,
      MshOperationProfile profile,
      List<OperationDiagnostic> diagnostics,
      out int payloadEnd)
    {
      var path = $"StaticRenderObjectSequence[{recordIndex}]";
      Ensure(data, recordOffset, 8, path + ".RenderVertices");
      var vertexCount = ReadUInt32(data, recordOffset);
      var blockCount = ReadUInt32(data, recordOffset + 4);
      if (vertexCount > profile.MaxStaticVerticesPerObject)
      {
        throw ResourceLimit(
          path + ".RenderVertices",
          recordOffset,
          vertexCount,
          profile.MaxStaticVerticesPerObject);
      }

      if (blockCount > profile.MaxStaticVertexBlocksPerObject)
      {
        throw ResourceLimit(
          path + ".VertexBlockCount",
          recordOffset + 4,
          blockCount,
          profile.MaxStaticVertexBlocksPerObject);
      }

      var minimumBlockCount = (vertexCount + 3) / 4;
      if (blockCount < minimumBlockCount)
      {
        throw Structural(
          path + ".VertexBlockCount",
          recordOffset + 4,
          "The declared vertex blocks cannot contain all active render vertices.");
      }

      EnsureCounted(data, recordOffset + 8, blockCount, 0xA0, path + ".VertexBlocks");
      if (blockCount > minimumBlockCount)
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".VertexBlockCount",
          recordOffset + 4,
          "Excess physical vertex blocks were preserved as padding.",
          new Dictionary<string, string>()));
      }

      var vertices = new RenderVertex[(int)vertexCount];
      var vertexDataOffset = recordOffset + 8;
      for (var lane = 0; lane < vertices.Length; lane++)
      {
        var blockOffset = vertexDataOffset + lane / 4 * 0xA0;
        var laneOffset = lane % 4;
        var laneFloatOffset = laneOffset * sizeof(float);
        var position = new Vector3(
          ReadSingle(data, blockOffset + laneFloatOffset),
          -ReadSingle(data, blockOffset + 0x10 + laneFloatOffset),
          ReadSingle(data, blockOffset + 0x20 + laneFloatOffset));
        var normal = new Vector3(
          ReadSingle(data, blockOffset + 0x30 + laneFloatOffset),
          -ReadSingle(data, blockOffset + 0x40 + laneFloatOffset),
          ReadSingle(data, blockOffset + 0x50 + laneFloatOffset));
        var textureCoordinate = new Vector2(
          ReadSingle(data, blockOffset + 0x60 + laneFloatOffset),
          ReadSingle(data, blockOffset + 0x70 + laneFloatOffset));
        var reserved = ReadSingle(data, blockOffset + 0x80 + laneFloatOffset);
        var normalSharing = ReadUInt16(data, blockOffset + 0x90 + laneOffset * sizeof(ushort));
        var positionSharing = ReadUInt16(data, blockOffset + 0x98 + laneOffset * sizeof(ushort));
        ValidateSharingLink(normalSharing, absoluteVertexStart + lane, path, lane, "NormalSharingIndex", blockOffset);
        ValidateSharingLink(positionSharing, absoluteVertexStart + lane, path, lane, "PositionSharingIndex", blockOffset);
        vertices[lane] = new RenderVertex(
          position,
          normal,
          textureCoordinate,
          reserved,
          normalSharing,
          positionSharing);
      }

      var padding = new List<byte>();
      for (var lane = vertices.Length; lane < checked((int)blockCount * 4); lane++)
      {
        var blockOffset = vertexDataOffset + lane / 4 * 0xA0;
        var laneOffset = lane % 4;
        for (var channel = 0; channel < 9; channel++)
        {
          padding.AddRange(data.Slice(blockOffset + channel * 0x10 + laneOffset * 4, 4).ToArray());
        }

        padding.AddRange(data.Slice(blockOffset + 0x90 + laneOffset * 2, 2).ToArray());
        padding.AddRange(data.Slice(blockOffset + 0x98 + laneOffset * 2, 2).ToArray());
      }

      var cursor = checked(vertexDataOffset + (int)blockCount * 0xA0);
      Ensure(data, cursor, 8, path + ".ObjectFlags");
      var objectFlags = ReadUInt32(data, cursor);
      var unclassifiedFlags = objectFlags & 0xFFFF0000;
      if (unclassifiedFlags != 0)
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".UnclassifiedObjectFlagsHighWord",
          cursor + 2,
          "Unclassified object-flag bits were preserved.",
          new Dictionary<string, string> { ["actual"] = $"0x{unclassifiedFlags:X8}" }));
      }

      cursor += 4;
      var textureLengthOffset = cursor;
      var textureLength = ReadUInt32(data, cursor);
      cursor += 4;
      if (textureLength > profile.MaxStaticTexturePathBytes)
      {
        throw ResourceLimit(
          path + ".TexturePathBytes",
          textureLengthOffset,
          textureLength,
          profile.MaxStaticTexturePathBytes);
      }

      Ensure(data, cursor, (int)textureLength, path + ".TexturePathBytes");
      var texturePathBytes = data.Slice(cursor, (int)textureLength).ToArray();
      cursor += (int)textureLength;
      Ensure(data, cursor, 4, path + ".Triangles");
      var triangleCountOffset = cursor;
      var triangleCount = ReadUInt32(data, cursor);
      cursor += 4;
      if (triangleCount > profile.MaxStaticTrianglesPerObject)
      {
        throw ResourceLimit(
          path + ".Triangles",
          triangleCountOffset,
          triangleCount,
          profile.MaxStaticTrianglesPerObject);
      }

      EnsureCounted(data, cursor, triangleCount, 8, path + ".Triangles");
      var triangles = new StaticTriangle[(int)triangleCount];
      for (var index = 0; index < triangles.Length; index++)
      {
        var triangleOffset = cursor + index * 8;
        var triangle = new StaticTriangle(
          ReadUInt16(data, triangleOffset),
          ReadUInt16(data, triangleOffset + 2),
          ReadUInt16(data, triangleOffset + 4),
          ReadUInt16(data, triangleOffset + 6));
        if (triangle.Vertex0 >= vertexCount
          || triangle.Vertex1 >= vertexCount
          || triangle.Vertex2 >= vertexCount)
        {
          throw Structural(
            path + $".Triangles[{index}]",
            triangleOffset,
            "A triangle index is outside the active render-vertex range.");
        }

        triangles[index] = triangle;
      }

      cursor += checked((int)triangleCount * 8);
      var scaleFrames = ReadVectorTrack(data, ref cursor, path + ".AnimationTracks.ScaleFrames", false, profile);
      var translationFrames = ReadVectorTrack(
        data,
        ref cursor,
        path + ".AnimationTracks.TranslationFrames",
        true,
        profile);
      var matrices = ReadMatrixTrack(data, ref cursor, path + ".AnimationTracks.Matrices", profile);
      Ensure(data, cursor, 21, path + ".Transform");
      var animationClassValue = ReadUInt32(data, cursor);
      cursor += 4;
      ValidateTrackLengths(
        commonHeader,
        animationClassValue,
        scaleFrames.Count,
        translationFrames.Count,
        matrices.Count,
        path,
        recordOffset,
        profile,
        diagnostics);
      var pivot = ReadVector3(data, cursor, invertY: true);
      cursor += 12;
      var barrelMaximumAngle = data[cursor++];
      var nextRecordMarker = ReadUInt32(data, cursor);
      payloadEnd = cursor + sizeof(uint);
      return new DecodedStaticRecord(
        recordOffset,
        vertices,
        triangles,
        blockCount,
        padding,
        objectFlags,
        texturePathBytes,
        new StaticAnimationTracks(scaleFrames, translationFrames, matrices),
        animationClassValue,
        pivot,
        barrelMaximumAngle,
        nextRecordMarker,
        data.Slice(recordOffset, payloadEnd - recordOffset).ToArray());
    }

    private static IReadOnlyList<Vector3> ReadVectorTrack(
      ReadOnlySpan<byte> data,
      ref int cursor,
      string path,
      bool invertY,
      MshOperationProfile profile)
    {
      Ensure(data, cursor, 4, path);
      var countOffset = cursor;
      var count = ReadUInt32(data, cursor);
      cursor += 4;
      if (count > profile.MaxStaticAnimationFramesPerTrack)
      {
        throw ResourceLimit(path, countOffset, count, profile.MaxStaticAnimationFramesPerTrack);
      }

      EnsureCounted(data, cursor, count, 12, path);
      var result = new Vector3[(int)count];
      for (var index = 0; index < result.Length; index++)
      {
        result[index] = ReadVector3(data, cursor + index * 12, invertY);
      }

      cursor += checked((int)count * 12);
      return result;
    }

    private static IReadOnlyList<Matrix4x4> ReadMatrixTrack(
      ReadOnlySpan<byte> data,
      ref int cursor,
      string path,
      MshOperationProfile profile)
    {
      Ensure(data, cursor, 4, path);
      var countOffset = cursor;
      var count = ReadUInt32(data, cursor);
      cursor += 4;
      if (count > profile.MaxStaticAnimationFramesPerTrack)
      {
        throw ResourceLimit(path, countOffset, count, profile.MaxStaticAnimationFramesPerTrack);
      }

      EnsureCounted(data, cursor, count, 64, path);
      var result = new Matrix4x4[(int)count];
      for (var index = 0; index < result.Length; index++)
      {
        var offset = cursor + index * 64;
        result[index] = new Matrix4x4(
          ReadSingle(data, offset), ReadSingle(data, offset + 4),
          ReadSingle(data, offset + 8), ReadSingle(data, offset + 12),
          ReadSingle(data, offset + 16), ReadSingle(data, offset + 20),
          ReadSingle(data, offset + 24), ReadSingle(data, offset + 28),
          ReadSingle(data, offset + 32), ReadSingle(data, offset + 36),
          ReadSingle(data, offset + 40), ReadSingle(data, offset + 44),
          ReadSingle(data, offset + 48), ReadSingle(data, offset + 52),
          ReadSingle(data, offset + 56), ReadSingle(data, offset + 60));
      }

      cursor += checked((int)count * 64);
      return result;
    }

    private static Vector3 ReadVector3(ReadOnlySpan<byte> data, int offset, bool invertY)
    {
      var y = ReadSingle(data, offset + 4);
      return new Vector3(ReadSingle(data, offset), invertY ? -y : y, ReadSingle(data, offset + 8));
    }

    private static void ValidateTrackLengths(
      CommonMeshBaseHeader commonHeader,
      uint animationClassValue,
      int scaleCount,
      int translationCount,
      int matrixCount,
      string path,
      int recordOffset,
      MshOperationProfile profile,
      List<OperationDiagnostic> diagnostics)
    {
      var effectiveAnimationClass = animationClassValue & 3;
      var expected = effectiveAnimationClass switch
      {
        0 => commonHeader.AnimationLengths.A,
        1 => commonHeader.AnimationLengths.B,
        2 => commonHeader.AnimationLengths.C,
        _ => commonHeader.AnimationLengths.D
      };
      if (animationClassValue > 3)
      {
        AddCompatibilityBounded(diagnostics, profile, Compatibility(
          path + ".AnimationClassValue",
          recordOffset,
          "An unrecognized animation class was preserved.",
          new Dictionary<string, string>
          {
            ["actual"] = animationClassValue.ToString(CultureInfo.InvariantCulture)
          }));
      }

      foreach (var track in new[]
      {
        (Name: "ScaleFrames", Count: scaleCount),
        (Name: "TranslationFrames", Count: translationCount),
        (Name: "Matrices", Count: matrixCount)
      })
      {
        if (track.Count > 0 && track.Count < expected)
        {
          throw Structural(
            path + ".AnimationTracks." + track.Name,
            recordOffset,
            $"A present animation track has {track.Count} frames but class {animationClassValue} declares {expected}.");
        }

        if (track.Count > expected)
        {
          AddCompatibilityBounded(diagnostics, profile, Compatibility(
            path + ".AnimationTracks." + track.Name,
            recordOffset,
            "An animation track longer than its selected declaration was preserved.",
            new Dictionary<string, string>
            {
              ["actual"] = track.Count.ToString(CultureInfo.InvariantCulture),
              ["expected"] = expected.ToString(CultureInfo.InvariantCulture)
            }));
        }
      }
    }

    private static void ValidateSharingLink(
      ushort link,
      int absoluteVertexIndex,
      string path,
      int localVertexIndex,
      string field,
      int blockOffset)
    {
      if (link != ushort.MaxValue && link >= absoluteVertexIndex)
      {
        throw Structural(
          $"{path}.RenderVertices[{localVertexIndex}].{field}",
          blockOffset,
          "A shared vertex index must reference an earlier absolute render vertex.");
      }
    }

    private static StaticHierarchy ReconstructHierarchy(
      IReadOnlyList<DecodedStaticRecord> records,
      MeshAssetLineageId lineageId,
      int rootSourceObjectLocalId,
      IReadOnlyList<int>? sourceObjectLocalIds,
      MshOperationProfile profile)
    {
      if (records.Count == 0)
      {
        throw Structural("StaticRenderObjectSequence", 0, "At least one static render object is required.");
      }

      var sourceIndex = 0;
      var root = new StaticSourceBuilder(
        SourceId(lineageId, rootSourceObjectLocalId, sourceObjectLocalIds, sourceIndex++),
        null,
        0);
      var current = root;
      var recordSources = new SourceObjectId[records.Count];
      for (var index = 0; index < records.Count; index++)
      {
        var record = records[index];
        var unwind = (byte)record.ObjectFlags;
        var beginsNested = (record.ObjectFlags & (uint)StaticRenderObjectFlags.BeginsNestedSourceObject) != 0;
        if (index == 0 && (unwind != 0 || beginsNested))
        {
          throw Structural(
            "StaticRenderObjectSequence[0].ObjectFlags",
            record.RecordOffset,
            "The first static render object must establish the root source object.");
        }

        if (unwind > current.Depth)
        {
          throw Structural(
            $"StaticRenderObjectSequence[{index}].ObjectFlags",
            record.RecordOffset,
            "The hierarchy unwind exceeds the current source-object depth.");
        }

        for (var count = 0; count < unwind; count++)
        {
          current = current.Parent!;
        }

        if (beginsNested)
        {
          var depth = current.Depth + 1;
          if (depth + 1 > profile.MaxStaticHierarchyDepth)
          {
            throw ResourceLimit(
              $"StaticRenderObjectSequence[{index}].ObjectFlags",
              record.RecordOffset,
              depth + 1,
              profile.MaxStaticHierarchyDepth);
          }

          var child = new StaticSourceBuilder(
            SourceId(lineageId, rootSourceObjectLocalId, sourceObjectLocalIds, sourceIndex++),
            current,
            depth);
          current.Children.Add(child);
          current = child;
        }

        current.RecordIndices.Add(index);
        recordSources[index] = current.Id;
      }

      if (sourceObjectLocalIds is not null && sourceObjectLocalIds.Count != sourceIndex)
      {
        throw new ArgumentException(
          "Source-object identities must match the reconstructed hierarchy.",
          nameof(sourceObjectLocalIds));
      }

      return new StaticHierarchy(root, recordSources, current.Depth);
    }

    private static SourceObjectId SourceId(
      MeshAssetLineageId lineageId,
      int firstLocalId,
      IReadOnlyList<int>? sourceObjectLocalIds,
      int index)
    {
      return new SourceObjectId(
        lineageId,
        sourceObjectLocalIds is null
          ? checked(firstLocalId + index)
          : index < sourceObjectLocalIds.Count
            ? sourceObjectLocalIds[index]
            : throw new ArgumentException(
              "Source-object identities must match the reconstructed hierarchy.",
              nameof(sourceObjectLocalIds)));
    }

    private static void EnsureCounted(
      ReadOnlySpan<byte> data,
      int offset,
      uint count,
      int elementSize,
      string path)
    {
      var length = (long)count * elementSize;
      if (length > int.MaxValue || offset < 0 || offset > data.Length - length)
      {
        throw Structural(path, Math.Min(offset, data.Length), "The declared elements do not fit in the serialized representation.");
      }
    }

    private sealed class DecodedStaticRecord
    {
      internal int RecordOffset { get; }
      internal IReadOnlyList<RenderVertex> RenderVertices { get; }
      internal IReadOnlyList<StaticTriangle> Triangles { get; }
      internal uint VertexBlockCount { get; }
      internal IReadOnlyList<byte> VertexBlockPadding { get; }
      internal uint ObjectFlags { get; }
      internal IReadOnlyList<byte> TexturePathBytes { get; }
      internal StaticAnimationTracks AnimationTracks { get; }
      internal uint AnimationClassValue { get; }
      internal Vector3 Pivot { get; }
      internal byte BarrelMaximumAngle { get; }
      internal uint NextRecordMarker { get; }
      internal byte[] SerializedRepresentation { get; }

      internal DecodedStaticRecord(
        int recordOffset,
        IReadOnlyList<RenderVertex> renderVertices,
        IReadOnlyList<StaticTriangle> triangles,
        uint vertexBlockCount,
        IReadOnlyList<byte> vertexBlockPadding,
        uint objectFlags,
        IReadOnlyList<byte> texturePathBytes,
        StaticAnimationTracks animationTracks,
        uint animationClassValue,
        Vector3 pivot,
        byte barrelMaximumAngle,
        uint nextRecordMarker,
        byte[] serializedRepresentation)
      {
        RecordOffset = recordOffset;
        RenderVertices = renderVertices;
        Triangles = triangles;
        VertexBlockCount = vertexBlockCount;
        VertexBlockPadding = vertexBlockPadding;
        ObjectFlags = objectFlags;
        TexturePathBytes = texturePathBytes;
        AnimationTracks = animationTracks;
        AnimationClassValue = animationClassValue;
        Pivot = pivot;
        BarrelMaximumAngle = barrelMaximumAngle;
        NextRecordMarker = nextRecordMarker;
        SerializedRepresentation = serializedRepresentation;
      }
    }

    private sealed class StaticSourceBuilder
    {
      internal SourceObjectId Id { get; }
      internal StaticSourceBuilder? Parent { get; }
      internal int Depth { get; }
      internal List<int> RecordIndices { get; } = new List<int>();
      internal List<StaticRenderObjectId> RenderObjectIds { get; } = new List<StaticRenderObjectId>();
      internal List<StaticSourceBuilder> Children { get; } = new List<StaticSourceBuilder>();

      internal StaticSourceBuilder(SourceObjectId id, StaticSourceBuilder? parent, int depth)
      {
        Id = id;
        Parent = parent;
        Depth = depth;
      }

      internal StaticSourceObject Build()
      {
        return new StaticSourceObject(Id, RenderObjectIds, Children.Select(child => child.Build()));
      }
    }

    private sealed class StaticHierarchy
    {
      private readonly StaticSourceBuilder _root;

      internal IReadOnlyList<SourceObjectId> RecordSourceIds { get; }
      internal int FinalDepth { get; }

      internal StaticHierarchy(
        StaticSourceBuilder root,
        IReadOnlyList<SourceObjectId> recordSourceIds,
        int finalDepth)
      {
        _root = root;
        RecordSourceIds = recordSourceIds;
        FinalDepth = finalDepth;
      }

      internal void AssignRenderObjectIds(IReadOnlyList<StaticRenderObject> renderObjects)
      {
        Assign(_root, renderObjects);
      }

      internal StaticSourceObject BuildRoot()
      {
        return _root.Build();
      }

      private static void Assign(
        StaticSourceBuilder source,
        IReadOnlyList<StaticRenderObject> renderObjects)
      {
        source.RenderObjectIds.AddRange(source.RecordIndices.Select(index => renderObjects[index].Id));
        foreach (var child in source.Children)
        {
          Assign(child, renderObjects);
        }
      }
    }

    private static IReadOnlyList<OperationDiagnostic> CapDiagnostics(
      IReadOnlyList<OperationDiagnostic> diagnostics,
      int maximum)
    {
      if (diagnostics.Count <= maximum)
      {
        return diagnostics;
      }

      var retainedDiagnosticCount = maximum - 1;
      var suppressedDiagnosticCount = diagnostics.Count - retainedDiagnosticCount;
      var retained = diagnostics.Take(retainedDiagnosticCount).ToList();
      retained.Add(new OperationDiagnostic(
        MshDiagnosticCodes.DiagnosticsTruncated,
        1010,
        DiagnosticSeverity.Warning,
        "$",
        "Additional diagnostics were suppressed by the operation profile.",
        data: new Dictionary<string, string>
        {
          ["suppressed"] = suppressedDiagnosticCount.ToString(CultureInfo.InvariantCulture)
        }));
      return retained;
    }

    private static bool IsActiveVertexByte(int offset)
    {
      var channelOffset = offset % 0x10;
      if (offset < 0x90)
      {
        return channelOffset < 0x0C;
      }

      var sharingOffset = offset % 8;
      return sharingOffset < 6;
    }

    private static bool IsFinite(Vector3 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
    }

    private static bool IsFinite(Vector2 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y);
    }

    private static bool IsFinite(float value)
    {
      return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static void Ensure(ReadOnlySpan<byte> data, int offset, int length, string path)
    {
      if (offset < 0 || length < 0 || offset > data.Length - length)
      {
        throw Structural(path, Math.Min(offset, data.Length), "The serialized representation is truncated.");
      }
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
    }

    private static float ReadSingle(ReadOnlySpan<byte> data, int offset)
    {
      return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, 4)));
    }

    private static MshContentException Structural(string path, long offset, string message)
    {
      return Failure(MshDiagnosticCodes.StructuralHazard, 1003, path, offset, message);
    }

    private static MshContentException ResourceLimit(
      string path,
      long offset,
      long actual,
      long maximum)
    {
      return new MshContentException(new OperationDiagnostic(
        MshDiagnosticCodes.ResourceLimitExceeded,
        1004,
        DiagnosticSeverity.Error,
        path,
        "The serialized representation exceeds the configured operation profile.",
        offset,
        new Dictionary<string, string>
        {
          ["actual"] = actual.ToString(CultureInfo.InvariantCulture),
          ["maximum"] = maximum.ToString(CultureInfo.InvariantCulture)
        }));
    }

    private static MshContentException Unsupported(string domain, string path, long offset)
    {
      return new MshContentException(new OperationDiagnostic(
        MshDiagnosticCodes.UnsupportedDomain,
        1005,
        DiagnosticSeverity.Error,
        path,
        $"The {domain} domain is outside the current safe MSH slice.",
        offset,
        new Dictionary<string, string> { ["domain"] = domain }));
    }

    private static OperationDiagnostic Compatibility(
      string path,
      long offset,
      string message,
      IReadOnlyDictionary<string, string> data)
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.CompatibilityAnomaly,
        1009,
        DiagnosticSeverity.Warning,
        path,
        message,
        offset,
        data);
    }

    private static MshContentException Failure(
      string code,
      int eventId,
      string path,
      long offset,
      string message)
    {
      return new MshContentException(new OperationDiagnostic(
        code,
        eventId,
        DiagnosticSeverity.Error,
        path,
        message,
        offset));
    }
  }
}
