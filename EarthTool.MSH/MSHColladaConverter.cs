using Collada141;
using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace EarthTool.MSH
{
  public class MSHColladaConverter : MSHConverter
  {
    public MSHColladaConverter(ILogger<MSHColladaConverter> logger) : base(logger)
    {
    }

    public override int InternalConvert(Model model, string outputPath = null)
    {
      WriteColladaModel(model, outputPath);
      return 1;
    }

    private void WriteColladaModel(Model model, string outputPath)
    {
      var modelName = GetModelName(model);
      var resultDir = Path.Combine(outputPath, modelName);

      if (!Directory.Exists(resultDir))
      {
        Directory.CreateDirectory(resultDir);
      }

      var geometries = GetGeometries(model.Parts, modelName);
      var scenes = GetScenes(geometries, modelName);
      var scene = GetScene(scenes);   

      var collada = new COLLADA
      {
        Asset = new Asset
        {
          Up_Axis = UpAxisType.Z_UP
        }
      };

      collada.Library_Geometries.Add(geometries);
      collada.Library_Visual_Scenes.Add(scenes);
      collada.Scene = scene;

      var serializer = new XmlSerializer(typeof(COLLADA));
      using (var stream = new FileStream(Path.Combine(resultDir, $"{modelName}.dae"), FileMode.Create))
      {

        serializer.Serialize(stream, collada);
      }

      File.WriteAllText(Path.Combine(resultDir, $"{modelName}.template"), model.Template.ToString());
    }

    private COLLADAScene GetScene(Library_Visual_Scenes scenes)
    {
      var scene = new COLLADAScene();
      scene.Instance_Visual_Scene = new InstanceWithExtra()
      {
        Url = $"#{scenes.Visual_Scene.First().Id}"
      };

      return scene;
    }

    private Library_Visual_Scenes GetScenes(Library_Geometries geometries, string modelName)
    {
      var visualScenes = new Library_Visual_Scenes();
      var visualScene = new Visual_Scene()
      {
        Id = "scene"
      };

      var node = new Node()
      {
        Id = modelName,
        Name = modelName
      };

      geometries.Geometry.Select(g => new Instance_Geometry()
      {
        Url = $"#{g.Id}"
      }).ToList().ForEach(g => node.Instance_Geometry.Add(g));

      visualScene.Node.Add(node);

      visualScenes.Visual_Scene.Add(visualScene);

      return visualScenes;
    }

    private string GetModelName(Model model)
    {
      return Path.GetFileNameWithoutExtension(model.FilePath);
    }

    private Library_Geometries GetGeometries(IEnumerable<ModelPart> parts, string modelName)
    {
      var geometries = new Library_Geometries();

      var geometriesList = parts.Select((part, i) => GetGeometry(part, i, modelName));
      geometriesList.ToList().ForEach(g => geometries.Geometry.Add(g));

      return geometries;
    }

    private Geometry GetGeometry(ModelPart part, int i, string modelName)
    {
      var geometry = new Geometry
      {
        Name = $"{modelName}-Part-{i}",
        Id = $"{modelName}-Part-{i}"
      };

      var positions = GetSource("positions", part.Vertices, v => new float[] { v.Position.X, v.Position.Y, v.Position.Z });
      var normals = GetSource("normals", part.Vertices, v => new float[] { v.Normal.X, v.Normal.Y, v.Normal.Z });

      var vertices = new Vertices()
      {
        Id = "vertices"
      };

      vertices.Input.Add(new InputLocal()
      {
        Semantic = "POSITION",
        Source = "#positions"
      });

      var poly = new Polylist
      {
        Count = (ulong)part.Faces.Count,
        Vcount = string.Join(' ', Enumerable.Repeat(3, part.Faces.Count)),
        P = string.Join(' ', part.Faces.SelectMany(f => new string[] { f.V1.ToString(), f.V1.ToString(), f.V2.ToString(), f.V2.ToString(), f.V3.ToString(), f.V3.ToString() }))
      };

      poly.Input.Add(new InputLocalOffset()
      {
        Semantic = "VERTEX",
        Source = "#vertices",
        Offset = 0
      });

      poly.Input.Add(new InputLocalOffset()
      {
        Semantic = "NORMAL",
        Source = "#normals",
        Offset = 1
      });

      var mesh = new Mesh
      {
        Vertices = vertices
      };

      mesh.Source.Add(positions);
      mesh.Source.Add(normals);
      mesh.Polylist.Add(poly);

      geometry.Mesh = mesh;

      return geometry;
    }

    private Source GetSource<T>(string name, IList<T> data, Func<T, IEnumerable<float>> func)
    {
      var source = new Source
      {
        Id = name,
        Name = name
      };
      var sourceArray = new Float_Array
      {
        Id = $"{name}-array",
        Count = (ulong)data.Count * 3,
        Value = string.Join(" ", data.SelectMany(func).Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)))
      };
      source.Float_Array = sourceArray;
      source.Technique_Common = new SourceTechnique_Common
      {
        Accessor = new Accessor
        {
          Source = $"#{name}-array",
          Count = (ulong)data.Count,
          Stride = 3
        }
      };
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "X",
        Type = "float"
      });
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "Y",
        Type = "float"
      });
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "Z",
        Type = "float"
      });
      return source;
    }
  }
}
