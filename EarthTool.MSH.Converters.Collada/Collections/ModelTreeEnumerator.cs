using Collada141;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EarthTool.MSH.Converters.Collada.Collections
{
  internal class ModelTreeEnumerator : IEnumerator<(Node Node, int BacktrackLevel)>
  {
    private readonly Node _root;
    private Stack<IEnumerator<Node>> _parentStack;
    private Node _current;
    private IEnumerator<Node> _currentLevel;
    private int _backtrackLevel;

    public ModelTreeEnumerator(Node root)
    {
      _root = root;
      _parentStack = new Stack<IEnumerator<Node>>();
    }

    public int BackTrackLevel => _backtrackLevel;

    public (Node Node, int BacktrackLevel) Current => (_current, _backtrackLevel);

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

      _currentLevel = _current.NodeProperty.GetEnumerator();

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
      if(_currentLevel != null)
      {
        while(_currentLevel.MoveNext())
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
