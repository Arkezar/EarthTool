using Collada141;
using EarthTool.MSH.Interfaces;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace EarthTool.DAE.Extensions
{
  public static class SlotExtensions
  {
    private const string MetadataProfile = "EARTHTOOL";
    private const string AttachmentElement = "msh_attachment";
    private const string ExtraAngleElement = "extra_angle";

    public static void AddAttachmentMetadata(this Node node, ISlot slot)
    {
      var document = new XmlDocument();
      var metadata = document.CreateElement(AttachmentElement);
      metadata.SetAttribute("version", "1");
      var extraAngle = document.CreateElement(ExtraAngleElement);
      extraAngle.InnerText = slot.ExtraAngle.ToString(CultureInfo.InvariantCulture);
      metadata.AppendChild(extraAngle);

      var technique = new Technique { Profile = MetadataProfile };
      technique.Any.Add(metadata);
      var extra = new Extra();
      extra.Technique.Add(technique);
      node.Extra.Add(extra);
    }

    public static byte ParseAttachmentExtraAngle(this Node node)
    {
      var metadata = node.Extra
        .SelectMany(extra => extra.Technique)
        .Where(technique => technique.Profile == MetadataProfile)
        .SelectMany(technique => technique.Any)
        .SingleOrDefault(element => element.LocalName == AttachmentElement);
      if (metadata == null)
      {
        return 0x80;
      }

      if (metadata.GetAttribute("version") != "1")
      {
        throw new InvalidDataException("Unsupported EARTHTOOL attachment metadata version.");
      }

      var value = metadata.ChildNodes.OfType<XmlElement>()
        .SingleOrDefault(element => element.LocalName == ExtraAngleElement)?.InnerText;
      if (!byte.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var extraAngle))
      {
        throw new InvalidDataException("EARTHTOOL attachment metadata has an invalid extra angle.");
      }

      return extraAngle;
    }
  }
}
