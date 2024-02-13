using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class UnitTransporter : VerticalTransporter
  {
    public UnitTransporter()
    {
    }

    public UnitTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      UnitsCount = GetInteger(data);
      DockingHeight = GetInteger(data);
      AnimLoadingStartStart = GetInteger(data);
      AnimLoadingStartEnd = GetInteger(data);
      AnimLoadingEndStart = GetInteger(data);
      AnimLoadingEndEnd = GetInteger(data);
      AnimUnloadingStartStart = GetInteger(data);
      AnimUnloadingStartEnd = GetInteger(data);
      AnimUnloadingEndStart = GetInteger(data);
      AnimUnloadingEndEnd = GetInteger(data);
    }

    public int UnitsCount { get; set; }

    public int DockingHeight { get; set; }

    public int AnimLoadingStartStart { get; set; }

    public int AnimLoadingStartEnd { get; set; }

    public int AnimLoadingEndStart { get; set; }

    public int AnimLoadingEndEnd { get; set; }

    public int AnimUnloadingStartStart { get; set; }

    public int AnimUnloadingStartEnd { get; set; }

    public int AnimUnloadingEndStart { get; set; }

    public int AnimUnloadingEndEnd { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => UnitsCount,
        () => DockingHeight,
        () => AnimLoadingStartStart,
        () => AnimLoadingStartEnd,
        () => AnimLoadingEndStart,
        () => AnimLoadingEndEnd,
        () => AnimUnloadingStartStart,
        () => AnimUnloadingStartEnd,
        () => AnimUnloadingEndStart,
        () => AnimUnloadingEndEnd
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
          bw.Write(UnitsCount);
          bw.Write(DockingHeight);
          bw.Write(AnimLoadingStartStart);
          bw.Write(AnimLoadingStartEnd);
          bw.Write(AnimLoadingEndStart);
          bw.Write(AnimLoadingEndEnd);
          bw.Write(AnimUnloadingStartStart);
          bw.Write(AnimUnloadingStartEnd);
          bw.Write(AnimUnloadingEndStart);
          bw.Write(AnimUnloadingEndEnd);
        }

        return output.ToArray();
      }
    }
  }
}