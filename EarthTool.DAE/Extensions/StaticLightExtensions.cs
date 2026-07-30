#nullable enable

using Collada141;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models.Elements;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml;
using Light = Collada141.Light;

namespace EarthTool.DAE.Extensions
{
  public static class StaticLightExtensions
  {
    private const string MetadataProfile = "EARTHTOOL";
    private const string MetadataElement = "msh_static_light";

    public static void AddStaticLightMetadata(this Light target, IStaticLight light, int sourceNumber)
    {
      var document = new XmlDocument();
      var metadata = document.CreateElement(MetadataElement);
      metadata.SetAttribute("version", "1");
      AddElement(document, metadata, "source_number", sourceNumber.ToString(CultureInfo.InvariantCulture));
      AddElement(document, metadata, "position", Format(light.Position));
      AddElement(document, metadata, "light_parameters", Format(light.LightParameters));
      switch (light)
      {
        case ISpotLight spot:
          AddElement(document, metadata, "horizontal_target_distance", Format(spot.HorizontalTargetDistance));
          AddElement(document, metadata, "target_heading", spot.TargetHeading.ToString(CultureInfo.InvariantCulture));
          AddElement(document, metadata, "reserved", $"{spot.Reserved1} {spot.Reserved2} {spot.Reserved3}");
          AddElement(document, metadata, "cone_half_angle_tangent", Format(spot.ConeHalfAngleTangent));
          AddElement(document, metadata, "distance_scaled_cone", Format(spot.DistanceScaledCone));
          AddElement(document, metadata, "vertical_target_slope", Format(spot.VerticalTargetSlope));
          AddElement(document, metadata, "final_parameter", Format(spot.FinalParameter));
          break;
        case IOmniLight omni:
          AddElement(document, metadata, "final_parameter", Format(omni.FinalParameter));
          break;
      }

      var technique = new Technique { Profile = MetadataProfile };
      technique.Any.Add(metadata);
      var extra = new Extra();
      extra.Technique.Add(technique);
      target.Extra.Add(extra);
    }

    public static IStaticLight? ParseStaticLightMetadata(this Light source, bool isSpot, int expectedSourceNumber)
    {
      var candidates = source.Extra
        .SelectMany(extra => extra.Technique)
        .Where(technique => technique.Profile == MetadataProfile)
        .SelectMany(technique => technique.Any)
        .Where(element => element.LocalName == MetadataElement)
        .ToArray();
      if (candidates.Length == 0)
      {
        return null;
      }

      if (candidates.Length != 1 || candidates[0].NamespaceURI.Length != 0)
      {
        throw InvalidMetadata("must contain exactly one unqualified msh_static_light element");
      }

      var metadata = candidates[0];
      if (metadata.GetAttribute("version") != "1")
      {
        throw new InvalidDataException("Unsupported EARTHTOOL static light metadata version.");
      }

      var sourceNumber = ParseInt(GetRequired(metadata, "source_number"), "source_number");
      if (sourceNumber != expectedSourceNumber)
      {
        throw InvalidMetadata("source_number conflicts with the numbered light name");
      }

      var position = ParseVector(GetRequired(metadata, "position"), "position");
      var lightParameters = ParseVector(GetRequired(metadata, "light_parameters"), "light_parameters");
      if (!isSpot)
      {
        return new OmniLight
        {
          Position = position,
          LightParameters = lightParameters,
          FinalParameter = ParseFloat(GetRequired(metadata, "final_parameter"), "final_parameter")
        };
      }

      var reserved = ParseBytes(GetRequired(metadata, "reserved"), "reserved");
      return new SpotLight
      {
        Position = position,
        LightParameters = lightParameters,
        HorizontalTargetDistance = ParseFloat(GetRequired(metadata, "horizontal_target_distance"), "horizontal_target_distance"),
        TargetHeading = ParseByte(GetRequired(metadata, "target_heading"), "target_heading"),
        Reserved1 = reserved[0],
        Reserved2 = reserved[1],
        Reserved3 = reserved[2],
        ConeHalfAngleTangent = ParseFloat(GetRequired(metadata, "cone_half_angle_tangent"), "cone_half_angle_tangent"),
        DistanceScaledCone = ParseFloat(GetRequired(metadata, "distance_scaled_cone"), "distance_scaled_cone"),
        VerticalTargetSlope = ParseFloat(GetRequired(metadata, "vertical_target_slope"), "vertical_target_slope"),
        FinalParameter = ParseFloat(GetRequired(metadata, "final_parameter"), "final_parameter")
      };
    }

    private static void AddElement(XmlDocument document, XmlElement parent, string name, string value)
    {
      var element = document.CreateElement(name);
      element.InnerText = value;
      parent.AppendChild(element);
    }

    private static string Format(Vector3 value)
      => $"{Format(value.X)} {Format(value.Y)} {Format(value.Z)}";

    private static string Format(float value)
      => value.ToString("R", CultureInfo.InvariantCulture);

    private static string GetRequired(XmlElement metadata, string name)
    {
      var elements = metadata.ChildNodes.OfType<XmlElement>()
        .Where(element => element.LocalName == name && element.NamespaceURI.Length == 0)
        .ToArray();
      if (elements.Length != 1)
      {
        throw InvalidMetadata($"requires exactly one {name} element");
      }

      return elements[0].InnerText;
    }

    private static Vector3 ParseVector(string value, string name)
    {
      var values = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if (values.Length != 3)
      {
        throw InvalidMetadata($"has an invalid {name} vector");
      }

      return new Vector3(ParseFloat(values[0], name), ParseFloat(values[1], name), ParseFloat(values[2], name));
    }

    private static byte[] ParseBytes(string value, string name)
    {
      var values = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if (values.Length != 3)
      {
        throw InvalidMetadata($"has an invalid {name} value");
      }

      return values.Select(item => ParseByte(item, name)).ToArray();
    }

    private static float ParseFloat(string value, string name)
    {
      if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ||
          float.IsNaN(result) || float.IsInfinity(result))
      {
        throw InvalidMetadata($"has an invalid {name} value");
      }

      return result;
    }

    private static int ParseInt(string value, string name)
    {
      if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
      {
        throw InvalidMetadata($"has an invalid {name} value");
      }

      return result;
    }

    private static byte ParseByte(string value, string name)
    {
      if (!byte.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
      {
        throw InvalidMetadata($"has an invalid {name} value");
      }

      return result;
    }

    private static InvalidDataException InvalidMetadata(string detail)
      => new InvalidDataException($"EARTHTOOL static light metadata {detail}.");
  }
}
