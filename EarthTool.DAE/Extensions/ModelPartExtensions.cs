using EarthTool.DAE.Collections;
using EarthTool.MSH.Enums;
using EarthTool.MSH.Interfaces;
using System.Linq;
using System.Text.RegularExpressions;

namespace EarthTool.DAE.Extensions
{
  public static class ModelPartExtensions
  {
    public static string EnrichPartName(this IModelPart part, string baseName)
      => $"{baseName}-{part.GetAnimationDetails()}";

    public static string GetAnimationDetails(this IModelPart part)
    {
      var partType = part.PartType switch
      {
        var p when p.HasFlag(PartType.Barrel) => "L",
        var p when p.HasFlag(PartType.Rotor) => "R",
        var p when p.HasFlag(PartType.Subpart) => "P",
        _ => "B"
      };

      var frameCount = new[]
      {
        part.Animations.TranslationFrames.Count(), part.Animations.RotationFrames.Count(),
        part.Animations.ScaleFrames.Count()
      }.Max();
      var animType = part.AnimationType switch
      {
        _ when frameCount == 0 => string.Empty,
        AnimationType.Looped => "A",
        AnimationType.TwoWay => "B",
        AnimationType.Single => "C",
        AnimationType.Lift => "D",
        _ => string.Empty
      };

      var frames = string.IsNullOrWhiteSpace(animType) ? string.Empty : frameCount.ToString();
      return $"{partType}{animType}{frames}";
    }

    public static (PartType PartType, AnimationType AnimationType, int FrameCount) ParseAnimationDetails(
      this ModelTreeNode node)
    {
      var regex = new Regex(@"([BPLR])(([ABCD])(\d+))?$");
      var matches = regex.Match(node.Node.Name);

      if (matches.Success)
      {
        return (matches.Groups[1].Success, matches.Groups[3].Success, matches.Groups[4].Success) switch
        {
          (true, false, false) => (GetPartType(matches.Groups[1].Value), AnimationType.Looped, 0),
          (true, true, true) => (GetPartType(matches.Groups[1].Value), GetAnimationType(matches.Groups[3].Value), int.Parse(matches.Groups[4].Value)),
          _ => (PartType.Base, AnimationType.Looped, 0)
        };
      }

      return (PartType.Base, AnimationType.Looped, 0);
    }

    private static PartType GetPartType(string name)
    {
      return name switch
      {
        "B" => PartType.Base,
        "P" => PartType.Subpart,
        "L" => PartType.Subpart | PartType.Barrel,
        "R" => PartType.Subpart | PartType.Rotor,
        _ => PartType.Base
      };
    }

    private static AnimationType GetAnimationType(string name)
    {
      return name switch
      {
        "A" => AnimationType.Looped,
        "B" => AnimationType.TwoWay,
        "C" => AnimationType.Single,
        "D" => AnimationType.Lift,
        _ => AnimationType.Looped
      };
    }
  }
}
