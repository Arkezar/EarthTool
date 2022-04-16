using Collada141;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.MSH.Converters.Collada.Collections
{
  internal class ModelTree : IEnumerable<(Node Node, int BacktrackLevel)>
  {
    private readonly Node _root;

    public ModelTree(COLLADA model)
    {
      _root = model.Library_Visual_Scenes.Single().Visual_Scene.Single().Node.Single();
    }

    public string Name => _root.Name;

    public IEnumerator<(Node Node, int BacktrackLevel)> GetEnumerator()
      => new ModelTreeEnumerator(_root);

    IEnumerator IEnumerable.GetEnumerator()
      => GetEnumerator();
  }
}
