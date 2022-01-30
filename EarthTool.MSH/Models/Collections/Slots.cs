using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models.Collections
{
  public class Slots
  {
    public IEnumerable<Slot> Turrets
    {
      get;
    }

    public IEnumerable<Slot> BarrelMuzzels
    {
      get;
    }

    public IEnumerable<Slot> TurretMuzzels
    {
      get;
    }

    public IEnumerable<Slot> Headlights
    {
      get;
    }

    public IEnumerable<Slot> Omnilights
    {
      get;
    }

    public IEnumerable<Slot> UnloadPoints
    {
      get;
    }

    public IEnumerable<Slot> HitSpots
    {
      get;
    }

    public IEnumerable<Slot> SmokeSpots
    {
      get;
    }

    public IEnumerable<Slot> Unknown
    {
      get;
    }

    public IEnumerable<Slot> Chimneys
    {
      get;
    }

    public IEnumerable<Slot> SmokeTraces
    {
      get;
    }

    public IEnumerable<Slot> Exhausts
    {
      get;
    }

    public IEnumerable<Slot> KeelTraces
    {
      get;
    }

    public IEnumerable<Slot> InterfacePivot
    {
      get;
    }

    public IEnumerable<Slot> CenterPivot
    {
      get;
    }

    public IEnumerable<Slot> ProductionSpotStart
    {
      get;
    }

    public IEnumerable<Slot> ProductionSpotEnd
    {
      get;
    }

    public IEnumerable<Slot> LandingSpot
    {
      get;
    }


    public Slots(Stream stream)
    {
      Turrets = GetSlots(stream, 4);
      BarrelMuzzels = GetSlots(stream, 4);
      TurretMuzzels = GetSlots(stream, 4);
      Headlights = GetSlots(stream, 4);
      Omnilights = GetSlots(stream, 4);
      UnloadPoints = GetSlots(stream, 4);
      HitSpots = GetSlots(stream, 4);
      SmokeSpots = GetSlots(stream, 4);
      Unknown = GetSlots(stream, 4);
      Chimneys = GetSlots(stream, 2);
      SmokeTraces = GetSlots(stream, 2);
      Exhausts = GetSlots(stream, 2);
      KeelTraces = GetSlots(stream, 2);
      InterfacePivot = GetSlots(stream, 1);
      CenterPivot = GetSlots(stream, 1);
      ProductionSpotStart = GetSlots(stream, 1);
      ProductionSpotEnd = GetSlots(stream, 1);
      LandingSpot = GetSlots(stream, 1);
    }

    public byte[] ToByteArray()
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
      return data.SelectMany(s => s.ToByteArray()).ToArray();
    }

    private IEnumerable<Slot> GetSlots(Stream stream, int count)
    {
      return Enumerable.Range(0, count).Select(i => new Slot(stream, ++i)).ToList();
    }
  }
}
