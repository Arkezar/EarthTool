#nullable enable

using EarthTool.MSH.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace EarthTool.GLTF.Internal
{
  internal sealed class AnimationProjectionSet
  {
    internal IReadOnlyList<ProjectedAnimationClip> Clips { get; }

    internal IReadOnlyList<ProjectedAnimationObject> Objects { get; }

    internal AnimationProjectionSet(
      IReadOnlyList<ProjectedAnimationClip> clips,
      IReadOnlyList<ProjectedAnimationObject> objects)
    {
      Clips = clips;
      Objects = objects;
    }
  }

  internal sealed class ProjectedAnimationClip
  {
    internal int ClassIndex { get; }

    internal string Name => $"EarthTool {(char)('A' + ClassIndex)}";

    internal IReadOnlyList<ProjectedAnimationObject> Objects { get; }

    internal int FrameCount => Objects[0].Frames.Count;

    internal string Fingerprint { get; }

    internal ProjectedAnimationClip(
      int classIndex,
      IReadOnlyList<ProjectedAnimationObject> objects,
      string fingerprint)
    {
      ClassIndex = classIndex;
      Objects = objects;
      Fingerprint = fingerprint;
    }
  }

  internal sealed class ProjectedAnimationObject
  {
    internal int SourceObjectLocalId { get; }

    internal uint AnimationClassValue { get; }

    internal int ClassIndex { get; }

    internal byte DeclaredLength { get; }

    internal IReadOnlyList<ProjectedAnimationFrame> Frames { get; }

    internal int? FailureFrame { get; }

    internal bool HasSourceTracks { get; }

    internal bool IsNative => HasSourceTracks && !FailureFrame.HasValue;

    internal string? Fingerprint { get; }

    internal StaticAnimationTracks SourceTracks { get; }

    internal ProjectedAnimationObject(
      int sourceObjectLocalId,
      uint animationClassValue,
      int classIndex,
      byte declaredLength,
      IReadOnlyList<ProjectedAnimationFrame> frames,
      int? failureFrame,
      string? fingerprint,
      bool hasSourceTracks,
      StaticAnimationTracks sourceTracks)
    {
      SourceObjectLocalId = sourceObjectLocalId;
      AnimationClassValue = animationClassValue;
      ClassIndex = classIndex;
      DeclaredLength = declaredLength;
      Frames = frames;
      FailureFrame = failureFrame;
      Fingerprint = fingerprint;
      HasSourceTracks = hasSourceTracks;
      SourceTracks = sourceTracks;
    }
  }

  internal readonly struct ProjectedAnimationFrame
  {
    internal Vector3 Translation { get; }

    internal Quaternion Rotation { get; }

    internal Vector3 Scale { get; }

    internal ProjectedAnimationFrame(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
      Translation = translation;
      Rotation = rotation;
      Scale = scale;
    }
  }

  internal static class StaticAnimationProjection
  {
    private static readonly Matrix4x4 MshToGltfBasis = new Matrix4x4(
      1, 0, 0, 0,
      0, 0, -1, 0,
      0, 1, 0, 0,
      0, 0, 0, 1);

    private static readonly Matrix4x4 GltfToMshBasis = Matrix4x4.Transpose(MshToGltfBasis);

    private static readonly Matrix4x4 StoredYAxisReflection = Matrix4x4.CreateScale(1, -1, 1);

    internal static AnimationProjectionSet Create(
      StaticMeshAsset asset,
      InterchangeBaseline baseline)
    {
      var records = asset.StaticRenderObjectSequence.ToDictionary(record => record.Id);
      var objects = new List<ProjectedAnimationObject>();
      foreach (var source in StaticSourceObjectTraversal.Flatten(asset.RootSourceObject))
      {
        var record = records[source.StaticRenderObjectIds[0]];
        var tracks = record.AnimationTracks;
        var hasSourceTracks = tracks.ScaleFrames.Count > 0
          || tracks.TranslationFrames.Count > 0
          || tracks.Matrices.Count > 0;
        var classIndex = (int)(record.AnimationClassValue & 3);
        var declaredLength = GetLength(asset.CommonBaseHeader.AnimationLengths, classIndex);
        var frameCount = declaredLength == 0 ? 1 : declaredLength;
        var frames = new List<ProjectedAnimationFrame>(frameCount);
        int? failureFrame = null;
        for (var frame = 0; hasSourceTracks && frame < frameCount; frame++)
        {
          if (!TryProjectFrame(record, frame, out var projected))
          {
            failureFrame = frame;
            frames.Clear();
            break;
          }
          frames.Add(projected);
        }

        var fingerprint = !hasSourceTracks || failureFrame.HasValue
          ? null
          : AnimationProjectionFingerprint.CreateObject(
            baseline,
            source.Id.Value,
            classIndex,
            declaredLength,
            frames.Select(frame => Canonicalize(
              frame.Translation,
              frame.Rotation,
              frame.Scale)).ToArray());
        objects.Add(new ProjectedAnimationObject(
          source.Id.Value,
          record.AnimationClassValue,
          classIndex,
          declaredLength,
          frames.AsReadOnly(),
          failureFrame,
          fingerprint,
          hasSourceTracks,
          tracks));
      }

      var clips = objects.Where(item => item.IsNative)
        .GroupBy(item => item.ClassIndex)
        .OrderBy(group => group.Key)
        .Select(group =>
        {
          var participants = group.OrderBy(item => item.SourceObjectLocalId).ToArray();
          return new ProjectedAnimationClip(
            group.Key,
            Array.AsReadOnly(participants),
            AnimationProjectionFingerprint.CreateClip(baseline, group.Key, participants));
        })
        .ToArray();
      return new AnimationProjectionSet(Array.AsReadOnly(clips), objects.AsReadOnly());
    }

    internal static ProjectedAnimationFrame Canonicalize(
      Vector3 translation,
      Quaternion rotation,
      Vector3 scale)
    {
      if (!IsFinite(translation) || !IsFinite(scale) || !IsFinite(rotation))
      {
        throw new InvalidDataException("Animation TRS contains a non-finite value.");
      }
      var lengthSquared = rotation.LengthSquared();
      if (!float.IsFinite(lengthSquared) || lengthSquared == 0)
      {
        throw new InvalidDataException("Animation rotation cannot be normalized.");
      }
      return new ProjectedAnimationFrame(
        NormalizeZero(translation),
        Canonicalize(Quaternion.Normalize(rotation)),
        NormalizeZero(scale));
    }

    internal static ProjectedAnimationFrame Canonicalize(Matrix4x4 transform)
    {
      if (!IsFinite(transform)
        || transform.M14 != 0
        || transform.M24 != 0
        || transform.M34 != 0
        || transform.M44 != 1
        || !Matrix4x4.Decompose(transform, out var scale, out var rotation, out var translation))
      {
        throw new InvalidDataException("Animation transform is not finite decomposable affine TRS.");
      }
      var candidate = Canonicalize(translation, rotation, scale);
      var recomposed = Matrix4x4.CreateScale(candidate.Scale)
        * Matrix4x4.CreateFromQuaternion(candidate.Rotation)
        * Matrix4x4.CreateTranslation(candidate.Translation);
      if (!ApproximatelyEqual(transform, recomposed))
      {
        throw new InvalidDataException("Animation transform contains unsupported matrix components.");
      }
      return candidate;
    }

    internal static byte[] SerializeScaleFrames(StaticAnimationTracks tracks)
    {
      return SerializeVectors(tracks.ScaleFrames, false);
    }

    internal static byte[] SerializeTranslationFrames(StaticAnimationTracks tracks)
    {
      return SerializeVectors(tracks.TranslationFrames, true);
    }

    internal static byte[] SerializeMatrices(StaticAnimationTracks tracks)
    {
      using var stream = new MemoryStream();
      using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
      {
        foreach (var matrix in tracks.Matrices)
        {
          foreach (var value in Values(matrix))
          {
            writer.Write(value);
          }
        }
      }
      return stream.ToArray();
    }

    internal static StaticAnimationTracks CreateCanonicalTracks(
      IReadOnlyList<ProjectedAnimationFrame> frames)
    {
      var scales = new Vector3[frames.Count];
      var translations = new Vector3[frames.Count];
      var matrices = new Matrix4x4[frames.Count];
      for (var index = 0; index < frames.Count; index++)
      {
        var frame = frames[index];
        var gltfTransform = Matrix4x4.CreateScale(frame.Scale)
          * Matrix4x4.CreateFromQuaternion(frame.Rotation)
          * Matrix4x4.CreateTranslation(frame.Translation);
        var mshTransform = MshToGltfBasis * gltfTransform * GltfToMshBasis;
        if (!Matrix4x4.Decompose(
          mshTransform,
          out var scale,
          out var rotation,
          out var translation))
        {
          throw new InvalidDataException("Animation TRS cannot be converted to canonical MSH tracks.");
        }
        scales[index] = NormalizeZero(scale);
        translations[index] = NormalizeZero(translation);
        var logicalRotation = Matrix4x4.CreateFromQuaternion(
          Canonicalize(Quaternion.Normalize(rotation)));
        matrices[index] = StoredYAxisReflection * logicalRotation * StoredYAxisReflection;
      }
      return new StaticAnimationTracks(scales, translations, matrices);
    }

    private static byte[] SerializeVectors(IReadOnlyList<Vector3> values, bool invertY)
    {
      using var stream = new MemoryStream();
      using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
      {
        foreach (var value in values)
        {
          writer.Write(value.X);
          writer.Write(invertY ? -value.Y : value.Y);
          writer.Write(value.Z);
        }
      }
      return stream.ToArray();
    }

    private static bool TryProjectFrame(
      StaticRenderObject record,
      int frame,
      out ProjectedAnimationFrame projected)
    {
      projected = default;
      var tracks = record.AnimationTracks;
      if (tracks.ScaleFrames.Count > 0 && frame >= tracks.ScaleFrames.Count
        || tracks.TranslationFrames.Count > 0 && frame >= tracks.TranslationFrames.Count
        || tracks.Matrices.Count > 0 && frame >= tracks.Matrices.Count)
      {
        return false;
      }

      var scale = tracks.ScaleFrames.Count == 0 ? Vector3.One : tracks.ScaleFrames[frame];
      var translation = tracks.TranslationFrames.Count == 0 ? record.Pivot : tracks.TranslationFrames[frame];
      var storedMatrix = tracks.Matrices.Count == 0 ? Matrix4x4.Identity : tracks.Matrices[frame];
      var matrix = StoredYAxisReflection * storedMatrix * StoredYAxisReflection;
      var mshTransform = Matrix4x4.CreateScale(scale)
        * matrix
        * Matrix4x4.CreateTranslation(translation);
      var gltfTransform = GltfToMshBasis * mshTransform * MshToGltfBasis;
      try
      {
        projected = Canonicalize(gltfTransform);
        return true;
      }
      catch (InvalidDataException)
      {
        return false;
      }
    }

    private static byte GetLength(AnimationClassBytes lengths, int classIndex)
    {
      return classIndex switch
      {
        0 => lengths.A,
        1 => lengths.B,
        2 => lengths.C,
        _ => lengths.D
      };
    }

    private static Quaternion Canonicalize(Quaternion value)
    {
      if (value.W < 0
        || value.W == 0 && value.X < 0
        || value.W == 0 && value.X == 0 && value.Y < 0
        || value.W == 0 && value.X == 0 && value.Y == 0 && value.Z < 0)
      {
        value = new Quaternion(-value.X, -value.Y, -value.Z, -value.W);
      }
      return new Quaternion(
        NormalizeZero(value.X),
        NormalizeZero(value.Y),
        NormalizeZero(value.Z),
        NormalizeZero(value.W));
    }

    private static Vector3 NormalizeZero(Vector3 value)
    {
      return new Vector3(
        NormalizeZero(value.X),
        NormalizeZero(value.Y),
        NormalizeZero(value.Z));
    }

    private static float NormalizeZero(float value)
    {
      return value == 0 ? 0 : value;
    }

    private static bool ApproximatelyEqual(Matrix4x4 expected, Matrix4x4 actual)
    {
      var expectedValues = Values(expected);
      var actualValues = Values(actual);
      return expectedValues.Zip(actualValues, (left, right) => ApproximatelyEqual(left, right)).All(equal => equal);
    }

    private static bool ApproximatelyEqual(float expected, float actual)
    {
      var tolerance = Math.Max(1e-6, 8d * Math.Max(Ulp(expected), Ulp(actual)));
      return Math.Abs((double)expected - actual) <= tolerance;
    }

    private static double Ulp(float value)
    {
      value = Math.Abs(value);
      if (value == 0)
      {
        return float.Epsilon;
      }
      var bits = BitConverter.SingleToInt32Bits(value);
      var adjacent = bits == 0x7F7FFFFF
        ? BitConverter.Int32BitsToSingle(bits - 1)
        : BitConverter.Int32BitsToSingle(bits + 1);
      return Math.Abs((double)adjacent - value);
    }

    private static float[] Values(Matrix4x4 value)
    {
      return new[]
      {
        value.M11, value.M12, value.M13, value.M14,
        value.M21, value.M22, value.M23, value.M24,
        value.M31, value.M32, value.M33, value.M34,
        value.M41, value.M42, value.M43, value.M44
      };
    }

    private static bool IsFinite(Matrix4x4 value)
    {
      return Values(value).All(float.IsFinite);
    }

    private static bool IsFinite(Vector3 value)
    {
      return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }

    private static bool IsFinite(Quaternion value)
    {
      return float.IsFinite(value.X)
        && float.IsFinite(value.Y)
        && float.IsFinite(value.Z)
        && float.IsFinite(value.W);
    }
  }

  internal static class AnimationProjectionFingerprint
  {
    internal static string CreateObject(
      InterchangeBaseline baseline,
      int sourceObjectLocalId,
      int classIndex,
      byte declaredLength,
      IReadOnlyList<ProjectedAnimationFrame> frames)
    {
      using var stream = new MemoryStream();
      using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
      {
        WritePreamble(writer, baseline, "animationTrsV1");
        writer.Write(sourceObjectLocalId);
        writer.Write(classIndex);
        writer.Write(declaredLength);
        writer.Write(frames.Count);
        foreach (var frame in frames)
        {
          Write(writer, frame.Translation);
          Write(writer, frame.Rotation);
          Write(writer, frame.Scale);
        }
      }
      return Hash(stream.ToArray());
    }

    internal static string CreateClip(
      InterchangeBaseline baseline,
      int classIndex,
      IReadOnlyList<ProjectedAnimationObject> objects)
    {
      using var stream = new MemoryStream();
      using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
      {
        WritePreamble(writer, baseline, "animationClipV1");
        writer.Write(classIndex);
        foreach (var item in objects.OrderBy(item => item.SourceObjectLocalId))
        {
          writer.Write(item.SourceObjectLocalId);
          WriteString(writer, item.Fingerprint!);
        }
      }
      return Hash(stream.ToArray());
    }

    private static void WritePreamble(
      BinaryWriter writer,
      InterchangeBaseline baseline,
      string projection)
    {
      WriteString(writer, "earthtool.msh.gltf");
      writer.Write(1);
      WriteString(writer, projection);
      writer.Write(1);
      writer.Write(baseline.AssetLineageId.ToByteArray());
      writer.Write(baseline.DocumentId.ToByteArray());
    }

    private static void Write(BinaryWriter writer, Vector3 value)
    {
      Write(writer, value.X);
      Write(writer, value.Y);
      Write(writer, value.Z);
    }

    private static void Write(BinaryWriter writer, Quaternion value)
    {
      Write(writer, value.X);
      Write(writer, value.Y);
      Write(writer, value.Z);
      Write(writer, value.W);
    }

    private static void Write(BinaryWriter writer, float value)
    {
      writer.Write(value == 0 ? 0 : value);
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
      var bytes = Encoding.UTF8.GetBytes(value);
      writer.Write(bytes.Length);
      writer.Write(bytes);
    }

    private static string Hash(byte[] value)
    {
      using var sha256 = SHA256.Create();
      return BitConverter.ToString(sha256.ComputeHash(value)).Replace("-", string.Empty).ToLowerInvariant();
    }
  }
}
