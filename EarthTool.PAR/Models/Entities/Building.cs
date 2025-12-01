using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
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
      BuildingType = (BuildingType)data.ReadInteger();
      BuildingTypeEx = (BuildingExType)data.ReadInteger();
      BuildingTabType = (BuildingTabType)data.ReadInteger();
      InitCannonId1 = data.ReadParameterStringRef();
      InitCannonId2 = data.ReadParameterStringRef();
      InitCannonId3 = data.ReadParameterStringRef();
      InitCannonId4 = data.ReadParameterStringRef();
      CopulaId = data.ReadParameterStringRef();
      BuildingTunnelNumber = data.ReadInteger();
      UpgradeCopulaSmallId = data.ReadParameterStringRef();
      UpgradeCopulaBigId = data.ReadParameterStringRef();
      BuildLCTransporterId = data.ReadParameterStringRef();
      ChimneySmokeId = data.ReadParameterStringRef();
      NeedPower = data.ReadInteger();
      SlaveBuildingId = data.ReadParameterStringRef();
      MaxSubBuildingCount = data.ReadInteger();
      PowerLevel = data.ReadInteger();
      PowerTransmitterRange = data.ReadInteger();
      ConnectTransmitterRange = data.ReadInteger();
      FullEnergyPowerInDay = data.ReadInteger();
      ResourceInputOutput = (ResourceInputOutputFlags)data.ReadInteger();
      TickPerContainer = data.ReadInteger();
      ContainerId = data.ReadParameterStringRef();
      ContainerSmeltingTicks = data.ReadInteger();
      ResourcesPerTransport = data.ReadInteger();
      TransporterId = data.ReadParameterStringRef();
      BuildingAmmoId = data.ReadParameterStringRef();
      RangeOfBuildingFire = data.ReadInteger();
      ShootExplosionId = data.ReadParameterStringRef();
      AmmoReloadingTime = data.ReadInteger();
      BuildExplosionId = data.ReadParameterStringRef();
      CopulaAnimationFlags = (CopulaAnimationFlags)data.ReadInteger();
      EndOfClosingCopulaAnimation = data.ReadInteger();
      LaserId = data.ReadParameterStringRef();
      SpaceStationType = (SpaceStationType)data.ReadInteger();
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
      bw.WriteParameterStringRef(InitCannonId1, encoding);
      bw.WriteParameterStringRef(InitCannonId2, encoding);
      bw.WriteParameterStringRef(InitCannonId3, encoding);
      bw.WriteParameterStringRef(InitCannonId4, encoding);
      bw.WriteParameterStringRef(CopulaId, encoding);
      bw.Write(BuildingTunnelNumber);
      bw.WriteParameterStringRef(UpgradeCopulaSmallId, encoding);
      bw.WriteParameterStringRef(UpgradeCopulaBigId, encoding);
      bw.WriteParameterStringRef(BuildLCTransporterId, encoding);
      bw.WriteParameterStringRef(ChimneySmokeId, encoding);
      bw.Write(NeedPower);
      bw.WriteParameterStringRef(SlaveBuildingId, encoding);
      bw.Write(MaxSubBuildingCount);
      bw.Write(PowerLevel);
      bw.Write(PowerTransmitterRange);
      bw.Write(ConnectTransmitterRange);
      bw.Write(FullEnergyPowerInDay);
      bw.Write((int)ResourceInputOutput);
      bw.Write(TickPerContainer);
      bw.WriteParameterStringRef(ContainerId, encoding);
      bw.Write(ContainerSmeltingTicks);
      bw.Write(ResourcesPerTransport);
      bw.WriteParameterStringRef(TransporterId, encoding);
      bw.WriteParameterStringRef(BuildingAmmoId, encoding);
      bw.Write(RangeOfBuildingFire);
      bw.WriteParameterStringRef(ShootExplosionId, encoding);
      bw.Write(AmmoReloadingTime);
      bw.WriteParameterStringRef(BuildExplosionId, encoding);
      bw.Write((int)CopulaAnimationFlags);
      bw.Write(EndOfClosingCopulaAnimation);
      bw.WriteParameterStringRef(LaserId, encoding);
      bw.Write((int)SpaceStationType);

      return output.ToArray();
    }
  }
}
