using Collada141;
using EarthTool.DAE.Collections;
using EarthTool.MSH.Enums;
using EarthTool.MSH.Interfaces;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace EarthTool.DAE.Extensions
{
  public static class ModelPartExtensions
  {
    private const string MetadataProfile = "EARTHTOOL";
    private const string StaticPartElement = "msh_static_part";
    private const string BarrelMaximumAngleElement = "barrel_maximum_angle_degrees";

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

    public static void AddBarrelMaximumAngleMetadata(this Node node, IModelPart part)
    {
      if (!part.PartType.HasFlag(PartType.Barrel))
      {
        return;
      }

      var document = new XmlDocument();
      var metadata = document.CreateElement(StaticPartElement);
      metadata.SetAttribute("version", "1");
      var angle = document.CreateElement(BarrelMaximumAngleElement);
      angle.InnerText = part.RiseAngle.ToString("R", CultureInfo.InvariantCulture);
      metadata.AppendChild(angle);

      var technique = new Technique { Profile = MetadataProfile };
      technique.Any.Add(metadata);
      var extra = new Extra();
      extra.Technique.Add(technique);
      node.Extra.Add(extra);
    }

    public static double ParseBarrelMaximumAngle(this ModelTreeNode node, PartType partType)
    {
      if (!partType.HasFlag(PartType.Barrel))
      {
        return 0;
      }

      var metadata = node.Node.Extra
        .SelectMany(extra => extra.Technique)
        .Where(technique => technique.Profile == MetadataProfile)
        .SelectMany(technique => technique.Any)
        .SingleOrDefault(element => element.LocalName == StaticPartElement);
      if (metadata == null)
      {
        return 0;
      }

      if (metadata.GetAttribute("version") != "1")
      {
        throw new InvalidDataException("Unsupported EARTHTOOL static-part metadata version.");
      }

      var value = metadata.ChildNodes.OfType<XmlElement>()
        .SingleOrDefault(element => element.LocalName == BarrelMaximumAngleElement)?.InnerText;
      if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var angle))
      {
        throw new InvalidDataException("EARTHTOOL static-part metadata has an invalid barrel maximum angle.");
      }

      return angle;
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
