#nullable enable

using EarthTool.MSH.Assets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EarthTool.MSH.Internal
{
  internal static class DynamicMeshDecoder
  {
    private const int BaseHeaderSize = 0x368;
    private const int DynamicFixedSize = 0x404;
    private const int MinimumDynamicRecordSize = 0x410;

    internal static MshDecodeResult Decode(
      MshDecodeContext context,
      MeshArchiveFraming framing,
      int baseOffset,
      MeshAssetLineageId lineageId)
    {
      var objectCount = 0;
      var stringBytes = 0;
      var rootDynamicObject = DecodeObject(
        context,
        baseOffset,
        1,
        "RootDynamicObject",
        ref objectCount,
        ref stringBytes,
        out var payloadEnd);
      var trailingLength = context.Data.Length - payloadEnd;
      if (trailingLength > context.Profile.MaxRootTrailingBytes)
      {
        throw context.ResourceLimit(
          "RootTrailingBytes",
          payloadEnd,
          trailingLength,
          context.Profile.MaxRootTrailingBytes);
      }

      var rootTrailingBytes = context.Data.Slice(payloadEnd, trailingLength).ToArray();
      if (trailingLength != 0)
      {
        context.AddDiagnostic(
          context.Compatibility(
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
        framing,
        rootDynamicObject.CommonBaseHeader,
        rootDynamicObject,
        rootTrailingBytes,
        context.Source,
        MeshAssetOrigin.Loaded);
      return context.Complete(asset);
    }

    private static DynamicObject DecodeObject(
      MshDecodeContext context,
      int objectOffset,
      int depth,
      string path,
      ref int objectCount,
      ref int stringBytes,
      out int payloadEnd)
    {
      context.ThrowIfCancellationRequested();
      var profile = context.Profile;
      var data = context.Data;
      if (depth > profile.MaxDynamicDepth)
      {
        throw context.ResourceLimit(path, objectOffset, depth, profile.MaxDynamicDepth);
      }

      objectCount++;
      if (objectCount > profile.MaxDynamicObjects)
      {
        throw context.ResourceLimit(path, objectOffset, objectCount, profile.MaxDynamicObjects);
      }

      if (depth > 1)
      {
        var headerPath = path + ".CommonBaseHeader";
        context.Ensure(objectOffset, BaseHeaderSize, headerPath);
        if (!data.Slice(objectOffset, 4).SequenceEqual(new byte[] { (byte)'M', (byte)'E', (byte)'S', (byte)'H' }))
        {
          throw context.Structural(headerPath + ".Magic", objectOffset, "Expected MESH.");
        }

        var version = context.ReadUInt32(objectOffset + 4);
        if (version != 1)
        {
          throw context.Structural(
            headerPath + ".Version",
            objectOffset + 4,
            "A dynamic child must use MSH version 1.");
        }

        var meshKind = context.ReadUInt32(objectOffset + 8);
        if (meshKind != 1)
        {
          throw context.Structural(
            headerPath + ".MeshKind",
            objectOffset + 8,
            "A declared dynamic child must have dynamic mesh kind.");
        }
      }

      context.Ensure(objectOffset, DynamicFixedSize, path + ".Extension");
      var fixedExtension = data.Slice(objectOffset + BaseHeaderSize, 0x9C).ToArray();
      var cursor = objectOffset + DynamicFixedSize;
      var meshName = ReadBytes(
        context,
        ref cursor,
        path + ".Extension.MeshNameBytes",
        ref stringBytes);
      var texturePath = ReadBytes(
        context,
        ref cursor,
        path + ".Extension.TexturePathBytes",
        ref stringBytes);
      var extension = new DynamicEffectExtension(fixedExtension, meshName, texturePath);
      AddCompatibilityDiagnostics(
        context,
        data.Slice(objectOffset, BaseHeaderSize),
        extension,
        path,
        objectOffset,
        depth == 1);

      var childrenPath = path + ".Children";
      context.Ensure(cursor, sizeof(uint), childrenPath);
      var childCount = context.ReadUInt32(cursor);
      var childCountOffset = cursor;
      cursor += sizeof(uint);
      if (childCount > profile.MaxDynamicChildrenPerObject)
      {
        throw context.ResourceLimit(
          childrenPath,
          childCountOffset,
          childCount,
          profile.MaxDynamicChildrenPerObject);
      }

      var remainingObjectCount = profile.MaxDynamicObjects - objectCount;
      if (childCount > remainingObjectCount)
      {
        throw context.ResourceLimit(
          childrenPath,
          childCountOffset,
          (long)objectCount + childCount,
          profile.MaxDynamicObjects);
      }

      var minimumChildBytes = (long)childCount * MinimumDynamicRecordSize;
      if (minimumChildBytes > data.Length - cursor)
      {
        throw context.Structural(
          childCount == 0 ? childrenPath : childrenPath + "[0]",
          cursor,
          "The declared dynamic children do not fit in the serialized representation.");
      }

      var children = new DynamicObject[(int)childCount];
      for (var index = 0; index < children.Length; index++)
      {
        var childPath = childrenPath + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        children[index] = DecodeObject(
          context,
          cursor,
          depth + 1,
          childPath,
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

    private static void AddCompatibilityDiagnostics(
      MshDecodeContext context,
      ReadOnlySpan<byte> commonHeader,
      DynamicEffectExtension extension,
      string path,
      int objectOffset,
      bool isRoot)
    {
      var extensionOffset = objectOffset + BaseHeaderSize;
      var canonicalHeader = MshCanonicalSerializer.CreateCanonicalCommonHeader(
        1,
        new AnimationClassBytes(),
        Array.Empty<Authoring.CanonicalStaticVertex>());
      if (!commonHeader.SequenceEqual(canonicalHeader))
      {
        context.AddDiagnosticBounded(
          context.Compatibility(
            path + ".CommonBaseHeader",
            objectOffset,
            "A noncanonical inherited dynamic base header was preserved.",
            new Dictionary<string, string>()));
      }

      var behaviorFindings = DynamicEffectBehavior.Diagnose(
        extension,
        isRoot ? DynamicObjectPlacement.Root : DynamicObjectPlacement.Child);
      foreach (var finding in behaviorFindings.TakeWhile(IsEffectIdentityFinding))
      {
        context.AddDiagnosticBounded(
          finding.At(path, extensionOffset + GetDynamicBehaviorOffset(finding.Field)));
      }

      if (extension.ReservedWord != 0)
      {
        context.AddDiagnosticBounded(
          context.Compatibility(
            path + ".Extension.ReservedWord",
            extensionOffset + 0x4C,
            "A nonzero reserved dynamic word was preserved.",
            new Dictionary<string, string>
            {
              ["actual"] = $"0x{extension.ReservedWord:X8}",
              ["expected"] = "0x00000000"
            }));
      }

      foreach (var finding in behaviorFindings.SkipWhile(IsEffectIdentityFinding))
      {
        context.AddDiagnosticBounded(
          finding.At(path, extensionOffset + GetDynamicBehaviorOffset(finding.Field)));
      }
    }

    private static bool IsEffectIdentityFinding(DynamicBehaviorFinding finding)
    {
      return finding.Field is DynamicBehaviorField.EffectType or DynamicBehaviorField.LightType;
    }

    private static int GetDynamicBehaviorOffset(DynamicBehaviorField field)
    {
      return field switch
      {
        DynamicBehaviorField.LightType => 0x04,
        DynamicBehaviorField.Frames => 0x08,
        DynamicBehaviorField.SpriteSheet => 0x10,
        DynamicBehaviorField.AdditiveFlag => 0x50,
        DynamicBehaviorField.AlphaTimingMode => 0x70,
        DynamicBehaviorField.ChildTranslation => 0x84,
        _ => 0
      };
    }

    private static byte[] ReadBytes(
      MshDecodeContext context,
      ref int cursor,
      string path,
      ref int stringBytes)
    {
      context.Ensure(cursor, sizeof(uint), path);
      var lengthOffset = cursor;
      var length = context.ReadUInt32(cursor);
      cursor += sizeof(uint);
      var remainingStringBytes = context.Profile.MaxDynamicStringBytes - stringBytes;
      if (length > remainingStringBytes)
      {
        throw context.ResourceLimit(
          path,
          lengthOffset,
          (long)stringBytes + length,
          context.Profile.MaxDynamicStringBytes);
      }

      if (length > int.MaxValue)
      {
        throw context.ResourceLimit(
          path,
          lengthOffset,
          length,
          context.Profile.MaxDynamicStringBytes);
      }

      context.Ensure(cursor, (int)length, path);
      var result = context.Data.Slice(cursor, (int)length).ToArray();
      cursor += (int)length;
      stringBytes += (int)length;
      return result;
    }
  }
}
