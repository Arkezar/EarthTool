using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MESHConverter
{
  class Program
  {
    static void Main(string[] args)
    {
      var files = new[]
      {
        new{ Path = @"D:\earthwork\mesh\edubu1_1", Length = 106 },
        new{ Path = @"D:\earthwork\mesh\edubu1_2", Length = 30 },
        new{ Path = @"D:\earthwork\mesh\edubu1_3", Length = 32 },
        new{ Path = @"D:\earthwork\mesh\edubu1_4", Length = 14 },
        new{ Path = @"D:\earthwork\mesh\edubu1side", Length = 12 }
      };

      foreach (var f in files)
      {
        Faces(f.Path);
        Vertices(f.Path, f.Length);

        Animation1(f.Path);
        File.AppendAllLines(f.Path + ".obj", File.ReadAllLines(f.Path + "faces.obj"));
      }
    }

    private static void Vertices(string path, int length)
    {
      var data = File.ReadAllBytes(path);
      var lineCnt = 0;
      var cnt = 0;

      var mesh = new List<Vertex>();

      using (var fs = new FileStream(path + ".obj", FileMode.Create))
      {
        using (var writer = new StreamWriter(fs))
        {
          using (var stream = new MemoryStream(data))
          {
            while (stream.Position < stream.Length)
            {
              var bloackVertices = new Vertex[]
              {
                new Vertex(),
                new Vertex(),
                new Vertex(),
                new Vertex()
              };

              var vData = ReadBytes(stream, 160);

              for (var i = 0; i < 4; i++)
              {
                var idx = i * 4;

                bloackVertices[i].X = BitConverter.ToSingle(vData, idx + 0);
                bloackVertices[i].Z = BitConverter.ToSingle(vData, idx + 16);
                bloackVertices[i].Y = BitConverter.ToSingle(vData, idx + 32);

                bloackVertices[i].XNormal = BitConverter.ToSingle(vData, idx + 48);
                bloackVertices[i].ZNormal = BitConverter.ToSingle(vData, idx + 64);
                bloackVertices[i].YNormal = BitConverter.ToSingle(vData, idx + 80);

                bloackVertices[i].V = BitConverter.ToSingle(vData, idx + 96);
                bloackVertices[i].U = BitConverter.ToSingle(vData, idx + 112);

                bloackVertices[i].D1 = BitConverter.ToUInt16(vData, idx + 144);
                bloackVertices[i].D2 = BitConverter.ToUInt16(vData, idx + 146);

                cnt++;
              }
              mesh.AddRange(bloackVertices);
              lineCnt++;
            }
          }

          var vertices = mesh.Take(length).ToList();

          vertices.ForEach(v => writer.WriteLine($"v {v.X} {v.Y} {v.Z}"));
          vertices.ForEach(v => writer.WriteLine($"vn {v.XNormal} {v.YNormal} {v.ZNormal}"));
          vertices.ForEach(v => writer.WriteLine($"vt {v.U} {v.V}"));
        }
      }
      Console.WriteLine("Line " + lineCnt);
      Console.WriteLine("Vertex coords " + cnt);
    }

    private static void Faces(string path)
    {
      var template = "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}";
      var data = File.ReadAllBytes(path + "faces");
      var vertIds = new List<short>();
      var facesCount = 0;
      using (var fs = new FileStream(path + "faces.obj", FileMode.Create))
      {
        using (var writer = new StreamWriter(fs))
        {
          using (var stream = new MemoryStream(data))
          {
            while (stream.Position < stream.Length)
            {
              var vData = ReadBytes(stream, 8);

              var idx = 0;
              var v1 = BitConverter.ToInt16(vData, 0 + idx);
              var v2 = BitConverter.ToInt16(vData, 2 + idx);
              var v3 = BitConverter.ToInt16(vData, 4 + idx);
              var v4 = BitConverter.ToInt16(vData, 6 + idx);

              vertIds.Add(v1);
              vertIds.Add(v2);
              vertIds.Add(v3);
              vertIds.Add(v4);

              writer.WriteLine(string.Format(template, v1 + 1, v2 + 1, v3 + 1));
              facesCount++;
            }
          }
        }
      }
      Console.WriteLine("Faces: " + facesCount);
    }

    private static void Animation1(string path)
    {
      var file = path + "unk2";
      if (!File.Exists(file))
        return;

      var data = File.ReadAllBytes(file);
      using (var fs = new FileStream(path + "animation.obj", FileMode.Create))
      {
        using (var writer = new StreamWriter(fs))
        {
          using (var stream = new MemoryStream(data))
          {
            while (stream.Position < stream.Length)
            {
              var vData = ReadBytes(stream, 64);

              for (int i = 0; i < 3; i++)
              {
                var idx = 16 * i;
                var x = BitConverter.ToSingle(vData, 0 + idx);
                var y= BitConverter.ToSingle(vData, 4 + idx);
                var z = BitConverter.ToSingle(vData, 8 + idx);
                var v4 = BitConverter.ToSingle(vData, 12 + idx);

                writer.WriteLine($"f {x} {y} {z}");
              }
            }
          }
        }
      }
    }

    private static void Movement(StreamWriter writer, MemoryStream stream)
    {
      var vData = ReadBytes(stream, 64);
      for (var i = 0; i < 3; i++)
      {
        var idx = i * 16;
        var x = Math.Round(BitConverter.ToSingle(vData, 0 + idx), 3);
        var y = Math.Round(BitConverter.ToSingle(vData, 4 + idx), 3);
        var z = Math.Round(BitConverter.ToSingle(vData, 8 + idx), 3);
        var w = Math.Round(BitConverter.ToSingle(vData, 12 + idx), 3);
        writer.WriteLine($"v {x} {y} {z}");
      }
    }

    private static byte[] ReadBytes(MemoryStream stream, int length)
    {
      var buffer = new byte[length];
      stream.Read(buffer, 0, length);
      return buffer;
    }
  }
}
