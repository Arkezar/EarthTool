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
using System.Drawing;
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
      var earthInfo = _earthInfoFactory.Get(modelName, Common.Enums.FileFlags.None, Guid.NewGuid());
      var geometries = LoadGeometries(model).ToArray();
      var descriptor = LoadDescriptor(model, geometries);

      return new EarthMesh()
      {
        FileHeader = earthInfo,
        Descriptor = descriptor,
        Geometries = geometries,
        PartsTree = _hierarchyBuilder.GetPartsTree(geometries)
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
      var offset = new Vector() { Value = GetTransformationMatrix(node.TransformationMatrix).Translation };

      var details = node.ParseAnimationDetails();

      return new ModelPart()
      {
        Faces = facesAndVertices.Faces,
        Vertices = facesAndVertices.Vertices,
        Animations = LoadAnimations(g, node.Model, offset),
        Texture = LoadTexture(node),
        UnknownFlag = 0,
        UnknownByte1 = 0,
        UnknownByte2 = (byte)(idx == count - 1 ? 0 : 120),
        UnknownByte3 = 0,
        Offset = offset,
        BackTrackDepth = (byte)node.BacktrackLevel,
        PartType = details.PartType,
        AnimationType = details.AnimationType,
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

          if (!vertices.Any(v => v.Position.Equals(position) && v.Normal.Equals(normal) && v.UVMap.Equals(uv)))
          {
            vertices.Add(new Vertex(position, normal, uv, (short)normalId, (short)positionId));
          }
        }
      }

      var resultFaces = faces.Select(f => GetFace(f, vertices, vertexVectors, vertexInput.Offset, normalVector,
        normalInput.Offset, uvs, uvMapInput.Offset)).ToArray();
      return (resultFaces, vertices);
    }

    private Face GetFace(int[][] f, IList<Vertex> vertices, IVector[] vertexVectors, ulong vertexOffset,
      IVector[] normalVector, ulong normalOffset,
      IUVMap[] uvs, ulong uvOffset)
    {
      var v1 = vertices.Single(v =>
        v.Position.Equals(vertexVectors[f[0][vertexOffset]]) && v.Normal.Equals(normalVector[f[0][normalOffset]]) &&
        v.UVMap.Equals(uvs[f[0][uvOffset]]));
      var v2 = vertices.Single(v =>
        v.Position.Equals(vertexVectors[f[1][vertexOffset]]) && v.Normal.Equals(normalVector[f[1][normalOffset]]) &&
        v.UVMap.Equals(uvs[f[1][uvOffset]]));
      var v3 = vertices.Single(v =>
        v.Position.Equals(vertexVectors[f[2][vertexOffset]]) && v.Normal.Equals(normalVector[f[2][normalOffset]]) &&
        v.UVMap.Equals(uvs[f[2][uvOffset]]));

      return new Face()
      {
        V1 = (short)vertices.IndexOf(v1),
        V2 = (short)vertices.IndexOf(v2),
        V3 = (short)vertices.IndexOf(v3),
        UNKNOWN = 1 // must be greater than 0?
      };
    }

    private IUVMap[] LoadUVs(Source source)
    {
      var values = source.Float_Array.Value.Split(' ').Select(v => float.Parse(v, CultureInfo.InvariantCulture));
      var groupSizes = source.Technique_Common.Accessor.Param.Count;
      return values.Select((v, i) => new { Value = v, Group = i / groupSizes }).GroupBy(v => v.Group)
        .Select(g => g.Select(v => v.Value))
        .Select(v => new UVMap(v.ElementAt(0), v.ElementAt(1))).ToArray();
    }

    private IVector[] LoadVectors(Source source)
    {
      var values = source.Float_Array.Value.Split(' ').Select(v => float.Parse(v, CultureInfo.InvariantCulture));
      var groupSizes = source.Technique_Common.Accessor.Param.Count;
      return values.Select((v, i) => new { Value = v, Group = i / groupSizes }).GroupBy(v => v.Group)
        .Select(g => g.Select(v => v.Value))
        .Select(v => new Vector(v.ElementAt(0), v.ElementAt(1), v.ElementAt(2))).ToArray();
    }

    private IMeshDescriptor LoadDescriptor(COLLADA model, IEnumerable<IModelPart> geometries)
    {
      return new MeshDescriptor()
      {
        MeshType = MeshType.Regular, // dynamic not supported yet
        RegularMeshSubType = MeshSubType.Unit,
        Frames = LoadFrames(model),
        SpotLights = LoadSpotLights(model),
        OmnidirectionalLights = LoadOmniLights(model),
        MountPoints = LoadMountPoints(model),
        Slots = LoadSlots(model),
        Boundaries = LoadBoundries(model, geometries),
        Template = LoadTemplate(model),
        TemplateDetails = LoadTemplateDetails(model)
      };
    }

    private ITemplateDetails LoadTemplateDetails(COLLADA model)
    {
      return new TemplateDetails();
    }

    private IModelTemplate LoadTemplate(COLLADA model)
    {
      return new ModelTemplate();
    }

    private IMeshBoundries LoadBoundries(COLLADA model, IEnumerable<IModelPart> geometries)
    {
      var Xs = geometries.SelectMany(g => g.Vertices.Select(v => v.Position.X));
      var maxX = MathF.Abs(Xs.Max() * byte.MaxValue);
      var minX = MathF.Abs(Xs.Min() * byte.MaxValue);
      var Ys = geometries.SelectMany(g => g.Vertices.Select(v => v.Position.Y));
      var maxY = MathF.Abs(Ys.Max() * byte.MaxValue);
      var minY = MathF.Abs(Ys.Min() * byte.MaxValue);
      return new MeshBoundries() { MaxX = (short)maxX, MinX = (short)minX, MaxY = (short)maxY, MinY = (short)minY };
    }

    private IModelSlots LoadSlots(COLLADA model)
    {
      return new ModelSlots()
      {
        BarrelMuzzels = LoadSlots(model, "BarrelMuzzle", 4),
        CenterPivot = LoadSlots(model, "CenterPivot", 1),
        Chimneys = LoadSlots(model, "Chimney", 2),
        Exhausts = LoadSlots(model, "Exhaust", 2),
        HitSpots = LoadSlots(model, "HitSpot", 4),
        InterfacePivot = LoadSlots(model, "InterfacePivot", 1),
        KeelTraces = LoadSlots(model, "KeelTrace", 2),
        LandingSpot = LoadSlots(model, "LandingSpot", 1),
        ProductionSpotStart = LoadSlots(model, "ProductionSpotStart", 1),
        ProductionSpotEnd = LoadSlots(model, "ProductionSpotEnd", 1),
        SmokeSpots = LoadSlots(model, "SmokeSpot", 4),
        SmokeTraces = LoadSlots(model, "SmokeTrace", 2),
        TurretMuzzels = LoadSlots(model, "TurretMuzzel", 4),
        Turrets = LoadSlots(model, "Turret", 4),
        UnloadPoints = LoadSlots(model, "UnloadPoint", 4),
        Unknown = LoadSlots(model, "Unknown", 4),
        Headlights = LoadSpotLightSlots(model, 4),
        Omnilights = LoadOmniLightSlots(model, 4),
      };
    }

    private IEnumerable<ISlot> LoadOmniLightSlots(COLLADA model, int count)
    {
      var lights = LoadOmniLights(model);
      var meshLightSlots = lights.Select((l, i) => new Slot() { Position = l, Id = i });
      return Fill(meshLightSlots, count);
    }

    private IEnumerable<ISlot> LoadSpotLightSlots(COLLADA model, int count)
    {
      var lights = LoadSpotLights(model);
      var meshLightSlots = lights.Select((l, i) => new Slot() { Position = l, Id = i });
      return Fill(meshLightSlots, count);
    }

    private IEnumerable<ISlot> LoadSlots(COLLADA model, string slotName, int count)
    {
      var slots = model.Library_Lights.SelectMany(ll =>
          ll.Light.Where(l => l.Technique_Common.Directional != null && l.Name.StartsWith($"{slotName}-")))
        .ToLookup(l => l.Name);
      var slotsPosition = model.Library_Visual_Scenes.SelectMany(lvs => lvs.Visual_Scene.SelectMany(vs =>
        vs.Node.SelectMany(n => n.NodeProperty.First().NodeProperty.Where(np => slots.Contains(np.Name)))));
      var meshSlots = slotsPosition.Select((n, i) => GetSlot(n, i));
      return Fill(meshSlots, count, () => new Slot());
    }

    private Slot GetSlot(Node n, int i)
    {
      var matrix = GetTransformationMatrix(n.Matrix.First());
      Matrix4x4.Decompose(matrix, out var _, out var q, out var _);
      var direction = Math.Atan2(2.0f * (q.X * q.Y + q.Z * q.W), 1.0f - 2.0f * (q.X * q.X + q.Z * q.Z));

      return new Slot() { Position = GetVector(n), Direction = direction, Flag = 128, Id = i };
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

    private IEnumerable<ISpotLight> LoadSpotLights(COLLADA model)
    {
      var spotlights = model.Library_Lights.SelectMany(ll => ll.Light.Where(l => l.Technique_Common.Spot != null))
        .ToLookup(l => $"#{l.Id}");
      var spotlightsPosition = model.Library_Visual_Scenes.SelectMany(lvs => lvs.Visual_Scene.SelectMany(vs =>
        vs.Node.SelectMany(n =>
          n.NodeProperty.First().NodeProperty
            .Where(np => spotlights.Contains(np.Instance_Light.FirstOrDefault()?.Url)))));
      var meshSpotlights = spotlights.GroupJoin(spotlightsPosition, l => l.Key, p => p.Instance_Light.First()?.Url,
        (l, p) => GetSpotLight(l.First(), p.First())).ToArray();
      return Fill(meshSpotlights, 4);
    }

    private IEnumerable<IOmniLight> LoadOmniLights(COLLADA model)
    {
      var omnilights = model.Library_Lights.SelectMany(ll => ll.Light.Where(l => l.Technique_Common.Point != null))
        .ToLookup(l => $"#{l.Id}");
      var omnilightsPosition = model.Library_Visual_Scenes.SelectMany(lvs => lvs.Visual_Scene.SelectMany(vs =>
        vs.Node.SelectMany(n =>
          n.NodeProperty.First().NodeProperty
            .Where(np => omnilights.Contains(np.Instance_Light.FirstOrDefault()?.Url)))));
      var meshOmnilights = omnilights.GroupJoin(omnilightsPosition, l => l.Key, p => p.Instance_Light.First()?.Url,
        (l, p) => GetOmniLight(l.First(), p.First())).ToArray();
      return Fill(meshOmnilights, 4);
    }

    private SpotLight GetSpotLight(Light light, Node node)
    {
      var color = light.Technique_Common.Spot.Color.Value.Split(' ')
        .Select(c => (int)(255 * float.Parse(c, System.Globalization.CultureInfo.InvariantCulture))).ToArray();
      var matrix = GetTransformationMatrix(node.Matrix.First());

      Matrix4x4.Decompose(matrix, out var _, out var q, out var _);
      var tilt = (float)MathF.Atan2(2.0f * (q.Y * q.W + q.X * q.Z), 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y));
      var direction = (float)MathF.Atan2(2.0f * (q.X * q.Y + q.Z * q.W), 1.0f - 2.0f * (q.X * q.X + q.Z * q.Z));
      return new SpotLight()
      {
        Value = matrix.Translation,
        Color = Color.FromArgb(color[0], color[1], color[2]),
        Length = (float)light.Technique_Common.Spot.Constant_Attenuation.Value,
        Ambience = (float)light.Technique_Common.Spot.Linear_Attenuation.Value,
        Width = (float)(light.Technique_Common.Spot.Falloff_Angle.Value * Math.PI / 180),
        Direction = (int)(2 * direction * 255 / Math.PI),
        Tilt = (float)(-tilt - Math.PI / 2)
      };
    }

    private OmniLight GetOmniLight(Light light, Node node)
    {
      var color = light.Technique_Common.Spot.Color.Value.Split(' ')
        .Select(c => (int)(255 * float.Parse(c, System.Globalization.CultureInfo.InvariantCulture))).ToArray();
      var matrix = GetTransformationMatrix(node.Matrix.First());

      return new OmniLight()
      {
        Value = matrix.Translation, Color = Color.FromArgb(color[0], color[1], color[2]), Radius = 0,
      };
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