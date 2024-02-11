using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Building : EquipableEntity
  {
    public Building(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
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
      CopulaAnimationFlags = GetInteger(data);
      EndOfClosingCopulaAnimation = GetInteger(data);
      LaserId = GetString(data);
      data.ReadBytes(4);
      SpaceStationType = GetInteger(data);
    }

    public int BuildingType { get; }

    public int BuildingTypeEx { get; }

    public int BuildingTabType { get; }

    public string InitCannonId1 { get; }

    public string InitCannonId2 { get; }

    public string InitCannonId3 { get; }

    public string InitCannonId4 { get; }

    public string CopulaId { get; }

    public int BuildingTunnelNumber { get; }

    public string UpgradeCopulaSmallId { get; }

    public string UpgradeCopulaBigId { get; }

    public string BuildLCTransporterId { get; }

    public string ChimneySmokeId { get; }

    public int NeedPower { get; }

    public string SlaveBuildingId { get; }

    public int MaxSubBuildingCount { get; }

    public int PowerLevel { get; }

    public int PowerTransmitterRange { get; }

    public int ConnectTransmitterRange { get; }

    public int FullEnergyPowerInDay { get; }

    public int ResourceInputOutput { get; }

    public int TickPerContainer { get; }

    public string ContainerId { get; }

    public int ContainerSmeltingTicks { get; }

    public int ResourcesPerTransport { get; }

    public string TransporterId { get; }

    public string BuildingAmmoId { get; }

    public int RangeOfBuildingFire { get; }

    public string ShootExplosionId { get; }

    public int AmmoReloadingTime { get; }

    public string BuildExplosionId { get; }

    public int CopulaAnimationFlags { get; }

    public int EndOfClosingCopulaAnimation { get; }

    public string LaserId { get; }

    public int SpaceStationType { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
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
          bw.Write(CopulaAnimationFlags);
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
