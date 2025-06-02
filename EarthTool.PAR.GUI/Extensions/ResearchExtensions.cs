using EarthTool.PAR.GUI.ViewModels;
using EarthTool.PAR.Models;

namespace EarthTool.PAR.GUI.Extensions;

public static class ResearchExtensions
{
  public static ResearchViewModel ToViewModel(this Research research)
  {
    return new ResearchViewModel(research);
  }
}