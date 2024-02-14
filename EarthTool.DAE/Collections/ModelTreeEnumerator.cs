using Collada141;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
    private Regex _regex;

    public ModelTreeEnumerator(COLLADA model, Node root)
    {
      _backtrackLevel = 0;
      _model = model;
      _root = root;
      _parentStack = new Stack<IEnumerator<Node>>();
      _regex = new Regex(@$"Part-(\d+)-(\d+)");
    }

    public ModelTreeNode Current => new ModelTreeNode(_model, _current, _backtrackLevel, _parentStack.Count);

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
      if (_current == null)
      {
        _current = _root;
      }

      if (_currentLevel != null)
      {
        _parentStack.Push(_currentLevel);
      }

      _currentLevel = _current.NodeProperty
        .Where(n => n.Name.StartsWith(_root.Name))
        .OrderBy(n => IsSubPart(n.Name))
        .ThenBy(n => GetPartNumber(n.Name))
        .GetEnumerator();

      _backtrackLevel = 0;
      while (BackTrack())
      {
        _currentLevel.Dispose();
        if (_parentStack.Count == 0)
        {
          return false;
        }

        _currentLevel = _parentStack.Pop();

        if (!IsSubPart(_currentLevel.Current.Name))
        {
          _backtrackLevel++;
        }
      }

      if (_currentLevel != null)
      {
        _current = _currentLevel.Current;
        return true;
      }

      return false;
    }

    private int GetPartNumber(string name)
    {
      var result = _regex.Match(name);
      int.TryParse(result.Groups[1].Value, out var partNumber);
      return partNumber;
    }

    private bool IsSubPart(string name)
    {
      var result = _regex.Match(name);
      return result.Success && int.Parse(result.Groups[2].Value) > 0;
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
      _currentLevel?.Dispose();
      _currentLevel = null;
      _parentStack.Clear();
      _backtrackLevel = 0;
    }

    public void Dispose()
    {
      Reset();
    }
  }
}