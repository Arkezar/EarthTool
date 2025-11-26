using System.Collections;
using System.Collections.Generic;

namespace EarthTool.MSH.Models
{
  public class PartNodeEnumerator : IEnumerator<PartNode>
  {
    private PartNode Root { get; }
    private Stack<IEnumerator<PartNode>> _parentStack;
    private IEnumerator<PartNode> _currentLevel;

    public PartNodeEnumerator(PartNode root)
    {
      _parentStack = new Stack<IEnumerator<PartNode>>();

      Root = root;
    }

    public bool MoveNext()
    {
      if (Current == null)
      {
        Current = Root;
        return true;
      }

      if (Current.Children.Count > 0)
      {
        if (_currentLevel != null)
        {
          _parentStack.Push(_currentLevel);
        }

        _currentLevel = Current.Children.GetEnumerator();
      }

      if (_currentLevel == null)
      {
        return false;
      }

      if (_currentLevel.MoveNext())
      {
        Current = _currentLevel.Current;
        return true;
      }

      do
      {
        _currentLevel.Dispose();

        if (!_parentStack.TryPop(out _currentLevel))
        {
          return false;
        }

        if (_currentLevel.MoveNext())
        {
          Current = _currentLevel.Current;
          return true;
        }
      } while (_parentStack.Count > 0);

      return false;
    }

    public void Reset()
    {
      Current = null;
      _currentLevel?.Dispose();
      _currentLevel = null;
      _parentStack.Clear();
    }

    public PartNode Current { get; private set; }

    object IEnumerator.Current => Current;

    public void Dispose()
    {
      Reset();
    }
  }
}
