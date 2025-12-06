using EarthTool.PAR.Models;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for a research entry.
/// </summary>
public class ResearchViewModel : ViewModelBase
{
  private readonly Research _research;

  public ResearchViewModel(Research research)
  {
    _research = research ?? throw new System.ArgumentNullException(nameof(research));
  }

  /// <summary>
  /// Gets the underlying research model.
  /// </summary>
  public Research Research => _research;

  /// <summary>
  /// Gets the research name.
  /// </summary>
  public string Name => _research.Name;
}
