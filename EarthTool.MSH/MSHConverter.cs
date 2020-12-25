using Collada141;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EarthTool.MSH
{
  public class MSHConverter : IMSHConverter
  {
    private readonly ILogger<MSHConverter> _logger;

    public MSHConverter(ILogger<MSHConverter> logger)
    {
      _logger = logger;
    }

    public int Convert(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);
      var model = new Model(filePath);

      _logger.LogDebug("Loaded {VerticesNumber} vertices, {FacesNumber} faces",
                       model.Parts.Sum(p => p.Vertices.Count),
                       model.Parts.Sum(p => p.Faces.Count));

      //WriteWavefrontModel(model, outputPath);
      WriteColladaModel(model, outputPath);

      return 1;
    }

    private void WriteColladaModel(Model model, string outputPath)
    {
      var workDir = Path.GetDirectoryName(model.FilePath);
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

        var positions = GetSource("positions", part.Vertices, v => new double[] { v.X + part.Offset.X, v.Y + part.Offset.Y, v.Z + part.Offset.Z });
        var normals = GetSource("normals", part.Vertices, v => new double[] { v.NormalX, v.NormalY, v.NormalZ });

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
          up_axis = UpAxisType.Y_UP
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

    private void WriteWavefrontModel(Model model, string outputPath)
    {

      var workDir = Path.GetDirectoryName(model.FilePath);
      var modelName = Path.GetFileNameWithoutExtension(model.FilePath);
      var resultDir = Path.Combine(outputPath, modelName);

      if (!Directory.Exists(resultDir))
      {
        Directory.CreateDirectory(resultDir);
      }

      for (var i = 0; i < model.Parts.Count; i++)
      {
        var resultFile = Path.Combine(resultDir, $"{modelName}_{i}.obj");
        using (var fs = new FileStream(resultFile, FileMode.Create))
        {
          using (var writer = new StreamWriter(fs))
          {
            WriteVertices(writer, model.Parts[i].Vertices);
            WriteFaces(writer, model.Parts[i].Faces);
          }
        }
        using (var fs = new FileStream(resultFile + ".info", FileMode.Create))
        {
          using (var writer = new StreamWriter(fs))
          {
            WriteInfo(writer, model.Parts[i]);
          }
        }
      }

      File.WriteAllText(Path.Combine(resultDir, $"{modelName}.template"), model.Template.ToString());
    }

    private void WriteInfo(StreamWriter writer, ModelPart part)
    {
      var text = JsonSerializer.Serialize(new
      {
        part.Texture,
        part.Offset
      }, new JsonSerializerOptions
      {
        WriteIndented = true
      });

      writer.Write(text);
    }

    private void WriteVertices(StreamWriter writer, IEnumerable<Vertex> vertices)
    {
      const string VERTEX_TEMPLATE = "v {0} {1} {2}";
      const string NORMAL_TEMPLATE = "vn {0} {1} {2}";
      const string UV_TEMPLATE = "vt {0} {1}";

      //vertices
      foreach (var vertex in vertices)
      {
        writer.WriteLine(string.Format(VERTEX_TEMPLATE, vertex.X, vertex.Y, vertex.Z));
      }

      //normal
      foreach (var vertex in vertices)
      {
        writer.WriteLine(string.Format(NORMAL_TEMPLATE, vertex.NormalX, vertex.NormalY, vertex.NormalZ));
      }

      //uv
      foreach (var vertex in vertices)
      {
        writer.WriteLine(string.Format(UV_TEMPLATE, vertex.U, vertex.V));
      }
    }

    private void WriteFaces(StreamWriter writer, IEnumerable<Face> faces)
    {
      const string FACE_TEMPLATE = "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}";
      foreach (var face in faces)
      {
        writer.WriteLine(string.Format(FACE_TEMPLATE, face.V1 + 1, face.V2 + 1, face.V3 + 1));
      }
    }
  }
}
