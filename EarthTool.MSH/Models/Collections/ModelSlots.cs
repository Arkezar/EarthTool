using EarthTool.MSH.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models.Collections
{
  public class ModelSlots : IModelSlots
  {
    public IEnumerable<ISlot> Turrets { get; set; }

    public IEnumerable<ISlot> BarrelMuzzels { get; set; }

    public IEnumerable<ISlot> TurretMuzzels { get; set; }

    public IEnumerable<ISlot> Headlights { get; set; }

    public IEnumerable<ISlot> Omnilights { get; set; }

    public IEnumerable<ISlot> UnloadPoints { get; set; }

    public IEnumerable<ISlot> HitSpots { get; set; }

    public IEnumerable<ISlot> SmokeSpots { get; set; }

    public IEnumerable<ISlot> Unknown { get; set; }

    public IEnumerable<ISlot> Chimneys { get; set; }

    public IEnumerable<ISlot> SmokeTraces { get; set; }

    public IEnumerable<ISlot> Exhausts { get; set; }

    public IEnumerable<ISlot> KeelTraces { get; set; }

    public IEnumerable<ISlot> InterfacePivot { get; set; }

    public IEnumerable<ISlot> CenterPivot { get; set; }

    public IEnumerable<ISlot> ProductionSpotStart { get; set; }

    public IEnumerable<ISlot> ProductionSpotEnd { get; set; }

    public IEnumerable<ISlot> LandingSpot { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      var data = Turrets.Concat(BarrelMuzzels)
                        .Concat(TurretMuzzels)
                        .Concat(Headlights)
                        .Concat(Omnilights)
                        .Concat(UnloadPoints)
                        .Concat(HitSpots)
                        .Concat(SmokeSpots)
                        .Concat(Unknown)
                        .Concat(Chimneys)
                        .Concat(SmokeTraces)
                        .Concat(Exhausts)
                        .Concat(KeelTraces)
                        .Concat(InterfacePivot)
                        .Concat(CenterPivot)
                        .Concat(ProductionSpotStart)
                        .Concat(ProductionSpotEnd)
                        .Concat(LandingSpot);
      return data.SelectMany(s => s.ToByteArray(encoding)).ToArray();
    }
  }
}