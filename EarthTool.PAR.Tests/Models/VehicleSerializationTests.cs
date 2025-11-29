using EarthTool.PAR.Enums;
using EarthTool.PAR.Factories;
using EarthTool.PAR.Models;
using System.Text;

namespace EarthTool.PAR.Tests.Models
{
  public class VehicleSerializationTests
  {
    [Fact]
    public void Vehicle_ToByteArray_And_Constructor_Roundtrip_Success()
    {
      // Arrange
      var original = new Vehicle
      {
        Name = "TEST_VEHICLE",
        RequiredResearch = new[] { 1, 2 },
        ClassId = EntityClassType.Vehicle,
        Mesh = "vehicle.msh",
        ShadowType = ShadowType.Tank,
        ViewParamsIndex = 1,
        Cost = 500,
        TimeOfBuild = 30,
        SoundPackId = "SOUND_VEHICLE",
        SmokeId = "SMOKE_VEHICLE",
        KillExplosionId = "EXPLOSION_VEHICLE",
        DestructedId = "DESTROYED_VEHICLE",
        HP = 1000,
        HpRegeneration = 10,
        Armor = 100,
        CalorificCapacity = 200,
        DisableResist = 20,
        StoreableFlags = StoreableFlags.None,
        StandType = StandType.None,
        SightRange = 300,
        TalkPackId = "TALK_VEHICLE",
        ShieldGeneratorId = "SHIELD_VEHICLE",
        MaxShieldUpgrade = MaxShieldUpgradeType.MediumGenerator,
        Slot1Type = ConnectorType.Slot,
        Slot2Type = ConnectorType.Slot,
        Slot3Type = ConnectorType.BigSlot,
        Slot4Type = ConnectorType.BigSlot,
        SoilSpeed = 120,
        RoadSpeed = 180,
        SandSpeed = 90,
        BankSpeed = 70,
        WaterSpeed = 50,
        DeepWaterSpeed = 30,
        AirSpeed = 0,
        ObjectType = VehicleObjectType.Mech,
        EngineSmokeId = "ENGINE_SMOKE_01",
        DustId = "DUST_01",
        BillowId = "BILLOW_01",
        StandBillowId = "STAND_BILLOW_01",
        TrackId = "TRACK_01"
      };

      // Act
      var bytes = original.ToByteArray(Encoding.UTF8);
      using var stream = new MemoryStream(bytes);
      using var reader = new BinaryReader(stream, Encoding.UTF8);
      
      // Use EntityFactory to deserialize
      var factory = new EntityFactory();
      var restored = (Vehicle)factory.CreateEntity(reader, EntityGroupType.Vehicle);

      // Assert
      restored.Name.Should().Be(original.Name);
      restored.RequiredResearch.Should().Equal(original.RequiredResearch);
      restored.Mesh.Should().Be(original.Mesh);
      restored.ShadowType.Should().Be(original.ShadowType);
      restored.ViewParamsIndex.Should().Be(original.ViewParamsIndex);
      restored.Cost.Should().Be(original.Cost);
      restored.TimeOfBuild.Should().Be(original.TimeOfBuild);
      restored.HP.Should().Be(original.HP);
      restored.HpRegeneration.Should().Be(original.HpRegeneration);
      restored.Armor.Should().Be(original.Armor);
      restored.SightRange.Should().Be(original.SightRange);
      restored.SoilSpeed.Should().Be(original.SoilSpeed);
      restored.RoadSpeed.Should().Be(original.RoadSpeed);
      restored.SandSpeed.Should().Be(original.SandSpeed);
      restored.BankSpeed.Should().Be(original.BankSpeed);
      restored.WaterSpeed.Should().Be(original.WaterSpeed);
      restored.DeepWaterSpeed.Should().Be(original.DeepWaterSpeed);
      restored.AirSpeed.Should().Be(original.AirSpeed);
      restored.ObjectType.Should().Be(original.ObjectType);
      restored.EngineSmokeId.Should().Be(original.EngineSmokeId);
      restored.DustId.Should().Be(original.DustId);
      restored.BillowId.Should().Be(original.BillowId);
      restored.StandBillowId.Should().Be(original.StandBillowId);
      restored.TrackId.Should().Be(original.TrackId);
    }

    [Fact]
    public void Vehicle_WithMinimalData_Serializes()
    {
      // Arrange
      var vehicle = new Vehicle
      {
        Name = "MINIMAL",
        RequiredResearch = new int[0],
        ClassId = EntityClassType.Vehicle,
        Mesh = "mesh.msh",
        ShadowType = ShadowType.None,
        ViewParamsIndex = 0,
        Cost = 0,
        TimeOfBuild = 0,
        SoundPackId = "",
        SmokeId = "",
        KillExplosionId = "",
        DestructedId = "",
        HP = 0,
        HpRegeneration = 0,
        Armor = 0,
        CalorificCapacity = 0,
        DisableResist = 0,
        StoreableFlags = StoreableFlags.None,
        StandType = StandType.None,
        SightRange = 0,
        TalkPackId = "",
        ShieldGeneratorId = "",
        MaxShieldUpgrade = MaxShieldUpgradeType.NoGenerator,
        Slot1Type = ConnectorType.Disabled,
        Slot2Type = ConnectorType.Disabled,
        Slot3Type = ConnectorType.Disabled,
        Slot4Type = ConnectorType.Disabled,
        SoilSpeed = 0,
        RoadSpeed = 0,
        SandSpeed = 0,
        BankSpeed = 0,
        WaterSpeed = 0,
        DeepWaterSpeed = 0,
        AirSpeed = 0,
        ObjectType = VehicleObjectType.None,
        EngineSmokeId = "",
        DustId = "",
        BillowId = "",
        StandBillowId = "",
        TrackId = ""
      };

      // Act
      var bytes = vehicle.ToByteArray(Encoding.UTF8);

      // Assert
      bytes.Should().NotBeEmpty();
    }

    [Fact]
    public void Vehicle_FieldTypes_ContainsBothStringsAndIntegers()
    {
      // Arrange
      var vehicle = new Vehicle
      {
        Name = "TEST",
        RequiredResearch = new[] { 1 },
        ClassId = EntityClassType.Vehicle,
        Mesh = "test.msh",
        ShadowType = ShadowType.Tank,
        ViewParamsIndex = 1,
        Cost = 100,
        TimeOfBuild = 10,
        SoundPackId = "SOUND",
        SmokeId = "",
        KillExplosionId = "",
        DestructedId = "",
        HP = 100,
        HpRegeneration = 1,
        Armor = 10,
        CalorificCapacity = 50,
        DisableResist = 5,
        StoreableFlags = StoreableFlags.None,
        StandType = StandType.None,
        SightRange = 100,
        TalkPackId = "",
        ShieldGeneratorId = "",
        MaxShieldUpgrade = MaxShieldUpgradeType.SmallGenerator,
        Slot1Type = ConnectorType.Slot,
        Slot2Type = ConnectorType.Disabled,
        Slot3Type = ConnectorType.Disabled,
        Slot4Type = ConnectorType.Disabled,
        SoilSpeed = 100,
        RoadSpeed = 120,
        SandSpeed = 80,
        BankSpeed = 60,
        WaterSpeed = 0,
        DeepWaterSpeed = 0,
        AirSpeed = 0,
        ObjectType = VehicleObjectType.Engine,
        EngineSmokeId = "SMOKE",
        DustId = "",
        BillowId = "",
        StandBillowId = "",
        TrackId = ""
      };

      // Act
      var fieldTypes = vehicle.FieldTypes.ToList();

      // Assert
      fieldTypes.Should().NotBeEmpty();
      fieldTypes.Should().Contain(false); // integers
      fieldTypes.Should().Contain(true);  // strings
    }
  }
}
