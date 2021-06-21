using Collada141;
using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
      var modelName = Path.GetFileNameWithoutExtension(model.FilePath);
      var resultDir = Path.Combine(outputPath, modelName);

      if (!Directory.Exists(resultDir))
      {
        Directory.CreateDirectory(resultDir);
      }

      var geometriesList = model.Parts.Select((part, i) =>
      {
        var geometry = new geometry
        {
          name = $"{modelName}-Part-{i}",
          id = $"{modelName}-Part-{i}"
        };

        var positions = GetSource("positions", part.Vertices, v => new double[] { v.Position.X, v.Position.Y, v.Position.Z });
        var normals = GetSource("normals", part.Vertices, v => new double[] { v.Normal.X, v.Normal.Y, v.Normal.Z });

        var vertices = new vertices()
        {
          id = "vertices",
          input = new InputLocal[] { new InputLocal()
            {
              semantic = "POSITION",
              source = "#positions"
            }
          }
        };

        var poly = new polylist
        {
          count = (ulong)part.Faces.Count,
          input = new InputLocalOffset[] { new InputLocalOffset()
            {
              semantic = "VERTEX",
              source = "#vertices",
              offset = 0
            },
            new InputLocalOffset()
            {
              semantic = "NORMAL",
              source = "#normals",
              offset = 1
            }
          },
          vcount = string.Join(' ', Enumerable.Repeat(3, part.Faces.Count)),
          p = string.Join(' ', part.Faces.SelectMany(f => new string[] { f.V1.ToString(), f.V1.ToString(), f.V2.ToString(), f.V2.ToString(), f.V3.ToString(), f.V3.ToString() }))
        };

        var mesh = new mesh
        {
          source = new source[] { positions, normals },
          Items = new object[] { poly },
          vertices = vertices
        };
        geometry.Item = mesh;

        return geometry;
      });

      var geometries = new library_geometries();
      geometries.geometry = geometriesList.ToArray();

      var visualScenes = new library_visual_scenes();
      var visualScene = new visual_scene()
      {
        id = "scene"
      };

      var node = new node()
      {
        id = modelName,
        name = modelName
      };

      node.instance_geometry = geometriesList.Select(g => new instance_geometry()
      {
        url = $"#{g.id}"
      }).ToArray();
      visualScene.node = new node[] { node };

      visualScenes.visual_scene = new visual_scene[] { visualScene };
      var scene = new COLLADAScene();
      scene.instance_visual_scene = new InstanceWithExtra()
      {
        url = "#scene"
      };

      var collada = new Collada141.COLLADA
      {
        asset = new asset
        {
          up_axis = UpAxisType.Z_UP
        }
      };

      collada.Items = new object[] { geometries, visualScenes };

      collada.scene = scene;
      collada.Save(Path.Combine(resultDir, $"{modelName}.dae"));

      File.WriteAllText(Path.Combine(resultDir, $"{modelName}.template"), model.Template.ToString());
    }

    private source GetSource<T>(string name, IList<T> data, Func<T, IEnumerable<double>> func)
    {
      var source = new source
      {
        id = name,
        name = name
      };
      var sourceArray = new float_array
      {
        id = $"{name}-array",
        count = (ulong)data.Count * 3,
        Values = data.SelectMany(func).ToArray()
      };
      source.Item = sourceArray;
      source.technique_common = new sourceTechnique_common
      {
        accessor = new accessor
        {
          source = $"#{name}-array",
          count = (ulong)data.Count,
          stride = 3,
          param = new param[]
          {
              new param()
              {
                name = "X",
                type = "float"
              },
              new param()
              {
                name = "Y",
                type = "float"
              },
              new param()
              {
                name = "Z",
                type = "float"
              }
          }
        }
      };

      return source;
    }
  }
}
