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
      BuildingType = (BuildingType)ReadInteger(data);
      BuildingTypeEx = (BuildingExType)ReadInteger(data);
      BuildingTabType = (BuildingTabType)ReadInteger(data);
      InitCannonId1 = ReadStringRef(data);
      InitCannonId2 = ReadStringRef(data);
      InitCannonId3 = ReadStringRef(data);
      InitCannonId4 = ReadStringRef(data);
      CopulaId = ReadStringRef(data);
      BuildingTunnelNumber = ReadInteger(data);
      UpgradeCopulaSmallId = ReadStringRef(data);
      UpgradeCopulaBigId = ReadStringRef(data);
      BuildLCTransporterId = ReadStringRef(data);
      ChimneySmokeId = ReadStringRef(data);
      NeedPower = ReadInteger(data);
      SlaveBuildingId = ReadStringRef(data);
      MaxSubBuildingCount = ReadInteger(data);
      PowerLevel = ReadInteger(data);
      PowerTransmitterRange = ReadInteger(data);
      ConnectTransmitterRange = ReadInteger(data);
      FullEnergyPowerInDay = ReadInteger(data);
      ResourceInputOutput = (ResourceInputOutputFlags)ReadInteger(data);
      TickPerContainer = ReadInteger(data);
      ContainerId = ReadStringRef(data);
      ContainerSmeltingTicks = ReadInteger(data);
      ResourcesPerTransport = ReadInteger(data);
      TransporterId = ReadStringRef(data);
      BuildingAmmoId = ReadStringRef(data);
      RangeOfBuildingFire = ReadInteger(data);
      ShootExplosionId = ReadStringRef(data);
      AmmoReloadingTime = ReadInteger(data);
      BuildExplosionId = ReadStringRef(data);
      CopulaAnimationFlags = (CopulaAnimationFlags)ReadInteger(data);
      EndOfClosingCopulaAnimation = ReadInteger(data);
      LaserId = ReadStringRef(data);
      SpaceStationType = (SpaceStationType)ReadInteger(data);
    }

    public BuildingType BuildingType { get; set; }

    public BuildingExType BuildingTypeEx { get; set; }

    public BuildingTabType BuildingTabType { get; set; }

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

    public ResourceInputOutputFlags ResourceInputOutput { get; set; }

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

    public SpaceStationType SpaceStationType { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => BuildingType,
        () => BuildingTypeEx,
        () => BuildingTabType,
        () => InitCannonId1,
        () => ReferenceMarker,
        () => InitCannonId2,
        () => ReferenceMarker,
        () => InitCannonId3,
        () => ReferenceMarker,
        () => InitCannonId4,
        () => ReferenceMarker,
        () => CopulaId,
        () => ReferenceMarker,
        () => BuildingTunnelNumber,
        () => UpgradeCopulaSmallId,
        () => ReferenceMarker,
        () => UpgradeCopulaBigId,
        () => ReferenceMarker,
        () => BuildLCTransporterId,
        () => ReferenceMarker,
        () => ChimneySmokeId,
        () => ReferenceMarker,
        () => NeedPower,
        () => SlaveBuildingId,
        () => ReferenceMarker,
        () => MaxSubBuildingCount,
        () => PowerLevel,
        () => PowerTransmitterRange,
        () => ConnectTransmitterRange,
        () => FullEnergyPowerInDay,
        () => ResourceInputOutput,
        () => TickPerContainer,
        () => ContainerId,
        () => ReferenceMarker,
        () => ContainerSmeltingTicks,
        () => ResourcesPerTransport,
        () => TransporterId,
        () => ReferenceMarker,
        () => BuildingAmmoId,
        () => ReferenceMarker,
        () => RangeOfBuildingFire,
        () => ShootExplosionId,
        () => ReferenceMarker,
        () => AmmoReloadingTime,
        () => BuildExplosionId,
        () => ReferenceMarker,
        () => (int)CopulaAnimationFlags,
        () => EndOfClosingCopulaAnimation,
        () => LaserId,
        () => ReferenceMarker,
        () => SpaceStationType
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)BuildingType);
      bw.Write((int)BuildingTypeEx);
      bw.Write((int)BuildingTabType);
      WriteStringRef(bw, InitCannonId1, encoding);
      WriteStringRef(bw, InitCannonId2, encoding);
      WriteStringRef(bw, InitCannonId3, encoding);
      WriteStringRef(bw, InitCannonId4, encoding);
      WriteStringRef(bw, CopulaId, encoding);
      bw.Write(BuildingTunnelNumber);
      WriteStringRef(bw, UpgradeCopulaSmallId, encoding);
      WriteStringRef(bw, UpgradeCopulaBigId, encoding);
      WriteStringRef(bw, BuildLCTransporterId, encoding);
      WriteStringRef(bw, ChimneySmokeId, encoding);
      bw.Write(NeedPower);
      WriteStringRef(bw, SlaveBuildingId, encoding);
      bw.Write(MaxSubBuildingCount);
      bw.Write(PowerLevel);
      bw.Write(PowerTransmitterRange);
      bw.Write(ConnectTransmitterRange);
      bw.Write(FullEnergyPowerInDay);
      bw.Write((int)ResourceInputOutput);
      bw.Write(TickPerContainer);
      WriteStringRef(bw, ContainerId, encoding);
      bw.Write(ContainerSmeltingTicks);
      bw.Write(ResourcesPerTransport);
      WriteStringRef(bw, TransporterId, encoding);
      WriteStringRef(bw, BuildingAmmoId, encoding);
      bw.Write(RangeOfBuildingFire);
      WriteStringRef(bw, ShootExplosionId, encoding);
      bw.Write(AmmoReloadingTime);
      WriteStringRef(bw, BuildExplosionId, encoding);
      bw.Write((int)CopulaAnimationFlags);
      bw.Write(EndOfClosingCopulaAnimation);
      WriteStringRef(bw, LaserId, encoding);
      bw.Write((int)SpaceStationType);

      return output.ToArray();
    }
  }
}
