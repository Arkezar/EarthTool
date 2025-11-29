using EarthTool.PAR.Enums;
using EarthTool.PAR.Factories;
using EarthTool.PAR.Models;
using System.Text;

namespace EarthTool.PAR.Tests.Models
{
  public class MissileSerializationTests
  {
    [Fact]
    public void Missile_Roundtrip_Success()
    {
      // Arrange
      var original = new Missile
      {
        Name = "TEST_MISSILE",
        RequiredResearch = new[] { 15 },
        ClassId = EntityClassType.Missile,
        Mesh = "missile.msh",
        ShadowType = ShadowType.None,
        ViewParamsIndex = 0,
        Cost = 150,
        TimeOfBuild = 10,
        SoundPackId = "",
        SmokeId = "MISSILE_SMOKE",
        KillExplosionId = "MISSILE_EXPLOSION",
        DestructedId = "",
        HP = 50,
        HpRegeneration = 0,
        Armor = 5,
        CalorificCapacity = 10,
        DisableResist = 0,
        StoreableFlags = StoreableFlags.None,
        StandType = StandType.None,
        Type = MissileType.Rocket,
        RocketType = RocketType.Guided100,
        MissileSize = 1,
        RocketDummyId = "",
        IsAntiRocketTarget = 0,
        Speed = 300,
        TimeOfShoot = 1000,
        PlusRangeOfFire = 100,
        HitType = HitType.Direct,
        HitRange = 50,
        TypeOfDamage = DamageFlags.Normal,
        Damage = 200,
        ExplosionId = "MISSILE_HIT_EXPLOSION"
      };

      // Act
      var bytes = original.ToByteArray(Encoding.UTF8);
      using var stream = new MemoryStream(bytes);
      using var reader = new BinaryReader(stream, Encoding.UTF8);
      var factory = new EntityFactory();
      var restored = (Missile)factory.CreateEntity(reader, EntityGroupType.Missile);

      // Assert
      restored.Name.Should().Be(original.Name);
      restored.Type.Should().Be(original.Type);
      restored.Speed.Should().Be(original.Speed);
      restored.Damage.Should().Be(original.Damage);
      restored.HitType.Should().Be(original.HitType);
      restored.ExplosionId.Should().Be(original.ExplosionId);
    }
  }
}
