using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Building : EquipableEntity
  {
    public Building()
    {
    }

    public Building(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      BuildingType = GetInteger(data);
      BuildingTypeEx = GetInteger(data);
      BuildingTabType = GetInteger(data);
      InitCannonId1 = GetString(data);
      data.ReadBytes(4);
      InitCannonId2 = GetString(data);
      data.ReadBytes(4);
      InitCannonId3 = GetString(data);
      data.ReadBytes(4);
      InitCannonId4 = GetString(data);
      data.ReadBytes(4);
      CopulaId = GetString(data);
      data.ReadBytes(4);
      BuildingTunnelNumber = GetInteger(data);
      UpgradeCopulaSmallId = GetString(data);
      data.ReadBytes(4);
      UpgradeCopulaBigId = GetString(data);
      data.ReadBytes(4);
      BuildLCTransporterId = GetString(data);
      data.ReadBytes(4);
      ChimneySmokeId = GetString(data);
      data.ReadBytes(4);
      NeedPower = GetInteger(data);
      SlaveBuildingId = GetString(data);
      data.ReadBytes(4);
      MaxSubBuildingCount = GetInteger(data);
      PowerLevel = GetInteger(data);
      PowerTransmitterRange = GetInteger(data);
      ConnectTransmitterRange = GetInteger(data);
      FullEnergyPowerInDay = GetInteger(data);
      ResourceInputOutput = GetInteger(data);
      TickPerContainer = GetInteger(data);
      ContainerId = GetString(data);
      data.ReadBytes(4);
      ContainerSmeltingTicks = GetInteger(data);
      ResourcesPerTransport = GetInteger(data);
      TransporterId = GetString(data);
      data.ReadBytes(4);
      BuildingAmmoId = GetString(data);
      data.ReadBytes(4);
      RangeOfBuildingFire = GetInteger(data);
      ShootExplosionId = GetString(data);
      data.ReadBytes(4);
      AmmoReloadingTime = GetInteger(data);
      BuildExplosionId = GetString(data);
      data.ReadBytes(4);
      CopulaAnimationFlags = (CopulaAnimationFlags)GetInteger(data);
      EndOfClosingCopulaAnimation = GetInteger(data);
      LaserId = GetString(data);
      data.ReadBytes(4);
      SpaceStationType = GetInteger(data);
    }

    public int BuildingType { get; set; }

    public int BuildingTypeEx { get; set; }

    public int BuildingTabType { get; set; }

    public string InitCannonId1 { get; set; }

    public string InitCannonId2 { get; set; }

    public string InitCannonId3 { get; set; }

    public string InitCannonId4 { get; set; }

    public string CopulaId { get; set; }

    public int BuildingTunnelNumber { get; set; }

    public string UpgradeCopulaSmallId { get; set; }

    public string UpgradeCopulaBigId { get; set; }

    public string BuildLCTransporterId { get; set; }

    public string ChimneySmokeId { get; set; }

    public int NeedPower { get; set; }

    public string SlaveBuildingId { get; set; }

    public int MaxSubBuildingCount { get; set; }

    public int PowerLevel { get; set; }

    public int PowerTransmitterRange { get; set; }

    public int ConnectTransmitterRange { get; set; }

    public int FullEnergyPowerInDay { get; set; }

    public int ResourceInputOutput { get; set; }

    public int TickPerContainer { get; set; }

    public string ContainerId { get; set; }

    public int ContainerSmeltingTicks { get; set; }

    public int ResourcesPerTransport { get; set; }

    public string TransporterId { get; set; }

    public string BuildingAmmoId { get; set; }

    public int RangeOfBuildingFire { get; set; }

    public string ShootExplosionId { get; set; }

    public int AmmoReloadingTime { get; set; }

    public string BuildExplosionId { get; set; }

    public CopulaAnimationFlags CopulaAnimationFlags { get; set; }

    public int EndOfClosingCopulaAnimation { get; set; }

    public string LaserId { get; set; }

    public int SpaceStationType { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => BuildingType,
        () => BuildingTypeEx,
        () => BuildingTabType,
        () => InitCannonId1,
        () => 1,
        () => InitCannonId2,
        () => 1,
        () => InitCannonId3,
        () => 1,
        () => InitCannonId4,
        () => 1,
        () => CopulaId,
        () => 1,
        () => BuildingTunnelNumber,
        () => UpgradeCopulaSmallId,
        () => 1,
        () => UpgradeCopulaBigId,
        () => 1,
        () => BuildLCTransporterId,
        () => 1,
        () => ChimneySmokeId,
        () => 1,
        () => NeedPower,
        () => SlaveBuildingId,
        () => 1,
        () => MaxSubBuildingCount,
        () => PowerLevel,
        () => PowerTransmitterRange,
        () => ConnectTransmitterRange,
        () => FullEnergyPowerInDay,
        () => ResourceInputOutput,
        () => TickPerContainer,
        () => ContainerId,
        () => 1,
        () => ContainerSmeltingTicks,
        () => ResourcesPerTransport,
        () => TransporterId,
        () => 1,
        () => BuildingAmmoId,
        () => 1,
        () => RangeOfBuildingFire,
        () => ShootExplosionId,
        () => 1,
        () => AmmoReloadingTime,
        () => BuildExplosionId,
        () => 1,
        () => (int)CopulaAnimationFlags,
        () => EndOfClosingCopulaAnimation,
        () => LaserId,
        () => 1,
        () => SpaceStationType
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(BuildingType);
          bw.Write(BuildingTypeEx);
          bw.Write(BuildingTabType);
          bw.Write(InitCannonId1.Length);
          bw.Write(encoding.GetBytes(InitCannonId1));
          bw.Write(-1);
          bw.Write(InitCannonId2.Length);
          bw.Write(encoding.GetBytes(InitCannonId2));
          bw.Write(-1);
          bw.Write(InitCannonId3.Length);
          bw.Write(encoding.GetBytes(InitCannonId3));
          bw.Write(-1);
          bw.Write(InitCannonId4.Length);
          bw.Write(encoding.GetBytes(InitCannonId4));
          bw.Write(-1);
          bw.Write(CopulaId.Length);
          bw.Write(encoding.GetBytes(CopulaId));
          bw.Write(-1);
          bw.Write(BuildingTunnelNumber);
          bw.Write(UpgradeCopulaSmallId.Length);
          bw.Write(encoding.GetBytes(UpgradeCopulaSmallId));
          bw.Write(-1);
          bw.Write(UpgradeCopulaBigId.Length);
          bw.Write(encoding.GetBytes(UpgradeCopulaBigId));
          bw.Write(-1);
          bw.Write(BuildLCTransporterId.Length);
          bw.Write(encoding.GetBytes(BuildLCTransporterId));
          bw.Write(-1);
          bw.Write(ChimneySmokeId.Length);
          bw.Write(encoding.GetBytes(ChimneySmokeId));
          bw.Write(-1);
          bw.Write(NeedPower);
          bw.Write(SlaveBuildingId.Length);
          bw.Write(encoding.GetBytes(SlaveBuildingId));
          bw.Write(-1);
          bw.Write(MaxSubBuildingCount);
          bw.Write(PowerLevel);
          bw.Write(PowerTransmitterRange);
          bw.Write(ConnectTransmitterRange);
          bw.Write(FullEnergyPowerInDay);
          bw.Write(ResourceInputOutput);
          bw.Write(TickPerContainer);
          bw.Write(ContainerId.Length);
          bw.Write(encoding.GetBytes(ContainerId));
          bw.Write(-1);
          bw.Write(ContainerSmeltingTicks);
          bw.Write(ResourcesPerTransport);
          bw.Write(TransporterId.Length);
          bw.Write(encoding.GetBytes(TransporterId));
          bw.Write(-1);
          bw.Write(BuildingAmmoId.Length);
          bw.Write(encoding.GetBytes(BuildingAmmoId));
          bw.Write(-1);
          bw.Write(RangeOfBuildingFire);
          bw.Write(ShootExplosionId.Length);
          bw.Write(encoding.GetBytes(ShootExplosionId));
          bw.Write(-1);
          bw.Write(AmmoReloadingTime);
          bw.Write(BuildExplosionId.Length);
          bw.Write(encoding.GetBytes(BuildExplosionId));
          bw.Write(-1);
          bw.Write((int)CopulaAnimationFlags);
          bw.Write(EndOfClosingCopulaAnimation);
          bw.Write(LaserId.Length);
          bw.Write(encoding.GetBytes(LaserId));
          bw.Write(-1);
          bw.Write(SpaceStationType);
        }

        return output.ToArray();
      }
    }
  }
}
