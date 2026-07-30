using EarthTool.Common.Factories;
using EarthTool.DAE.Elements;
using EarthTool.DAE.Services;
using EarthTool.MSH.Enums;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using EarthTool.MSH.Services;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace EarthTool.DAE.Tests;

public class StaticLightConversionTests
{
  [Fact]
  public void ExportUsesAttachmentActivityStableNumbersAndLosslessMetadata()
  {
    var spot = new SpotLight
    {
      Position = Vector3.Zero,
      LightParameters = new Vector3(-1.25f, 2.5f, 300.125f),
      HorizontalTargetDistance = 41.5f,
      TargetHeading = 0xC1,
      Reserved1 = 0xA2,
      Reserved2 = 0xB3,
      Reserved3 = 0xD4,
      ConeHalfAngleTangent = 0.75f,
      DistanceScaledCone = 31.125f,
      VerticalTargetSlope = -0.625f,
      FinalParameter = 17.875f
    };
    var spots = EmptySpots();
    spots[0] = new SpotLight { Position = new Vector3(10, 20, 30) };
    spots[2] = spot;
    var omnis = EmptyOmnis();
    omnis[3] = new OmniLight
    {
      Position = new Vector3(1.25f, -2.5f, 5),
      LightParameters = new Vector3(8.5f, -3.25f, 0.125f),
      FinalParameter = -9.75f
    };
    var mesh = new EarthMesh
    {
      BaseHeader = new MeshBaseHeader
      {
        SpotLights = spots,
        OmnidirectionalLights = omnis,
        Slots = new ModelSlots
        {
          Headlights = CreateLightSlots(13, 3),
          Omnilights = CreateLightSlots(17, 4)
        }
      }
    };

    var exported = new LightingFactory().GetLights(mesh).ToArray();

    Assert.Equal(new[] { "SpotLight-3", "OmniLight-4" }, exported.Select(item => item.Light.Name));
    var exportedSpot = exported[0].Light;
    Assert.Equal("1 1 1", exportedSpot.Technique_Common.Spot.Color.Value);
    Assert.Equal(2 * Math.Atan(0.75) * 180 / Math.PI,
      exportedSpot.Technique_Common.Spot.Falloff_Angle.Value, 10);
    var metadata = GetMetadata(exportedSpot);
    Assert.Equal("1", metadata.GetAttribute("version"));
    Assert.Equal("3", GetChildText(metadata, "source_number"));
    Assert.Equal("0 0 0", GetChildText(metadata, "position"));
    Assert.Equal("-1.25 2.5 300.125", GetChildText(metadata, "light_parameters"));
    Assert.Equal("193", GetChildText(metadata, "target_heading"));
    Assert.Equal("162 179 212", GetChildText(metadata, "reserved"));
    Assert.Equal("17.875", GetChildText(metadata, "final_parameter"));
  }

  [Fact]
  public void PublicDaeRoundTripRestoresCompleteSparseStaticLightRecords()
  {
    var mesh = CreateMesh();
    var spot = new SpotLight
    {
      Position = Vector3.Zero,
      LightParameters = new Vector3(-1.25f, 2.5f, 300.125f),
      HorizontalTargetDistance = 41.5f,
      TargetHeading = 0xC1,
      Reserved1 = 0xA2,
      Reserved2 = 0xB3,
      Reserved3 = 0xD4,
      ConeHalfAngleTangent = 0.75f,
      DistanceScaledCone = 31.125f,
      VerticalTargetSlope = -0.625f,
      FinalParameter = 17.875f
    };
    var omni = new OmniLight
    {
      Position = new Vector3(1.25f, -2.5f, 5),
      LightParameters = new Vector3(8.5f, -3.25f, 0.125f),
      FinalParameter = -9.75f
    };
    var header = Assert.IsType<MeshBaseHeader>(mesh.BaseHeader);
    var spots = header.SpotLights.ToArray();
    spots[2] = spot;
    header.SpotLights = spots;
    var omnis = header.OmnidirectionalLights.ToArray();
    omnis[3] = omni;
    header.OmnidirectionalLights = omnis;
    var slots = Assert.IsType<ModelSlots>(header.Slots);
    slots.Headlights = CreateLightSlots(13, 3);
    slots.Omnilights = CreateLightSlots(17, 4);
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dae");

    try
    {
      new ColladaMeshWriter(CreateColladaModelFactory()).Write(mesh, path);

      var converted = new ColladaMeshReader(new EarthInfoFactory(Encoding.UTF8), new HierarchyBuilder()).Read(path);

      Assert.Equal(4, converted.BaseHeader.SpotLights.Count);
      Assert.Equal(4, converted.BaseHeader.OmnidirectionalLights.Count);
      var convertedSpot = converted.BaseHeader.SpotLights[2];
      Assert.Equal(spot.Position, convertedSpot.Position);
      Assert.Equal(spot.LightParameters, convertedSpot.LightParameters);
      Assert.Equal(spot.HorizontalTargetDistance, convertedSpot.HorizontalTargetDistance);
      Assert.Equal(spot.TargetHeading, convertedSpot.TargetHeading);
      Assert.Equal(spot.Reserved1, convertedSpot.Reserved1);
      Assert.Equal(spot.Reserved2, convertedSpot.Reserved2);
      Assert.Equal(spot.Reserved3, convertedSpot.Reserved3);
      Assert.Equal(spot.ConeHalfAngleTangent, convertedSpot.ConeHalfAngleTangent);
      Assert.Equal(spot.DistanceScaledCone, convertedSpot.DistanceScaledCone);
      Assert.Equal(spot.VerticalTargetSlope, convertedSpot.VerticalTargetSlope);
      Assert.Equal(spot.FinalParameter, convertedSpot.FinalParameter);
      var convertedOmni = converted.BaseHeader.OmnidirectionalLights[3];
      Assert.Equal(omni.Position, convertedOmni.Position);
      Assert.Equal(omni.LightParameters, convertedOmni.LightParameters);
      Assert.Equal(omni.FinalParameter, convertedOmni.FinalParameter);
      Assert.False(converted.BaseHeader.Slots.Headlights.ElementAt(0).IsValid);
      Assert.True(converted.BaseHeader.Slots.Headlights.ElementAt(2).IsValid);
      Assert.Equal(Vector3.Zero, converted.BaseHeader.Slots.Headlights.ElementAt(2).Position.Value);
      Assert.False(converted.BaseHeader.Slots.Omnilights.ElementAt(0).IsValid);
      Assert.True(converted.BaseHeader.Slots.Omnilights.ElementAt(3).IsValid);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Theory]
  [InlineData("version", "version")]
  [InlineData("missing", "light_parameters")]
  [InlineData("malformed", "light_parameters")]
  [InlineData("conflict", "conflicts")]
  public void PublicDaeImportRejectsInvalidStaticLightMetadata(string mutation, string expectedMessage)
  {
    var path = WriteSpotDae();
    try
    {
      var document = XDocument.Load(path);
      var metadata = document.Descendants().Single(element => element.Name.LocalName == "msh_static_light");
      switch (mutation)
      {
        case "version":
          metadata.SetAttributeValue("version", "2");
          break;
        case "missing":
          metadata.Elements().Single(element => element.Name.LocalName == "light_parameters").Remove();
          break;
        case "malformed":
          metadata.Elements().Single(element => element.Name.LocalName == "light_parameters").Value = "1 invalid 3";
          break;
        case "conflict":
          metadata.Elements().Single(element => element.Name.LocalName == "source_number").Value = "2";
          break;
      }
      document.Save(path);

      var exception = Assert.Throws<InvalidDataException>(() => CreateReader().Read(path));

      Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Theory]
  [InlineData("duplicate", "duplicate")]
  [InlineData("unnumbered", "numbered name")]
  [InlineData("out-of-range", "outside 1 through 4")]
  public void PublicDaeImportRejectsInvalidStaticLightNumbering(string mutation, string expectedMessage)
  {
    var path = WriteSpotDae();
    try
    {
      var document = XDocument.Load(path);
      var light = document.Descendants()
        .Single(element => element.Name.LocalName == "light" && (string)element.Attribute("name")! == "SpotLight-3");
      switch (mutation)
      {
        case "duplicate":
          light.AddAfterSelf(new XElement(light));
          break;
        case "unnumbered":
          light.SetAttributeValue("name", "Lamp");
          break;
        case "out-of-range":
          light.SetAttributeValue("name", "SpotLight-5");
          break;
      }
      document.Save(path);

      var exception = Assert.Throws<InvalidDataException>(() => CreateReader().Read(path));

      Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public void PublicDaeImportUsesStrictStandardFallbackWithoutMetadata()
  {
    var path = WriteSpotDae();
    try
    {
      var document = XDocument.Load(path);
      var light = document.Descendants()
        .Single(element => element.Name.LocalName == "light" && (string)element.Attribute("name")! == "SpotLight-3");
      light.Elements().Where(element => element.Name.LocalName == "extra").Remove();
      light.Descendants().Single(element => element.Name.LocalName == "color").Value = "-1.25 2.5 3.75";
      light.Descendants().Single(element => element.Name.LocalName == "falloff_angle").Value = "60";
      document.Save(path);

      var converted = CreateReader().Read(path);

      var spot = converted.BaseHeader.SpotLights[2];
      Assert.Equal(new Vector3(-1.25f, 2.5f, 3.75f), spot.LightParameters);
      Assert.Equal((float)Math.Tan(Math.PI / 6), spot.ConeHalfAngleTangent, 6);
      Assert.Equal(0, spot.HorizontalTargetDistance);
      Assert.Equal((byte)0, spot.TargetHeading);
      Assert.Equal((byte)0, spot.Reserved1);
      Assert.Equal((byte)0, spot.Reserved2);
      Assert.Equal((byte)0, spot.Reserved3);
      Assert.Equal(0, spot.DistanceScaledCone);
      Assert.Equal(0, spot.VerticalTargetSlope);
      Assert.Equal(0, spot.FinalParameter);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public void PublicDaeImportKeepsSentinelPositionLightActive()
  {
    var mesh = CreateMesh();
    var header = Assert.IsType<MeshBaseHeader>(mesh.BaseHeader);
    var spots = header.SpotLights.ToArray();
    spots[0] = new SpotLight { Position = new Vector3(-128, 128, -128) };
    header.SpotLights = spots;
    Assert.IsType<ModelSlots>(header.Slots).Headlights = CreateLightSlots(13, 1);
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dae");

    try
    {
      new ColladaMeshWriter(CreateColladaModelFactory()).Write(mesh, path);

      var converted = CreateReader().Read(path);

      Assert.Equal(new Vector3(-128, 128, -128), converted.BaseHeader.SpotLights[0].Position);
      Assert.True(converted.BaseHeader.Slots.Headlights.First().IsValid);
      Assert.Contains(new LightingFactory().GetLights(converted), item => item.Light.Name == "SpotLight-1");
    }
    finally
    {
      File.Delete(path);
    }
  }

  private static ISpotLight[] EmptySpots()
    => Enumerable.Range(0, 4).Select(_ => (ISpotLight)new SpotLight()).ToArray();

  private static IOmniLight[] EmptyOmnis()
    => Enumerable.Range(0, 4).Select(_ => (IOmniLight)new OmniLight()).ToArray();

  private static EarthMesh CreateMesh()
  {
    var part = new ModelPart
    {
      PartType = PartType.Base,
      Vertices = new IVertex[]
      {
        new Vertex(new EarthTool.MSH.Models.Elements.Vector(0, 0, 0),
          new EarthTool.MSH.Models.Elements.Vector(0, 0, 1), new TextureCoordinate(0, 0), 0, 0),
        new Vertex(new EarthTool.MSH.Models.Elements.Vector(1, 0, 0),
          new EarthTool.MSH.Models.Elements.Vector(0, 0, 1), new TextureCoordinate(1, 0), 0, 1),
        new Vertex(new EarthTool.MSH.Models.Elements.Vector(0, 1, 0),
          new EarthTool.MSH.Models.Elements.Vector(0, 0, 1), new TextureCoordinate(0, 1), 0, 2)
      },
      Faces = new IFace[] { new Face { V1 = 0, V2 = 1, V3 = 2, Flags = 1 } },
      Texture = new TextureInfo { FileName = "Textures\\fixture.tex" },
      Animations = new Animations(),
      Offset = new EarthTool.MSH.Models.Elements.Vector()
    };
    return new EarthMesh
    {
      BaseHeader = new MeshBaseHeader
      {
        MeshKind = MeshKind.Static,
        Frames = new MeshFrames(),
        MountPoints = Enumerable.Range(0, 4).Select(_ => (IVector)new EarthTool.MSH.Models.Elements.Vector()).ToArray(),
        SpotLights = EmptySpots(),
        OmnidirectionalLights = EmptyOmnis(),
        Footprint = new MeshFootprint(),
        Slots = CreateEmptySlots(),
        HorizontalExtents = new MeshHorizontalExtents()
      },
      Geometries = new[] { part },
      PartsTree = new HierarchyBuilder().GetPartsTree(new[] { part })
    };
  }

  private static ModelSlots CreateEmptySlots()
    => new()
    {
      Turrets = InactiveSlots(4, 1),
      BarrelMuzzels = InactiveSlots(4, 5),
      TurretMuzzels = InactiveSlots(4, 9),
      Headlights = InactiveSlots(4, 13),
      Omnilights = InactiveSlots(4, 17),
      UnloadPoints = InactiveSlots(4, 21),
      HitSpots = InactiveSlots(4, 25),
      SmokeSpots = InactiveSlots(4, 29),
      Unknown = InactiveSlots(4, 33),
      Chimneys = InactiveSlots(2, 37),
      SmokeTraces = InactiveSlots(2, 39),
      Exhausts = InactiveSlots(2, 41),
      KeelTraces = InactiveSlots(2, 43),
      InterfacePivot = InactiveSlots(1, 45),
      CenterPivot = InactiveSlots(1, 46),
      ProductionSpotStart = InactiveSlots(1, 47),
      ProductionSpotEnd = InactiveSlots(1, 48),
      LandingSpot = InactiveSlots(1, 49)
    };

  private static ISlot[] InactiveSlots(int count, int firstId)
    => Enumerable.Range(0, count).Select(index => (ISlot)new Slot { Id = firstId + index }).ToArray();

  private static string WriteSpotDae()
  {
    var mesh = CreateMesh();
    var header = Assert.IsType<MeshBaseHeader>(mesh.BaseHeader);
    var spots = header.SpotLights.ToArray();
    spots[2] = new SpotLight
    {
      Position = new Vector3(1.25f, -2.5f, 5),
      LightParameters = new Vector3(-1.25f, 2.5f, 300.125f),
      ConeHalfAngleTangent = 0.75f
    };
    header.SpotLights = spots;
    Assert.IsType<ModelSlots>(header.Slots).Headlights = CreateLightSlots(13, 3);
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dae");
    new ColladaMeshWriter(CreateColladaModelFactory()).Write(mesh, path);
    return path;
  }

  private static ISlot[] CreateLightSlots(int firstId, int activeNumber)
    => Enumerable.Range(0, 4).Select(index => (ISlot)new Slot
    {
      Id = firstId + index,
      Position = index + 1 == activeNumber
        ? new EarthTool.MSH.Models.Elements.Vector(0, 0, 0)
        : new EarthTool.MSH.Models.Elements.Vector(-128, 128, -128)
    }).ToArray();

  private static XmlElement GetMetadata(Collada141.Light light)
    => light.Extra.SelectMany(extra => extra.Technique)
      .Single(technique => technique.Profile == "EARTHTOOL")
      .Any.Single(element => element.LocalName == "msh_static_light");

  private static string GetChildText(XmlElement metadata, string name)
    => metadata.ChildNodes.OfType<XmlElement>().Single(element => element.LocalName == name).InnerText;

  private static ColladaModelFactory CreateColladaModelFactory()
    => new(
      new AnimationsFactory(),
      new GeometriesFactory(),
      new MaterialFactory(),
      new LightingFactory(),
      new SlotFactory());

  private static ColladaMeshReader CreateReader()
    => new(new EarthInfoFactory(Encoding.UTF8), new HierarchyBuilder());
}
