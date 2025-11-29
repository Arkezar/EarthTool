using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum StoreableFlags
  {
    None                        = 0x00,
    ResistantOnInfiniteDisabled = 0x01,
    ResistantOnHitSonic         = 0x02,
    AlwaysShadowed              = 0x04,
    NotEnemyTarget              = 0x08,
    NotSelectableActive         = 0x10,
    NeverShadowed               = 0x20,
    CTFUnitFlags                = 0x3B,
  }
}
