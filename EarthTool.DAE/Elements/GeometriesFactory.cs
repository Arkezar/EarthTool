﻿using Collada141;
using EarthTool.DAE.Extensions;
using EarthTool.MSH.Enums;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EarthTool.DAE.Elements
{
  public class GeometriesFactory
  {
    public IEnumerable<Geometry> GetGeometries(IEnumerable<PartNode> parts, string modelName)
    {
      return parts.SelectMany((p, i) => p.Parts.Select((sp, idx) => GetGeometry(sp, i, idx, modelName)));
    }

    public IEnumerable<Node> GetGeometryNodes(IEnumerable<PartNode> parts, IEnumerable<Node> emitterNodes, string modelName)
    {
      return parts.SelectMany((p, i) => p.Parts.Select((sp, idx) => GetGeometryNode(sp, i, idx, emitterNodes, modelName)));
    }

    public Node GetGeometryRootNode(IEnumerable<Node> geometryNodes, PartNode partsTree, string modelName)
    {
      var partNodes = geometryNodes.Where(g => g.Id.Contains($"Part-{partsTree.Id}-")).OrderBy(p => p.Id);
      var root = partNodes.First();

      foreach (var subpart in partNodes.Skip(1))
      {
        root.NodeProperty.Add(subpart);
      }

      foreach (var child in partsTree.Children)
      {
        root.NodeProperty.Add(GetGeometryRootNode(geometryNodes, child, modelName));
      }

      return root;
    }

    private Node GetGeometryNode(IModelPart part, int i, int idx, IEnumerable<Node> emitterNodes, string modelName)
    {
      var id = part.EnrichPartName($"Part-{i}-{idx}");
      var node = new Node() { Id = id, Name = id };

      if (idx == 0)
      {
        node.Matrix.Add(new Matrix
        {
          Sid = "transform",
          Value = string.Format(CultureInfo.InvariantCulture, "1 0 0 {0} 0 1 0 {1} 0 0 1 {2} 0 0 0 1",
            part.Offset.X,
            part.Offset.Y,
            part.Offset.Z)
        });
      }

      var instanceGeometry = new Instance_Geometry() { Url = $"#{id}", Bind_Material = new Bind_Material() };

      var instanceMaterial =
        new Instance_Material { Symbol = $"{id}-material", Target = $"#{id}-material", };
      instanceMaterial.Bind_Vertex_Input.Add(new Instance_MaterialBind_Vertex_Input
      {
        Input_Set = 0, Input_Semantic = "TEXCOORD", Semantic = "UVMap"
      });

      instanceGeometry.Bind_Material.Technique_Common.Add(instanceMaterial);
      node.Instance_Geometry.Add(instanceGeometry);

      if (part.PartType.HasFlag(PartType.Emitter1))
      {
        node.NodeProperty.Add(emitterNodes.ElementAt(0));
      }
      
      if (part.PartType.HasFlag(PartType.Emitter2))
      {
        node.NodeProperty.Add(emitterNodes.ElementAt(1));
      }
      
      if (part.PartType.HasFlag(PartType.Emitter3))
      {
        node.NodeProperty.Add(emitterNodes.ElementAt(2));
      }
      
      if (part.PartType.HasFlag(PartType.Emitter4))
      {
        node.NodeProperty.Add(emitterNodes.ElementAt(3));
      }
      
      return node;
    }

    private Geometry GetGeometry(IModelPart part, int i, int idx, string modelName)
    {
      var id = part.EnrichPartName($"Part-{i}-{idx}");

      var geometry = new Geometry { Name = id, Id = id };

      var positions = GetSource("positions", part.Vertices,
        v => new float[] { v.Position.X, v.Position.Y, v.Position.Z });
      var normals = GetSource("normals", part.Vertices, v => new float[] { v.Normal.X, v.Normal.Y, v.Normal.Z });
      var uv = GetMapSource("map", part.Vertices, v => new float[] { v.TextureCoordinate.S, v.TextureCoordinate.T });

      var vertices = new Vertices() { Id = "vertices" };

      vertices.Input.Add(new InputLocal() { Semantic = "POSITION", Source = "#positions" });

      var poly = new Polylist
      {
        Count = (ulong)part.Faces.Count(),
        Vcount = string.Join(' ', Enumerable.Repeat(3, part.Faces.Count())),
        P = string.Join(' ',
          part.Faces.SelectMany(f => new string[] { f.V1.ToString(), f.V2.ToString(), f.V3.ToString() })),
        Material = $"{id}-material"
      };

      poly.Input.Add(new InputLocalOffset() { Semantic = "VERTEX", Source = "#vertices", Offset = 0 });

      poly.Input.Add(new InputLocalOffset() { Semantic = "NORMAL", Source = "#normals", Offset = 0 });

      poly.Input.Add(new InputLocalOffset() { Semantic = "TEXCOORD", Source = "#map", Offset = 0 });

      var mesh = new Mesh { Vertices = vertices };

      mesh.Source.Add(positions);
      mesh.Source.Add(normals);
      mesh.Source.Add(uv);
      mesh.Polylist.Add(poly);

      geometry.Mesh = mesh;
      return geometry;
    }

    private Source GetSource<T>(string name, IEnumerable<T> data, Func<T, IEnumerable<float>> func)
    {
      var source = new Source { Id = name, Name = name };
      var sourceArray = new Float_Array
      {
        Id = $"{name}-array",
        Count = (ulong)data.Count() * 3,
        Value = string.Join(" ", data.SelectMany(func).Select(v => v.ToString(CultureInfo.InvariantCulture)))
      };
      source.Float_Array = sourceArray;
      source.Technique_Common = new SourceTechnique_Common
      {
        Accessor = new Accessor { Source = $"#{name}-array", Count = (ulong)data.Count(), Stride = 3 }
      };
      source.Technique_Common.Accessor.Param.Add(new Param() { Name = "X", Type = "float" });
      source.Technique_Common.Accessor.Param.Add(new Param() { Name = "Y", Type = "float" });
      source.Technique_Common.Accessor.Param.Add(new Param() { Name = "Z", Type = "float" });
      return source;
    }

    private Source GetMapSource<T>(string name, IEnumerable<T> data, Func<T, IEnumerable<float>> func)
    {
      var source = new Source { Id = name, Name = name };
      var sourceArray = new Float_Array
      {
        Id = $"{name}-array",
        Count = (ulong)data.Count() * 2,
        Value = string.Join(" ", data.SelectMany(func).Select(v => v.ToString(CultureInfo.InvariantCulture)))
      };
      source.Float_Array = sourceArray;
      source.Technique_Common = new SourceTechnique_Common
      {
        Accessor = new Accessor { Source = $"#{name}-array", Count = (ulong)data.Count(), Stride = 2 }
      };
      source.Technique_Common.Accessor.Param.Add(new Param() { Name = "S", Type = "float" });
      source.Technique_Common.Accessor.Param.Add(new Param() { Name = "T", Type = "float" });
      return source;
    }
  }
}