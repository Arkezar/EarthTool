using EarthTool.Common.Interfaces;
using EarthTool.MSH.Models;
using System.Collections.Generic;
using System.IO;

namespace EarthTool.MSH
{
  public class MSHConverter : IMSHConverter
  {
    public int Convert(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);
      WriteWavefrontModel(new Model(filePath), outputPath);
      return 1;
    }

    private static void WriteWavefrontModel(Model model, string outputPath)
    {
      for (var i = 0; i < model.Parts.Count; i++)
      {
        var workDir = Path.GetDirectoryName(model.FilePath);
        var modelName = Path.GetFileNameWithoutExtension(model.FilePath);

        var resultDir = Path.Combine(outputPath, modelName);
        if (!Directory.Exists(resultDir))
        {
          Directory.CreateDirectory(resultDir);
        }

        var resultFile = Path.Combine(resultDir, $"{modelName}_{i}.obj");
        using (var fs = new FileStream(resultFile, FileMode.Create))
        {
          using (var writer = new StreamWriter(fs))
          {
            WriteVertices(writer, model.Parts[i].Vertices);
            WriteFaces(writer, model.Parts[i].Faces);
          }
        }
        File.WriteAllText(resultFile + ".info", model.Parts[i].Texture.FileName);
      }
    }


    private static void WriteVertices(StreamWriter writer, IEnumerable<Vertex> vertices)
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

    private static void WriteFaces(StreamWriter writer, IEnumerable<Face> faces)
    {
      const string FACE_TEMPLATE = "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}";
      foreach (var face in faces)
      {
        writer.WriteLine(string.Format(FACE_TEMPLATE, face.V1 + 1, face.V2 + 1, face.V3 + 1));
      }
    }
  }
}
