using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class ContainerTransporter : Equipment
  {
    public ContainerTransporter()
    {
    }

    public ContainerTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      AnimContainerDownStart = GetInteger(data);
      AnimContainerDownEnd = GetInteger(data);
      AnimContainerUpStart = GetInteger(data);
      AnimContainerUpEnd = GetInteger(data);
    }

    public int AnimContainerDownStart { get; set; }

    public int AnimContainerDownEnd { get; set; }

    public int AnimContainerUpStart { get; set; }

    public int AnimContainerUpEnd { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => AnimContainerDownStart,
        () => AnimContainerDownEnd,
        () => AnimContainerUpStart,
        () => AnimContainerUpEnd
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(AnimContainerDownStart);
          bw.Write(AnimContainerDownEnd);
          bw.Write(AnimContainerUpStart);
          bw.Write(AnimContainerUpEnd);
        }

        return output.ToArray();
      }
    }
  }
}
