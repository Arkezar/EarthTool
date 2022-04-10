using EarthTool.Common.Enums;
using EarthTool.MSH.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace EarthTool.MSH.Converters.Wavefront
{
  public class MSHWavefrontConverter : MSHConverter
  {
    public MSHWavefrontConverter(ILogger<MSHWavefrontConverter> logger) : base(logger)
    {
    }

    public override Task InternalConvert(ModelType modelType, IMesh model, string outputPath = null)
    {
      WriteWavefrontModel(model, outputPath);
      return Task.CompletedTask;
    }

    private void WriteWavefrontModel(IMesh model, string outputPath)
    {
      var modelName = Path.GetFileNameWithoutExtension(model.FileHeader.FilePath);

      if (!Directory.Exists(outputPath))
      {
        Directory.CreateDirectory(outputPath);
      }

      var partIndex = 0;
      foreach (var part in model.Geometries)
      {
        var resultFile = Path.Combine(outputPath, $"{modelName}_{partIndex}.obj");
        using (var fs = new FileStream(resultFile, FileMode.Create))
        {
          using (var writer = new StreamWriter(fs))
          {
            WriteVertices(writer, part.Vertices);
            WriteFaces(writer, part.Faces);
          }
        }
        using (var fs = new FileStream(resultFile + ".info", FileMode.Create))
        {
          using (var writer = new StreamWriter(fs))
          {
            WriteInfo(writer, part);
          }
        }
        partIndex++;
      }

      File.WriteAllText(Path.Combine(outputPath, $"{modelName}.template"), model.Descriptor.Template.ToString());
    }

    private void WriteInfo(StreamWriter writer, IModelPart part)
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

    private void WriteVertices(StreamWriter writer, IEnumerable<IVertex> vertices)
    {
      const string VERTEX_TEMPLATE = "v {0} {1} {2}";
      const string NORMAL_TEMPLATE = "vn {0} {1} {2}";
      const string UV_TEMPLATE = "vt {0} {1}";

      //vertices
      foreach (var vertex in vertices)
      {
        writer.WriteLine(string.Format(VERTEX_TEMPLATE, vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
      }

      //normal
      foreach (var vertex in vertices)
      {
        writer.WriteLine(string.Format(NORMAL_TEMPLATE, vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z));
      }

      //uv
      foreach (var vertex in vertices)
      {
        writer.WriteLine(string.Format(UV_TEMPLATE, vertex.U, vertex.V));
      }
    }

    private void WriteFaces(StreamWriter writer, IEnumerable<IFace> faces)
    {
      const string FACE_TEMPLATE = "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}";
      foreach (var face in faces)
      {
        writer.WriteLine(string.Format(FACE_TEMPLATE, face.V1 + 1, face.V2 + 1, face.V3 + 1));
      }
    }

    protected override ModelType GetOutputType(string filePath)
    {
      throw new NotImplementedException();
    }

    protected override IMesh LoadModel(string filePath)
    {
      throw new NotImplementedException();
    }
  }
}
