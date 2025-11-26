using Collada141;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.DAE.Collections
{
  internal class ModelTree : IEnumerable<ModelTreeNode>
  {
    private readonly COLLADA _model;
    private readonly Node _root;

    public ModelTree(COLLADA model)
    {
      _model = model;
      _root = model.Library_Visual_Scenes.Single().Visual_Scene.Single().Node.Single();
    }

    public string Name => _root.Name;

    public IEnumerator<ModelTreeNode> GetEnumerator()
      => new ModelTreeEnumerator(_model, _root);

    IEnumerator IEnumerable.GetEnumerator()
      => GetEnumerator();
  }
}
