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

    public Slot InterfacePivot
    {
      get;
    }

    public Slot CenterPivot
    {
      get;
    }

    public Slot ProductionSpotStart
    {
      get;
    }

    public Slot ProductionSpotEnd
    {
      get;
    }

    public Slot LandingSpot
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
      InterfacePivot = new Slot(stream, 0);
      CenterPivot = new Slot(stream, 0);
      ProductionSpotStart = new Slot(stream, 0);
      ProductionSpotEnd = new Slot(stream, 0);
      LandingSpot = new Slot(stream, 0);
    }

    private IEnumerable<Slot> GetSlots(Stream stream, int count)
    {
      return Enumerable.Range(0, count).Select(i => new Slot(stream, ++i)).ToList();
    }
  }
}
