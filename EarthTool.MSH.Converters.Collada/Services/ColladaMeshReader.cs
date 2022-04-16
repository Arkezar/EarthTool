using Collada141;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Converters.Collada.Collections;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Serialization;

namespace EarthTool.MSH.Converters.Collada.Services
{
  public class ColladaMeshReader : IMeshReader
  {
    private readonly IEarthInfoFactory _earthInfoFactory;

    public ColladaMeshReader(IEarthInfoFactory earthInfoFactory)
    {
      _earthInfoFactory = earthInfoFactory;
    }

    public IMesh Read(string filePath)
    {
      var model = LoadModel(filePath);
      return Read(model);
    }

    private IMesh Read(COLLADA model)
    {
      var modelName = model.Library_Visual_Scenes.First().Visual_Scene.First().Node.First().Id;
      var earthInfo = _earthInfoFactory.Get(modelName, Common.Enums.FileFlags.None, Guid.NewGuid());
      var geometries = LoadGeometries(model);
      var descriptor = LoadDescriptor(model);

      return new EarthMesh()
      {
        FileHeader = earthInfo,
        Descriptor = descriptor,
        Geometries = geometries
      };
    }

    private IEnumerable<IModelPart> LoadGeometries(COLLADA model)
    {
      var modelTree = new ModelTree(model);
      var geometries = model.Library_Geometries.First().Geometry;
      var result = modelTree.Select(g => LoadGeometry(geometries.Single(n => n.Name == g.Node.Name), model, modelTree)).ToArray();
      
      result.Last().UnknownBytes = new byte[5];
      result.Last().PartType = 0;
      result.First().PartType = 0;

      return result;
    }

    private ModelPart LoadGeometry(Geometry g, COLLADA model, ModelTree modelTree)
    {
      var facesAndVertices = LoadFacesWithVertices(g);
      var offsetAndDepth = LoadOffsetAndDepth(modelTree, g.Name);

      return new ModelPart()
      {
        Faces = facesAndVertices.Faces,
        Vertices = facesAndVertices.Vertices,
        Animations = LoadAnimations(g, model),
        Texture = LoadTexture(g, model),
        UnknownBytes = new byte[] { 0, 0, 0, 120, 0 },
        Offset = offsetAndDepth.Vector,
        Depth = (byte)offsetAndDepth.BacktrackLevel,
        PartType = 8
      };
    }

    private (IVector Vector, int BacktrackLevel) LoadOffsetAndDepth(ModelTree model, string id)
    {
      var node = model.Single(n => n.Node.Id == id);
      return (GetVector(node.Node), node.BacktrackLevel);
    }

    private ITextureInfo LoadTexture(Geometry g, COLLADA model)
    {
      return new Models.Elements.TextureInfo() { FileName = "Textures\\UCSUML.tex" };
    }

    private IAnimations LoadAnimations(Geometry g, COLLADA model)
    {
      return new Models.Collections.Animations();
    }

    private (IEnumerable<IFace> Faces, IEnumerable<IVertex> Vertices) LoadFacesWithVertices(Geometry g)
    {
      var vertexSource = g.Mesh.Vertices.Input.First().Source.Trim('#');
      var normalSource = g.Mesh.Triangles.First().Input.Single(i => i.Semantic == "NORMAL").Source.Trim('#');
      var uvMapSource = g.Mesh.Triangles.First().Input.Single(i => i.Semantic == "TEXCOORD").Source.Trim('#');

      var vertices = LoadVectors(g.Mesh.Source.Single(s => s.Id == vertexSource));
      var normals = LoadVectors(g.Mesh.Source.Single(s => s.Id == normalSource));
      var uvs = LoadUVs(g.Mesh.Source.Single(s => s.Id == uvMapSource));

      var triangles = g.Mesh.Triangles.First();
      var faceValues = triangles.P.Split(' ').Select(v => int.Parse(v));
      var faces = faceValues.Select((v, i) => new { Value = v, Group = i / triangles.Input.Count })
        .GroupBy(v => v.Group)
        .Select((v, i) => new { Face = v.Select(x => x.Value).ToArray(), Group = i / 3 })
        .GroupBy(v => v.Group)
        .Select(x => x.Select(v => v.Face.ToArray()).ToArray()).ToArray();

      var faceVertices = faces.Select((f, fi) => f.Select((x, i) => new { Id = 3 * fi + i, Vertex = new Models.Elements.Vertex(vertices[x[0]], normals[x[1]], uvs[x[2]], -1, -1) }));
      var resultFaces = faceVertices.Select(f => new Models.Elements.Face()
      {
        V1 = (short)f.ElementAt(0).Id,
        V2 = (short)f.ElementAt(1).Id,
        V3 = (short)f.ElementAt(2).Id,
        UNKNOWN = 3
      });
      var resultVertices = faceVertices.SelectMany(f => f).Select(v => v.Vertex);
      return (resultFaces, resultVertices);
    }

    private IUVMap[] LoadUVs(Source source)
    {
      var values = source.Float_Array.Value.Split(' ').Select(v => float.Parse(v, CultureInfo.InvariantCulture));
      var groupSizes = source.Technique_Common.Accessor.Param.Count;
      return values.Select((v, i) => new { Value = v, Group = i / groupSizes }).GroupBy(v => v.Group)
                   .Select(g => g.Select(v => v.Value))
                   .Select(v => new Models.Elements.UVMap(v.ElementAt(0), v.ElementAt(1))).ToArray();
    }

    private IVector[] LoadVectors(Source source)
    {
      var values = source.Float_Array.Value.Split(' ').Select(v => float.Parse(v, CultureInfo.InvariantCulture));
      var groupSizes = source.Technique_Common.Accessor.Param.Count;
      return values.Select((v, i) => new { Value = v, Group = i / groupSizes }).GroupBy(v => v.Group)
                   .Select(g => g.Select(v => v.Value))
                   .Select(v => new Models.Elements.Vector(v.ElementAt(0), v.ElementAt(1), v.ElementAt(2))).ToArray();
    }

    private IMeshDescriptor LoadDescriptor(COLLADA model)
    {
      return new MeshDescriptor()
      {
        Type = 0,
        UnknownValue1 = 0,
        UnknownValue2 = 1, // required for unit models!!!
        Frames = LoadFrames(model),
        SpotLights = LoadSpotLights(model),
        OmniLights = LoadOmniLights(model),
        MountPoints = LoadMountPoints(model),
        Slots = LoadSlots(model),
        Boundries = LoadBoundries(model),
        Template = LoadTemplate(model),
        TemplateDetails = LoadTemplateDetails(model)
      };
    }

    private ITemplateDetails LoadTemplateDetails(COLLADA model)
    {
      return new Models.Elements.TemplateDetails();
    }

    private IModelTemplate LoadTemplate(COLLADA model)
    {
      return new Models.Elements.ModelTemplate();
    }

    private IMeshBoundries LoadBoundries(COLLADA model)
    {
      return new MeshBoundries();
    }

    private IModelSlots LoadSlots(COLLADA model)
    {
      return new Models.Collections.ModelSlots()
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
      var meshLightSlots = lights.Select((l, i) => new Models.Elements.Slot() { Position = l, Id = i });
      return Fill(meshLightSlots, count);
    }

    private IEnumerable<ISlot> LoadSpotLightSlots(COLLADA model, int count)
    {
      var lights = LoadSpotLights(model);
      var meshLightSlots = lights.Select((l, i) => new Models.Elements.Slot() { Position = l, Id = i });
      return Fill(meshLightSlots, count);
    }

    private IEnumerable<ISlot> LoadSlots(COLLADA model, string slotName, int count)
    {
      var slots = model.Library_Lights.SelectMany(ll => ll.Light.Where(l => l.Technique_Common.Directional != null && l.Name.StartsWith($"{slotName}-"))).ToLookup(l => l.Name);
      var slotsPosition = model.Library_Visual_Scenes.SelectMany(lvs => lvs.Visual_Scene.SelectMany(vs => vs.Node.SelectMany(n => n.NodeProperty.First().NodeProperty.Where(np => slots.Contains(np.Name)))));
      var meshSlots = slotsPosition.Select((n, i) => GetSlot(n, i));
      return Fill(meshSlots, count, () => new Models.Elements.Slot() { Position = new Models.Elements.Vector(), Id = 0 });
    }

    private Models.Elements.Slot GetSlot(Node n, int i)
    {
      var matrix = GetTransformationMatrix(n.Matrix.First());
      Matrix4x4.Decompose(matrix, out var _, out var q, out var _);
      var direction = Math.Atan2(2.0f * (q.X * q.Y + q.Z * q.W), 1.0f - 2.0f * (q.X * q.X + q.Z * q.Z));

      return new Models.Elements.Slot()
      {
        Position = GetVector(n),
        Direction = direction,
        Flag = 0,
        Id = i
      };
    }

    private IEnumerable<T> Fill<T>(IEnumerable<T> collection, int count, Func<T> constructor = null) where T : class, new()
    {
      var missing = count - collection.Count();
      return collection.Concat(Enumerable.Repeat(constructor?.Invoke() ?? new T(), missing));
    }

    private IEnumerable<IVector> LoadMountPoints(COLLADA model)
    {
      var mountPoints = model.Library_Lights.SelectMany(ll => ll.Light.Where(l => l.Technique_Common.Directional != null && l.Name.StartsWith("Turret-"))).ToLookup(l => l.Name);
      var mountPointsPosition = model.Library_Visual_Scenes.SelectMany(lvs => lvs.Visual_Scene.SelectMany(vs => vs.Node.SelectMany(n => n.NodeProperty.First().NodeProperty.Where(np => mountPoints.Contains(np.Name)))));
      var meshMountPoints = mountPointsPosition.Select(p => GetVector(p));
      return Fill(meshMountPoints, 4);
    }

    private Models.Elements.Vector GetVector(Node p)
    {
      var matrix = GetTransformationMatrix(p.Matrix.First());
      return new Models.Elements.Vector()
      {
        Value = matrix.Translation
      };
    }

    private IEnumerable<ISpotLight> LoadSpotLights(COLLADA model)
    {
      var spotlights = model.Library_Lights.SelectMany(ll => ll.Light.Where(l => l.Technique_Common.Spot != null)).ToLookup(l => l.Name);
      var spotlightsPosition = model.Library_Visual_Scenes.SelectMany(lvs => lvs.Visual_Scene.SelectMany(vs => vs.Node.SelectMany(n => n.NodeProperty.First().NodeProperty.Where(np => spotlights.Contains(np.Name)))));
      var meshSpotlights = spotlights.GroupJoin(spotlightsPosition, l => l.Key, p => p.Name, (l, p) => GetSpotLight(l.First(), p.First())).ToArray();
      return Fill(meshSpotlights, 4);
    }

    private IEnumerable<IOmniLight> LoadOmniLights(COLLADA model)
    {
      var omnilights = model.Library_Lights.SelectMany(ll => ll.Light.Where(l => l.Technique_Common.Point != null)).ToLookup(l => l.Name);
      var omnilightsPosition = model.Library_Visual_Scenes.SelectMany(lvs => lvs.Visual_Scene.SelectMany(vs => vs.Node.SelectMany(n => n.NodeProperty.First().NodeProperty.Where(np => omnilights.Contains(np.Name)))));
      var meshOmnilights = omnilights.GroupJoin(omnilightsPosition, l => l.Key, p => p.Name, (l, p) => GetOmniLight(l.First(), p.First())).ToArray();
      return Fill(meshOmnilights, 4);
    }

    private Models.Elements.SpotLight GetSpotLight(Light light, Node node)
    {
      var color = light.Technique_Common.Spot.Color.Value.Split(' ').Select(c => (int)(255 * float.Parse(c, System.Globalization.CultureInfo.InvariantCulture))).ToArray();
      var matrix = GetTransformationMatrix(node.Matrix.First());

      Matrix4x4.Decompose(matrix, out var _, out var q, out var _);
      var tilt = (float)MathF.Atan2(2.0f * (q.Y * q.W + q.X * q.Z), 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y));
      var direction = (float)MathF.Atan2(2.0f * (q.X * q.Y + q.Z * q.W), 1.0f - 2.0f * (q.X * q.X + q.Z * q.Z));
      return new Models.Elements.SpotLight()
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

    private Models.Elements.OmniLight GetOmniLight(Light light, Node node)
    {
      var color = light.Technique_Common.Spot.Color.Value.Split(' ').Select(c => (int)(255 * float.Parse(c, System.Globalization.CultureInfo.InvariantCulture))).ToArray();
      var matrix = GetTransformationMatrix(node.Matrix.First());

      return new Models.Elements.OmniLight()
      {
        Value = matrix.Translation,
        Color = Color.FromArgb(color[0], color[1], color[2]),
        Radius = 0,
      };
    }

    private Matrix4x4 GetTransformationMatrix(Matrix matrix)
    {
      var values = matrix.Value.Split(' ').Select(c => float.Parse(c, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
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
      return new MeshFrames()
      {
        ActionFrames = 0,
        BuildingFrames = 0,
        LoopedFrames = 0,
        MovementFrames = 0
      };
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
