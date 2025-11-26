using EarthTool.Common.Interfaces;
using EarthTool.MSH.Models.Elements;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface IModelSlots : IBinarySerializable
  {
    IEnumerable<ISlot> BarrelMuzzels { get; }
    IEnumerable<ISlot> CenterPivot { get; }
    IEnumerable<ISlot> Chimneys { get; }
    IEnumerable<ISlot> Exhausts { get; }
    IEnumerable<ISlot> Headlights { get; }
    IEnumerable<ISlot> HitSpots { get; }
    IEnumerable<ISlot> InterfacePivot { get; }
    IEnumerable<ISlot> KeelTraces { get; }
    IEnumerable<ISlot> LandingSpot { get; }
    IEnumerable<ISlot> Omnilights { get; }
    IEnumerable<ISlot> ProductionSpotEnd { get; }
    IEnumerable<ISlot> ProductionSpotStart { get; }
    IEnumerable<ISlot> SmokeSpots { get; }
    IEnumerable<ISlot> SmokeTraces { get; }
    IEnumerable<ISlot> TurretMuzzels { get; }
    IEnumerable<ISlot> Turrets { get; }
    IEnumerable<ISlot> Unknown { get; }
    IEnumerable<ISlot> UnloadPoints { get; }
  }
}
