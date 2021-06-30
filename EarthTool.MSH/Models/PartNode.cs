using System.Collections.Generic;

namespace EarthTool.MSH.Models
{
  public class PartNode
  {
    public int Id
    {
      get;
    }

    public ModelPart Part
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

    public PartNode(int id, ModelPart part = null, PartNode parent = null)
    {
      Children = new List<PartNode>();
      Id = id;
      Part = part;
      Parent = parent;
      if (parent != null)
      {
        parent.Children.Add(this);
      }
    }
  }
}
