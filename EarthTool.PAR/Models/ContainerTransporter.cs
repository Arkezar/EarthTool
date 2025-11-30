using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
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
      AnimContainerDownStart = data.ReadInteger();
      AnimContainerDownEnd = data.ReadInteger();
      AnimContainerUpStart = data.ReadInteger();
      AnimContainerUpEnd = data.ReadInteger();
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
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(AnimContainerDownStart);
      bw.Write(AnimContainerDownEnd);
      bw.Write(AnimContainerUpStart);
      bw.Write(AnimContainerUpEnd);

      return output.ToArray();
    }
  }
}
