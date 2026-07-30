using Collada141;
using EarthTool.Common.Bases;
using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.DAE.Collections;
using EarthTool.DAE.Extensions;
using EarthTool.MSH.Enums;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Light = Collada141.Light;
using Vector = EarthTool.MSH.Models.Elements.Vector;

namespace EarthTool.DAE.Services
{
  public class ColladaMeshReader : Reader<IMesh>
  {
    private const int DefaultFootprintBoxIndex = 15;
    private const int FixedPointScale = 256;

    private readonly IEarthInfoFactory _earthInfoFactory;
    private readonly IHierarchyBuilder _hierarchyBuilder;
    private readonly Regex _regex;

    public ColladaMeshReader(IEarthInfoFactory earthInfoFactory, IHierarchyBuilder hierarchyBuilder)
    {
      _earthInfoFactory = earthInfoFactory;
      _hierarchyBuilder = hierarchyBuilder;
      _regex = new Regex(@$"Part-(\d+)-(\d+)");
    }

    public override FileType InputFileExtension => FileType.DAE;

    protected override IMesh InternalRead(string filePath)
    {
      var model = LoadModel(filePath);
      return Read(model);
    }

    private IMesh Read(COLLADA model)
    {
      var modelName = model.Library_Visual_Scenes.First().Visual_Scene.First().Node.First().Id;
      var earthInfo = _earthInfoFactory.Get(FileFlags.None, Guid.NewGuid());
      var geometries = LoadGeometries(model).ToArray();
      var partsTree = _hierarchyBuilder.GetPartsTree(geometries);
      var baseHeader = LoadBaseHeader(model, partsTree.Parts);

      return new EarthMesh()
      {
        FileHeader = earthInfo,
        BaseHeader = baseHeader,
        Geometries = geometries,
        PartsTree = partsTree
      };
    }

    private IEnumerable<IModelPart> LoadGeometries(COLLADA model)
    {
      var modelTree = new ModelTree(model).ToArray();
      var result = modelTree.Select((p, i) => LoadModelPart(p, i, modelTree.Length)).ToArray();
      return result;
    }

    private ModelPart LoadModelPart(ModelTreeNode node, int idx, int count)
    {
      var g = node.Geometry;
      var facesAndVertices = LoadFacesWithVertices(g);
      var faces = facesAndVertices.Faces.ToArray();
      var triangleFlags = node.ParseTriangleFlags(faces.Length);
      for (var i = 0; i < faces.Length; i++)
      {
        ((Face)faces[i]).Flags = triangleFlags[i];
      }
      var offset = new Vector() { Value = GetTransformationMatrix(node.TransformationMatrix).Translation };

      var details = node.ParseAnimationDetails();

      return new ModelPart()
      {
        Faces = faces,
        Vertices = facesAndVertices.Vertices,
        Animations = LoadAnimations(g, node.Model, offset),
        Texture = LoadTexture(node),
        NextRecordMarker = idx == count - 1 ? 0u : 1u,
        Offset = offset,
        BackTrackDepth = (byte)node.BacktrackLevel,
        PartType = details.PartType,
        AnimationType = details.AnimationType,
        RiseAngle = node.ParseBarrelMaximumAngle(details.PartType),
      };
    }

    private bool IsSubPart(string name)
    {
      var result = _regex.Match(name);
      return result.Success && int.Parse(result.Groups[2].Value) > 0;
    }

    private ITextureInfo LoadTexture(ModelTreeNode node)
    {
      var material = node.Materials.First();
      var effectId = material.Instance_Effect.Url.Substring(1);
      var effect = node.Model.Library_Effects.First().Effect.Single(e => e.Id == effectId);
      var effectProfile = effect.Fx_Profile_Abstract.OfType<Profile_COMMON>().First();
      var diffuseTextureId = effectProfile.Technique.Lambert.Diffuse.Texture.Texture;
      var samplerSourceId = effectProfile.Newparam.Single(p => p.Sid == diffuseTextureId).Sampler2D.Source;
      var sourceId = effectProfile.Newparam.Single(p => p.Sid == samplerSourceId).Surface.Init_From.First().Value;
      var texture = node.Model.Library_Images.First().Image.Single(i => i.Id == sourceId).Init_From;
      return new TextureInfo() { FileName = Path.Combine("Textures", Path.ChangeExtension(texture, "tex")) };
    }

    private IAnimations LoadAnimations(Geometry g, COLLADA model, IVector offset)
    {
      var animation = model.Library_Animations.FirstOrDefault()?.Animation.SingleOrDefault(a => a.Name == g.Name);
      var sourceId = animation?.AnimationProperty.FirstOrDefault()?.Sampler.FirstOrDefault()?.Input
        .Single(i => i.Semantic == "OUTPUT").Source;
      var source = animation?.AnimationProperty.FirstOrDefault()?.Source.SingleOrDefault(s => "#" + s.Id == sourceId);
      if (source != null)
      {
        var frames = source.Technique_Common.Accessor.Count;
        var data = source.Float_Array.Value.Split(' ')
          .Select(v => float.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture));

        var split = Enumerable.Range(0, (int)frames).Select(i => data.Skip(i * 16).Take(16).ToArray()).ToArray();
        var matrices = split.Select(f => new Matrix4x4(f[0], f[4], f[8], f[12], f[1], f[5], f[9], f[13], f[2], f[6],
          f[10], f[14], f[3], f[7], f[11], f[15])).ToArray();

        var tmpMovement = new List<Vector3>();
        var tmpRotations = new List<Matrix4x4>();
        foreach (var matrix in matrices)
        {
          Matrix4x4.Decompose(matrix, out _, out var rotation, out var translation);
          rotation.Y = -rotation.Y;
          tmpMovement.Add(translation);
          tmpRotations.Add(Matrix4x4.CreateFromQuaternion(rotation));
        }

        var movement = tmpMovement.Select(m => new Vector(m.X, m.Y, m.Z)).ToArray();
        var rotations = tmpRotations.Select(r => new RotationFrame() { TransformationMatrix = r }).ToArray();

        return new Animations()
        {
          TranslationFrames = tmpMovement.Distinct().Count() > 1 ? movement : Enumerable.Empty<IVector>(),
          RotationFrames = tmpRotations.Distinct().Count() > 1 ? rotations : Enumerable.Empty<IRotationFrame>()
        };
      }

      return new Animations();
    }

    private (IEnumerable<IFace> Faces, IEnumerable<IVertex> Vertices) LoadFacesWithVertices(Geometry g)
    {
      var vertexSource = g.Mesh.Vertices.Input.First(i => i.Semantic == "POSITION").Source.Trim('#');
      InputLocalOffset vertexInput;
      InputLocalOffset normalInput;
      InputLocalOffset uvMapInput;

      string polys;
      int polyCount;
      int vCount = 3;
      int inputCount;

      if (g.Mesh.TrianglesSpecified)
      {
        vertexInput = g.Mesh.Triangles.First().Input.Single(i => i.Semantic == "VERTEX");
        normalInput = g.Mesh.Triangles.First().Input.Single(i => i.Semantic == "NORMAL");
        uvMapInput = g.Mesh.Triangles.First().Input.Single(i => i.Semantic == "TEXCOORD");

        polys = g.Mesh.Triangles.First().P;
        polyCount = (int)g.Mesh.Triangles.First().Count;
        inputCount = g.Mesh.Triangles.First().Input.Sum(i => (int)i.Offset);
      }
      else if (g.Mesh.PolylistSpecified)
      {
        vertexInput = g.Mesh.Polylist.First().Input.Single(i => i.Semantic == "VERTEX");
        normalInput = g.Mesh.Polylist.First().Input.Single(i => i.Semantic == "NORMAL");
        uvMapInput = g.Mesh.Polylist.First().Input.Single(i => i.Semantic == "TEXCOORD");
        polys = g.Mesh.Polylist.First().P;
        polyCount = (int)g.Mesh.Polylist.First().Count;
        inputCount = g.Mesh.Polylist.First().Input.Sum(i => (int)i.Offset);
      }
      else
      {
        throw new NotSupportedException("Unsupported mesh type");
      }

      var normalSource = normalInput.Source.Trim('#');
      var uvMapSource = uvMapInput.Source.Trim('#');
      var vertexVectors = LoadVectors(g.Mesh.Source.Single(s => s.Id == vertexSource)).ToArray();
      var normalVector = LoadVectors(g.Mesh.Source.Single(s => s.Id == normalSource)).ToArray();
      var uvs = LoadUVs(g.Mesh.Source.Single(s => s.Id == uvMapSource)).ToArray();

      var faceValues = polys.Split(' ').Select(v => int.Parse(v));
      var faces = faceValues.Select((v, i) => new
      {
        Value = v,
        Group = i / new[] { vertexInput.Offset, normalInput.Offset, uvMapInput.Offset }.Distinct().Count()
      })
        .GroupBy(v => v.Group)
        .Select((v, i) => new { Face = v.Select(x => x.Value).ToArray(), Group = i / vCount })
        .GroupBy(v => v.Group)
        .Select(x => x.Select(v => v.Face.ToArray()).ToArray()).ToArray();

      var vertices = new List<Vertex>();
      foreach (var group in faces)
      {
        foreach (var face in group)
        {
          var position = vertexVectors[face[vertexInput.Offset]];
          var normal = normalVector[face[normalInput.Offset]];
          var uv = uvs[face[uvMapInput.Offset]];

          var positionId = vertices.IndexOf(vertices.FirstOrDefault(v => v.Position.Equals(position)));
          var normalId = vertices.IndexOf(vertices.FirstOrDefault(v => v.Normal.Equals(normal)));

          if (!vertices.Any(v => v.Position.Equals(position) && v.Normal.Equals(normal) && v.TextureCoordinate.Equals(uv)))
          {
            vertices.Add(new Vertex(position, normal, uv,
              normalId < 0 ? ushort.MaxValue : checked((ushort)normalId),
              positionId < 0 ? ushort.MaxValue : checked((ushort)positionId)));
          }
        }
      }

      var resultFaces = faces.Select(f => GetFace(f, vertices, vertexVectors, vertexInput.Offset, normalVector,
        normalInput.Offset, uvs, uvMapInput.Offset)).ToArray();
      return (resultFaces, vertices);
    }

    private Face GetFace(int[][] f, IList<Vertex> vertices, IVector[] vertexVectors, ulong vertexOffset,
      IVector[] normalVector, ulong normalOffset,
      ITextureCoordinate[] uvs, ulong uvOffset)
    {
      var v1 = vertices.Single(v =>
        v.Position.Equals(vertexVectors[f[0][vertexOffset]]) && v.Normal.Equals(normalVector[f[0][normalOffset]]) &&
        v.TextureCoordinate.Equals(uvs[f[0][uvOffset]]));
      var v2 = vertices.Single(v =>
        v.Position.Equals(vertexVectors[f[1][vertexOffset]]) && v.Normal.Equals(normalVector[f[1][normalOffset]]) &&
        v.TextureCoordinate.Equals(uvs[f[1][uvOffset]]));
      var v3 = vertices.Single(v =>
        v.Position.Equals(vertexVectors[f[2][vertexOffset]]) && v.Normal.Equals(normalVector[f[2][normalOffset]]) &&
        v.TextureCoordinate.Equals(uvs[f[2][uvOffset]]));

      return new Face()
      {
        V1 = checked((ushort)vertices.IndexOf(v1)),
        V2 = checked((ushort)vertices.IndexOf(v2)),
        V3 = checked((ushort)vertices.IndexOf(v3)),
        Flags = 1 // must be greater than 0?
      };
    }

    private ITextureCoordinate[] LoadUVs(Source source)
    {
      var values = source.Float_Array.Value.Split(' ').Select(v => float.Parse(v, CultureInfo.InvariantCulture));
      var groupSizes = source.Technique_Common.Accessor.Param.Count;
      return values.Select((v, i) => new { Value = v, Group = i / groupSizes }).GroupBy(v => v.Group)
        .Select(g => g.Select(v => v.Value))
        .Select(v => new TextureCoordinate(v.ElementAt(0), 1 - v.ElementAt(1))).ToArray();
    }

    private IVector[] LoadVectors(Source source)
    {
      var values = source.Float_Array.Value.Split(' ').Select(v => float.Parse(v, CultureInfo.InvariantCulture));
      var groupSizes = source.Technique_Common.Accessor.Param.Count;
      return values.Select((v, i) => new { Value = v, Group = i / groupSizes }).GroupBy(v => v.Group)
        .Select(g => g.Select(v => v.Value))
        .Select(v => new Vector(v.ElementAt(0), v.ElementAt(1), v.ElementAt(2))).ToArray();
    }

    private IMeshBaseHeader LoadBaseHeader(COLLADA model, IEnumerable<IModelPart> geometries)
    {
      var spotLights = LoadStaticLights(model, true);
      var omniLights = LoadStaticLights(model, false);
      return new MeshBaseHeader()
      {
        MeshKind = MeshKind.Static, // dynamic not supported yet
        BoxPresenceMask = 1u << DefaultFootprintBoxIndex,
        Frames = LoadFrames(model),
        SpotLights = CreateSpotLightRecords(spotLights),
        OmnidirectionalLights = CreateOmniLightRecords(omniLights),
        MountPoints = LoadMountPoints(model),
        Slots = LoadSlots(model, spotLights, omniLights),
        HorizontalExtents = LoadHorizontalExtents(geometries),
        Footprint = LoadDefaultFootprint(geometries)
      };
    }

    private IMeshFootprint LoadDefaultFootprint(IEnumerable<IModelPart> geometries)
    {
      var footprint = new MeshFootprint
      {
        // Converter-derived coverage for a single occupied logical cell 15 in the 4x4 footprint.
        CoverageDescriptors = new uint[] { 0x3A000008, 0x00008000, 0xCA001000, 0xFF000001 },
        CoverageBitmaps = new ulong[]
        {
          0xFFFFFFFFFFFF0FFF,
          0x0FFFFFFFFFFFFFFF,
          0xFFF0FFFFFFFFFFFF,
          0xFFFFFFFFFFFFFFF0
        }
      };
      var maximumZ = geometries.SelectMany(g => g.Vertices.Select(v => v.Position.Z)).Max();
      footprint.BoxHeights[DefaultFootprintBoxIndex] = (ushort)(maximumZ * FixedPointScale);
      return footprint;
    }

    private IMeshHorizontalExtents LoadHorizontalExtents(IEnumerable<IModelPart> geometries)
    {
      var xCoordinates = geometries.SelectMany(g => g.Vertices.Select(v => v.Position.X));
      var yCoordinates = geometries.SelectMany(g => g.Vertices.Select(v => v.Position.Y));
      return new MeshHorizontalExtents
      {
        PositiveY = (ushort)(yCoordinates.Max() * FixedPointScale),
        NegativeY = (ushort)(-yCoordinates.Min() * FixedPointScale),
        PositiveX = (ushort)(xCoordinates.Max() * FixedPointScale),
        NegativeX = (ushort)(-xCoordinates.Min() * FixedPointScale)
      };
    }

    private IModelSlots LoadSlots(COLLADA model, LoadedStaticLight[] spotLights, LoadedStaticLight[] omniLights)
    {
      return new ModelSlots()
      {
        Turrets = LoadSlots(model, "Turret", 4, 1),
        BarrelMuzzels = LoadSlots(model, "BarrelMuzzle", 4, 5),
        TurretMuzzels = LoadSlots(model, "TurretMuzzel", 4, 9),
        Headlights = CreateLightSlots(spotLights, 4, 13),
        Omnilights = CreateLightSlots(omniLights, 4, 17),
        UnloadPoints = LoadSlots(model, "UnloadPoint", 4, 21),
        HitSpots = LoadSlots(model, "HitSpot", 4, 25),
        SmokeSpots = LoadSlots(model, "SmokeSpot", 4, 29),
        Unknown = LoadSlots(model, "Unknown", 4, 33),
        Chimneys = LoadSlots(model, "Chimney", 2, 37),
        SmokeTraces = LoadSlots(model, "SmokeTrace", 2, 39),
        Exhausts = LoadSlots(model, "Exhaust", 2, 41),
        KeelTraces = LoadSlots(model, "KeelTrace", 2, 43),
        InterfacePivot = LoadSlots(model, "InterfacePivot", 1, 45),
        CenterPivot = LoadSlots(model, "CenterPivot", 1, 46),
        ProductionSpotStart = LoadSlots(model, "ProductionSpotStart", 1, 47),
        ProductionSpotEnd = LoadSlots(model, "ProductionSpotEnd", 1, 48),
        LandingSpot = LoadSlots(model, "LandingSpot", 1, 49)
      };
    }

    private static IEnumerable<ISlot> CreateLightSlots(IEnumerable<LoadedStaticLight> lights, int count, int firstId)
    {
      var result = Enumerable.Range(0, count).Select(index => (ISlot)new Slot { Id = firstId + index }).ToArray();
      foreach (var light in lights)
      {
        result[light.SourceNumber - 1] = new Slot
        {
          Id = firstId + light.SourceNumber - 1,
          Position = CreateActiveLightAttachmentPosition(light.Light.Position)
        };
      }

      return result;
    }

    private static IVector CreateActiveLightAttachmentPosition(Vector3 position)
    {
      var rawX = (short)(position.X * FixedPointScale);
      var rawY = (short)(-position.Y * FixedPointScale);
      var rawZ = (short)(position.Z * FixedPointScale);
      return rawX == short.MinValue && rawY == short.MinValue && rawZ == short.MinValue
        ? new Vector((short.MinValue + 1) / (float)FixedPointScale, position.Y, position.Z)
        : new Vector { Value = position };
    }

    private IEnumerable<ISlot> LoadSlots(COLLADA model, string slotName, int count, int firstId)
    {
      var lights = model.Library_Lights.SelectMany(ll =>
          ll.Light.Where(l => l.Technique_Common.Directional != null && l.Name.StartsWith($"{slotName}-")))
        .ToLookup(l => l.Name);
      var modelTree = new ModelTree(model);
      var slotNodes = modelTree.SelectMany(n => n.Node.NodeProperty).Where(n => lights.Contains(n.Name));
      var result = Enumerable.Range(0, count).Select(i => (ISlot)new Slot { Id = firstId + i }).ToArray();
      foreach (var node in slotNodes)
      {
        if (!TryGetSlotNumber(node.Name, slotName, count, out var number))
        {
          continue;
        }

        result[number - 1] = GetSlot(node, firstId + number - 1);
      }

      return result;
    }

    private static bool TryGetSlotNumber(string name, string slotName, int count, out int number)
    {
      var suffix = name.Substring(slotName.Length + 1);
      return int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out number) &&
             number >= 1 && number <= count;
    }

    private Slot GetSlot(Node n, int i)
    {
      var matrix = GetTransformationMatrix(n.Matrix.First());
      Matrix4x4.Decompose(matrix, out var _, out var q, out var _);
      var direction = Math.Atan2(2.0f * (q.X * q.Y + q.Z * q.W), 1.0f - 2.0f * (q.X * q.X + q.Z * q.Z));

      return new Slot()
      {
        Position = GetVector(n),
        Direction = direction,
        ExtraAngle = n.ParseAttachmentExtraAngle(),
        Id = i
      };
    }

    private IEnumerable<ISlot> Fill(IEnumerable<ISlot> collection, int count, int firstId)
    {
      var items = collection.ToList();
      return items.Concat(Enumerable.Range(items.Count, count - items.Count)
        .Select(i => (ISlot)new Slot { Id = firstId + i }));
    }

    private IEnumerable<T> Fill<T>(IEnumerable<T> collection, int count, Func<T> constructor = null)
      where T : class, new()
    {
      var missing = count - collection.Count();
      return collection.Concat(Enumerable.Repeat(constructor?.Invoke() ?? new T(), missing));
    }

    private IEnumerable<IVector> LoadMountPoints(COLLADA model)
    {
      var mountPoints = model.Library_Lights
        .SelectMany(ll => ll.Light.Where(l => l.Technique_Common.Directional != null && l.Name.StartsWith("Turret-")))
        .ToLookup(l => l.Name);
      var mountPointsPosition = model.Library_Visual_Scenes.SelectMany(lvs => lvs.Visual_Scene.SelectMany(vs =>
        vs.Node.SelectMany(n => n.NodeProperty.First().NodeProperty.Where(np => mountPoints.Contains(np.Name)))));
      var meshMountPoints = mountPointsPosition.Select(p => GetVector(p));
      return Fill(meshMountPoints, 4);
    }

    private Vector GetVector(Node p)
    {
      var matrix = GetTransformationMatrix(p.Matrix.First());
      return new Vector() { Value = matrix.Translation };
    }

    private LoadedStaticLight[] LoadStaticLights(COLLADA model, bool isSpot)
    {
      var expectedName = isSpot ? "SpotLight" : "OmniLight";
      var lights = model.Library_Lights.SelectMany(library => library.Light)
        .Where(light => isSpot
          ? light.Technique_Common?.Spot != null
          : light.Technique_Common?.Point != null)
        .ToArray();
      if (lights.Length > 4)
      {
        throw new InvalidDataException($"COLLADA contains more than four {expectedName} records.");
      }

      var nodes = GetAllNodes(model).ToArray();
      var result = new List<LoadedStaticLight>();
      var sourceNumbers = new HashSet<int>();
      foreach (var light in lights)
      {
        var sourceNumber = ParseLightNumber(light.Name, expectedName);
        if (!sourceNumbers.Add(sourceNumber))
        {
          throw new InvalidDataException($"COLLADA contains duplicate {expectedName} source number {sourceNumber}.");
        }

        if (string.IsNullOrEmpty(light.Id))
        {
          throw new InvalidDataException($"COLLADA {expectedName}-{sourceNumber} is missing its id.");
        }

        var matchingNodes = nodes.Where(node => node.Instance_Light.Any(instance => instance.Url == $"#{light.Id}"))
          .ToArray();
        if (matchingNodes.Length != 1 || matchingNodes[0].Matrix.Count != 1)
        {
          throw new InvalidDataException($"COLLADA {expectedName}-{sourceNumber} must have exactly one node and matrix.");
        }

        var parsed = light.ParseStaticLightMetadata(isSpot, sourceNumber) ??
          GetFallbackLight(light, matchingNodes[0], isSpot);
        result.Add(new LoadedStaticLight(sourceNumber, parsed));
      }

      return result.ToArray();
    }

    private static int ParseLightNumber(string name, string expectedName)
    {
      var prefix = $"{expectedName}-";
      if (name == null || !name.StartsWith(prefix, StringComparison.Ordinal) ||
          !int.TryParse(name.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
          name != $"{expectedName}-{number}")
      {
        throw new InvalidDataException($"COLLADA static lights must use the numbered name {expectedName}-1 through {expectedName}-4.");
      }

      if (number < 1 || number > 4)
      {
        throw new InvalidDataException($"COLLADA {expectedName} source number {number} is outside 1 through 4.");
      }

      return number;
    }

    private IStaticLight GetFallbackLight(Light light, Node node, bool isSpot)
    {
      var position = GetTransformationMatrix(node.Matrix.Single()).Translation;
      if (!isSpot)
      {
        return new OmniLight
        {
          Position = position,
          LightParameters = ParseStandardColor(light.Technique_Common.Point.Color)
        };
      }

      var spot = light.Technique_Common.Spot;
      var coneAngleRadians = spot.Falloff_Angle.Value * Math.PI / 180;
      return new SpotLight
      {
        Position = position,
        LightParameters = ParseStandardColor(spot.Color),
        ConeHalfAngleTangent = (float)Math.Tan(coneAngleRadians / 2)
      };
    }

    private static Vector3 ParseStandardColor(TargetableFloat3 color)
    {
      var values = color?.Value?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if (values == null || values.Length != 3 ||
          !values.All(value => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
                               !float.IsNaN(parsed) && !float.IsInfinity(parsed)))
      {
        throw new InvalidDataException("COLLADA static light has an invalid standard color.");
      }

      return new Vector3(
        float.Parse(values[0], CultureInfo.InvariantCulture),
        float.Parse(values[1], CultureInfo.InvariantCulture),
        float.Parse(values[2], CultureInfo.InvariantCulture));
    }

    private static IReadOnlyList<ISpotLight> CreateSpotLightRecords(IEnumerable<LoadedStaticLight> lights)
    {
      var result = Enumerable.Range(0, 4).Select(_ => (ISpotLight)new SpotLight()).ToArray();
      foreach (var light in lights)
      {
        result[light.SourceNumber - 1] = (ISpotLight)light.Light;
      }

      return result;
    }

    private static IReadOnlyList<IOmniLight> CreateOmniLightRecords(IEnumerable<LoadedStaticLight> lights)
    {
      var result = Enumerable.Range(0, 4).Select(_ => (IOmniLight)new OmniLight()).ToArray();
      foreach (var light in lights)
      {
        result[light.SourceNumber - 1] = (IOmniLight)light.Light;
      }

      return result;
    }

    private static IEnumerable<Node> GetAllNodes(COLLADA model)
      => model.Library_Visual_Scenes.SelectMany(library => library.Visual_Scene)
        .SelectMany(scene => scene.Node)
        .SelectMany(GetNodeTree);

    private static IEnumerable<Node> GetNodeTree(Node node)
    {
      yield return node;
      foreach (var child in node.NodeProperty.SelectMany(GetNodeTree))
      {
        yield return child;
      }
    }

    private sealed class LoadedStaticLight
    {
      public LoadedStaticLight(int sourceNumber, IStaticLight light)
      {
        SourceNumber = sourceNumber;
        Light = light;
      }

      public int SourceNumber { get; }
      public IStaticLight Light { get; }
    }

    private Matrix4x4 GetTransformationMatrix(Matrix matrix)
    {
      var values = matrix.Value.Split(' ')
        .Select(c => float.Parse(c, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
      return new Matrix4x4(
        values[0],
        values[4],
        values[8],
        values[12],
        values[1],
        values[5],
        values[9],
        values[13],
        values[2],
        values[6],
        values[10],
        values[14],
        values[3],
        values[7],
        values[11],
        values[15]
      );
    }

    private IMeshFrames LoadFrames(COLLADA model)
    {
      var modelTree = new ModelTree(model).ToArray();

      return new MeshFrames()
      {
        LoopedFrames = GetFrameCount(modelTree, AnimationType.Looped),
        MovementFrames = GetFrameCount(modelTree, AnimationType.TwoWay),
        ActionFrames = GetFrameCount(modelTree, AnimationType.Single),
        BuildingFrames = GetFrameCount(modelTree, AnimationType.Lift)
      };
    }

    private byte GetFrameCount(IEnumerable<ModelTreeNode> modelTree, AnimationType type)
    {
      var frames = modelTree
        .Where(p => p.ParseAnimationDetails().AnimationType == type)
        .Select(p => p.ParseAnimationDetails().FrameCount);
      var def = frames.DefaultIfEmpty();
      var max = def.Max();

      return (byte)max;
    }

    private COLLADA LoadModel(string filePath)
    {
      var serializer = new XmlSerializer(typeof(COLLADA));
      using (var stream = File.OpenRead(filePath))
      {
        return (COLLADA)serializer.Deserialize(stream);
      }
    }
  }
}
