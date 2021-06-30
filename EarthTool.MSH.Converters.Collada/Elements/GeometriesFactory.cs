using Collada141;
using EarthTool.MSH.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EarthTool.MSH.Converters.Collada.Elements
{
  public class GeometriesFactory
  {
    public IEnumerable<(Geometry Geometry, Node GeometryNode)> GetGeometries(IEnumerable<ModelPart> parts, string modelName)
    {
      return parts.Select((part, i) =>
      {
        return (GetGeometry(part, i, modelName), GetGeometryNode(part, i, modelName));
      });
    }

    public Node GetGeometryRootNode(IEnumerable<Node> geometryNodes, PartNode partsTree, string modelName)
    {
      var rootNode = new Node()
      {
        Id = modelName,
        Name = modelName
      };

      var root = geometryNodes.Single(g => g.Id == $"{modelName}-Part-{partsTree.Id}");
      foreach (var child in partsTree.Children)
      {
        root.NodeProperty.Add(GetGeometryRootNode(geometryNodes, child, modelName));
      }
      return root;
    }

    private Node GetGeometryNode(ModelPart part, int i, string modelName)
    {
      var id = $"{modelName}-Part-{i}";
      var node = new Node()
      {
        Id = id,
        Name = id
      };

      node.Matrix.Add(new Matrix
      {
        Sid = "transform",
        Value = string.Format(CultureInfo.InvariantCulture, "1 0 0 {0} 0 1 0 {1} 0 0 1 {2} 0 0 0 1", part.Offset.X, part.Offset.Y, part.Offset.Z)
      });

      var instanceGeometry = new Instance_Geometry()
      {
        Url = $"#{id}",
        Bind_Material = new Bind_Material()
      };

      var instanceMaterial = new Instance_Material
      {
        Symbol = $"{id}-material",
        Target = $"#{id}-material",
      };
      instanceMaterial.Bind_Vertex_Input.Add(new Instance_MaterialBind_Vertex_Input
      {
        Input_Set = 0,
        Input_Semantic = "TEXCOORD",
        Semantic = "UVMap"
      });

      instanceGeometry.Bind_Material.Technique_Common.Add(instanceMaterial);

      node.Instance_Geometry.Add(instanceGeometry);

      return node;
    }

    private Geometry GetGeometry(ModelPart part, int i, string modelName)
    {
      var id = $"{modelName}-Part-{i}";

      var geometry = new Geometry
      {
        Name = id,
        Id = id
      };

      var positions = GetSource("positions", part.Vertices, v => new float[] { v.Position.X, v.Position.Y, v.Position.Z });
      var normals = GetSource("normals", part.Vertices, v => new float[] { v.Normal.X, v.Normal.Y, v.Normal.Z });
      var uv = GetMapSource("map", part.Vertices, v => new float[] { v.U, v.V });

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
        P = string.Join(' ', part.Faces.SelectMany(f => new string[] { f.V1.ToString(), f.V2.ToString(), f.V3.ToString() })),
        Material = id + "-material"
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
        Offset = 0
      });

      poly.Input.Add(new InputLocalOffset()
      {
        Semantic = "TEXCOORD",
        Source = "#map",
        Offset = 0
      });

      var mesh = new Mesh
      {
        Vertices = vertices
      };

      mesh.Source.Add(positions);
      mesh.Source.Add(normals);
      mesh.Source.Add(uv);
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
        Value = string.Join(" ", data.SelectMany(func).Select(v => v.ToString(CultureInfo.InvariantCulture)))
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

    private Source GetMapSource<T>(string name, IList<T> data, Func<T, IEnumerable<float>> func)
    {
      var source = new Source
      {
        Id = name,
        Name = name
      };
      var sourceArray = new Float_Array
      {
        Id = $"{name}-array",
        Count = (ulong)data.Count * 2,
        Value = string.Join(" ", data.SelectMany(func).Select(v => v.ToString(CultureInfo.InvariantCulture)))
      };
      source.Float_Array = sourceArray;
      source.Technique_Common = new SourceTechnique_Common
      {
        Accessor = new Accessor
        {
          Source = $"#{name}-array",
          Count = (ulong)data.Count,
          Stride = 2
        }
      };
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "S",
        Type = "float"
      });
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "T",
        Type = "float"
      });
      return source;
    }
  }
}
