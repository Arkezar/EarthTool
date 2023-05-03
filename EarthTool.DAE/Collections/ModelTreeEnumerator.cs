using Collada141;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.DAE.Collections
{
  internal class ModelTreeEnumerator : IEnumerator<ModelTreeNode>
  {
    private readonly COLLADA _model;
    private readonly Node _root;
    private Stack<IEnumerator<Node>> _parentStack;
    private Node _current;
    private IEnumerator<Node> _currentLevel;
    private int _backtrackLevel;

    public ModelTreeEnumerator(COLLADA model, Node root)
    {
      _model = model;
      _root = root;
      _parentStack = new Stack<IEnumerator<Node>>();
    }

    public ModelTreeNode Current => new ModelTreeNode(_model, _current, _backtrackLevel, _parentStack.Count);

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
      _backtrackLevel = 0;

      if (_current == null)
      {
        _current = _root;
      }

      if (_currentLevel != null)
      {
        _parentStack.Push(_currentLevel);
      }

      _currentLevel = _current.NodeProperty.OrderBy(n => n.Name).GetEnumerator();

      while (BackTrack())
      {
        _currentLevel.Dispose();
        if (_parentStack.Count == 0)
        {
          return false;
        }

        _currentLevel = _parentStack.Pop();
        _backtrackLevel++;
      }

      if (_currentLevel != null)
      {
        _current = _currentLevel.Current;
        return true;
      }

      return false;
    }

    private bool BackTrack()
    {
      var modelName = _root.Name;
      if (_currentLevel != null)
      {
        while (_currentLevel.MoveNext())
        {
          if (_currentLevel.Current.Name.StartsWith(modelName))
          {
            return false;
          }
        }

        return true;
      }

      return false;
    }

    public void Reset()
    {
      _current = _root;
    }

    public void Dispose()
    {
    }
  }
}