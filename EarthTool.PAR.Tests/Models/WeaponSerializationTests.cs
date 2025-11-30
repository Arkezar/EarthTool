using EarthTool.PAR.Enums;
using EarthTool.PAR.Factories;
using EarthTool.PAR.Models;
using System.Text;

namespace EarthTool.PAR.Tests.Models
{
  public class WeaponSerializationTests
  {
    [Fact]
    public void Weapon_Roundtrip_Success()
    {
      // Arrange
      var original = new Weapon
      {
        Name = "TEST_WEAPON",
        RequiredResearch = new[] { 10 },
        ClassId = EntityClassType.Cannon,
        Mesh = "weapon.msh",
        ShadowType = ShadowType.Tank,
        ViewParamsIndex = 5,
        Cost = 800,
        TimeOfBuild = 45,
        SoundPackId = "SOUND_WEAPON",
        SmokeId = "",
        KillExplosionId = "EXPLOSION_WEAPON",
        DestructedId = "",
        RangeOfSight = 600,
        PlugType = ConnectorType.Small,
        SlotType = ConnectorType.Small,
        MaxAlphaPerTick = 5,
        MaxBetaPerTick = 3,
        AlphaMargin = 10,
        BetaMargin = 8,
        BarrelBetaType = BarrelBetaType.Normal,
        BarrelBetaAngle = 45,
        BarrelCount = 1,
        AmmoId = "AMMO_01",
        AmmoType = 1,
        TargetType = TargetType.Ground,
        RangeOfFire = 500,
        PlusDamage = 50,
        FireType = WeaponFireType.Single,
        ShootDelay = 100,
        NeedExternal = 0,
        ReloadDelay = 50,
        MaxAmmo = 100,
        BarrelExplosionId = "BARREL_EXPLOSION"
      };

      // Act
      var bytes = original.ToByteArray(Encoding.UTF8);
      using var stream = new MemoryStream(bytes);
      using var reader = new BinaryReader(stream, Encoding.UTF8);
      var factory = new EntityFactory();
      var restored = (Weapon)factory.CreateEntity(reader, EntityGroupType.Cannon);

      // Assert
      restored.Name.Should().Be(original.Name);
      restored.RangeOfSight.Should().Be(original.RangeOfSight);
      restored.RangeOfFire.Should().Be(original.RangeOfFire);
      restored.PlusDamage.Should().Be(original.PlusDamage);
      restored.FireType.Should().Be(original.FireType);
      restored.AmmoId.Should().Be(original.AmmoId);
      restored.TargetType.Should().Be(original.TargetType);
    }
  }
}
