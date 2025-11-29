namespace EarthTool.PAR.Enums
{
  public enum StandType
  {
    None           = 0x00,
    Accurate       = 0x01,
    Coarsely       = 0x02,
    Swing          = 0x03,
    Turn           = 0x04,
    MoveDownSmall  = 0x10,
    MoveDownMedium = 0x20,
    MoveDownBig    = 0x30,
    ScaleSmall     = 0x40,
    ScaleMedium    = 0x80,
    ScaleBig       = 0xC0,
    MoveSmall      = 0x100,
    MoveMedium     = 0x200,
    MoveBig        = 0x300,
    Tree           = 0x3C4,
    Rock           = 0x194,
  }
}
