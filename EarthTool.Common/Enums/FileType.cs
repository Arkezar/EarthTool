using System.ComponentModel;

namespace EarthTool.Common.Enums
{
  public enum FileType
  {
    [Description("Earth archive file")]
    WD,
    [Description("Earth texture file")]
    TEX,
    [Description("Earth mesh file")]
    MSH,
    [Description("Earth data file")]
    DAT,
    [Description("Earth interface file")]
    INT,
    [Description("Plain text file")]
    TXT,
    [Description("Earth language file")]
    LAN,
    [Description("Earth map file")]
    LND,
    [Description("Earth mission file")]
    MIS,
    [Description("Earth parameters file")]
    PAR,
    [Description("Compiled Earth C file")]
    ECO,
    [Description("Compiled Moon C file")]
    ECOMP,
    [Description("Earth sound pack information")]
    WPK,
    [Description("Information file")]
    INF,
    [Description("")]
    LGH,
    [Description("")]
    ATD,
    [Description("Sound wave file")]
    WAV
  }
}
