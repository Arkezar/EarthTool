using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Elements;
using Microsoft.Extensions.Logging;
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

    public override Task InternalConvert(Model model, string outputPath = null)
    {
      WriteWavefrontModel(model, outputPath);
      return Task.CompletedTask;
    }

    private void WriteWavefrontModel(Model model, string outputPath)
    {
      var modelName = Path.GetFileNameWithoutExtension(model.FilePath);

      if (!Directory.Exists(outputPath))
      {
        Directory.CreateDirectory(outputPath);
      }

      var partIndex = 0;
      foreach (var part in model.Parts)
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

      File.WriteAllText(Path.Combine(outputPath, $"{modelName}.template"), model.Template.ToString());
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
