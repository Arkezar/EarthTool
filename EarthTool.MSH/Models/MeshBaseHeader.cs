using EarthTool.Common;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class MeshBaseHeader : IMeshBaseHeader
  {
    public const uint SupportedVersion = 1;
    public const int SerializedSize = 0x368;

    public MeshKind MeshKind { get; set; }

    public uint BoxPresenceMask { get; set; }

    public IMeshFrames Frames { get; set; }

    public int HeaderFlags { get; set; }

    public IEnumerable<IVector> MountPoints { get; set; }

    public IEnumerable<ISpotLight> SpotLights { get; set; }

    public IEnumerable<IOmniLight> OmnidirectionalLights { get; set; }

    public IMeshFootprint Footprint { get; set; }

    public IModelSlots Slots { get; set; }

    public IMeshHorizontalExtents HorizontalExtents { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      ValidateState();
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(Identifiers.Mesh);
          bw.Write((int)MeshKind);
          bw.Write(BoxPresenceMask);
          bw.Write(Frames.ToByteArray(encoding));
          bw.Write(HeaderFlags);
          bw.Write(MountPoints.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          bw.Write(SpotLights.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          bw.Write(OmnidirectionalLights.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          bw.Write(Footprint.ToByteArray(encoding));
          bw.Write(Slots.ToByteArray(encoding));
          bw.Write(HorizontalExtents.ToByteArray(encoding));
        }

        var result = output.ToArray();
        if (result.Length != SerializedSize)
        {
          throw new InvalidOperationException($"BaseHeader must serialize to exactly 0x{SerializedSize:X} bytes; wrote 0x{result.Length:X} bytes.");
        }

        return result;
      }
    }

    private void ValidateState()
    {
      if (!Enum.IsDefined(typeof(MeshKind), MeshKind))
      {
        throw new InvalidOperationException($"BaseHeader MeshKind value {(int)MeshKind} is unsupported.");
      }

      Require(Frames, nameof(Frames));
      Require(HorizontalExtents, nameof(HorizontalExtents));
      RequireCount(MountPoints, 4, nameof(MountPoints));
      RequireCount(SpotLights, 4, nameof(SpotLights));
      RequireCount(OmnidirectionalLights, 4, nameof(OmnidirectionalLights));

      Require(Footprint, nameof(Footprint));
      RequireCount(Footprint.BoxHeights, MeshFootprint.BoxCount, "Footprint.BoxHeights");
      RequireCount(Footprint.BoxFlags, MeshFootprint.BoxCount, "Footprint.BoxFlags");
      RequireCount(Footprint.CoverageDescriptors, MeshFootprint.CoverageCount, "Footprint.CoverageDescriptors");
      RequireCount(Footprint.CoverageBitmaps, MeshFootprint.CoverageCount, "Footprint.CoverageBitmaps");

      Require(Slots, nameof(Slots));
      RequireCount(Slots.Turrets, 4, "Slots.Turrets");
      RequireCount(Slots.BarrelMuzzels, 4, "Slots.BarrelMuzzels");
      RequireCount(Slots.TurretMuzzels, 4, "Slots.TurretMuzzels");
      RequireCount(Slots.Headlights, 4, "Slots.Headlights");
      RequireCount(Slots.Omnilights, 4, "Slots.Omnilights");
      RequireCount(Slots.UnloadPoints, 4, "Slots.UnloadPoints");
      RequireCount(Slots.HitSpots, 4, "Slots.HitSpots");
      RequireCount(Slots.SmokeSpots, 4, "Slots.SmokeSpots");
      RequireCount(Slots.Unknown, 4, "Slots.Unknown");
      RequireCount(Slots.Chimneys, 2, "Slots.Chimneys");
      RequireCount(Slots.SmokeTraces, 2, "Slots.SmokeTraces");
      RequireCount(Slots.Exhausts, 2, "Slots.Exhausts");
      RequireCount(Slots.KeelTraces, 2, "Slots.KeelTraces");
      RequireCount(Slots.InterfacePivot, 1, "Slots.InterfacePivot");
      RequireCount(Slots.CenterPivot, 1, "Slots.CenterPivot");
      RequireCount(Slots.ProductionSpotStart, 1, "Slots.ProductionSpotStart");
      RequireCount(Slots.ProductionSpotEnd, 1, "Slots.ProductionSpotEnd");
      RequireCount(Slots.LandingSpot, 1, "Slots.LandingSpot");
    }

    private static void Require(object value, string name)
    {
      if (value == null)
      {
        throw new InvalidOperationException($"BaseHeader {name} is required.");
      }
    }

    private static void RequireCount<T>(IEnumerable<T> values, int expected, string name)
    {
      var items = values?.ToArray();
      if (items == null || items.Length != expected)
      {
        throw new InvalidOperationException($"BaseHeader {name} must contain exactly {expected} records.");
      }

      if (items.Any(item => ReferenceEquals(item, null)))
      {
        throw new InvalidOperationException($"BaseHeader {name} cannot contain null records.");
      }
    }

  }
}
