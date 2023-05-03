using Collada141;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace EarthTool.DAE.Collections
{
  public class ModelTreeNode
  {
    public COLLADA Model { get; }
    public Node Node { get; }
    public int BacktrackLevel { get; }
    public int Depth { get; }

    public IEnumerable<Animation> Animations =>
      Model.Library_Animations.SelectMany(la => la.Animation)
        .Where(a => a.Channel.Any(c => c.Target.Equals($"{Node.Id}/transform")));

    public Geometry Geometry =>
      Model.Library_Geometries.SelectMany(lg => lg.Geometry)
        .Single(g => g.Id == Node.Instance_Geometry.First().Url.TrimStart('#'));

    public IEnumerable<Material> Materials =>
      Model.Library_Materials.SelectMany(lm => lm.Material)
        .Where(m => Node.Instance_Geometry.Any(ie =>
          ie.Bind_Material.Technique_Common.Any(t => t.Target.TrimStart('#') == m.Id)));

    public Matrix TransformationMatrix => Node.MatrixSpecified ? Node.Matrix.First() : null;

    public ModelTreeNode(COLLADA model, Node node, int backtrackLevel, int depth)
    {
      Model = model;
      Node = node;
      BacktrackLevel = backtrackLevel;
      Depth = depth;
    }
  }
}