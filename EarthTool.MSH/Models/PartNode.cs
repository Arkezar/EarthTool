using EarthTool.MSH.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace EarthTool.MSH.Models
{
  public class PartNode : IEnumerable<PartNode>
  {
    public int Id
    {
      get;
    }

    public List<IModelPart> Parts
    {
      get;
    }

    public PartNode Parent
    {
      get;
    }

    public List<PartNode> Children
    {
      get;
    }

    public PartNode(int id, IModelPart part = null, PartNode parent = null)
    {
      Parts = new List<IModelPart>();
      Parts.Add(part);
      Children = new List<PartNode>();
      Id = id;
      Parent = parent;
      if (parent != null)
      {
        parent.Children.Add(this);
      }
    }

    public IEnumerator<PartNode> GetEnumerator()
    {
      return new PartNodeEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}