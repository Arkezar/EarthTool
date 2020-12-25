using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace EarthTool.MSH
{
  public class MSHWavefrontConverter : MSHConverter
  {
    public MSHWavefrontConverter(ILogger<MSHWavefrontConverter> logger) : base(logger)
    {
    }

    public override int InternalConvert(Model model, string outputPath = null)
    {
      WriteWavefrontModel(model, outputPath);
      return 1;
    }

    private void WriteWavefrontModel(Model model, string outputPath)
    {
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
