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
    public Building(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      BuildingType = BitConverter.ToInt32(data.ReadBytes(4));
      BuildingTypeEx = BitConverter.ToInt32(data.ReadBytes(4));
      BuildingTabType = BitConverter.ToInt32(data.ReadBytes(4));
      InitCannonId1 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      InitCannonId2 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      InitCannonId3 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      InitCannonId4 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      CopulaId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      BuildingTunnelNumber = BitConverter.ToInt32(data.ReadBytes(4));
      UpgradeCopulaSmallId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      UpgradeCopulaBigId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      BuildLCTransporterId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      ChimneySmokeId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      NeedPower = BitConverter.ToInt32(data.ReadBytes(4));
      SlaveBuildingId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      MaxSubBuildingCount = BitConverter.ToInt32(data.ReadBytes(4));
      PowerLevel = BitConverter.ToInt32(data.ReadBytes(4));
      PowerTransmitterRange = BitConverter.ToInt32(data.ReadBytes(4));
      ConnectTransmitterRange = BitConverter.ToInt32(data.ReadBytes(4));
      FullEnergyPowerInDay = BitConverter.ToInt32(data.ReadBytes(4));
      ResourceInputOutput = BitConverter.ToInt32(data.ReadBytes(4));
      TickPerContainer = BitConverter.ToInt32(data.ReadBytes(4));
      ContainerId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      ContainerSmeltingTicks = BitConverter.ToInt32(data.ReadBytes(4));
      ResourcesPerTransport = BitConverter.ToInt32(data.ReadBytes(4));
      TransporterId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      BuildingAmmoId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      RangeOfBuildingFire = BitConverter.ToInt32(data.ReadBytes(4));
      ShootExplosionId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      AmmoReloadingTime = BitConverter.ToInt32(data.ReadBytes(4));
      BuildExplosionId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      CopulaAnimationFlags = BitConverter.ToInt32(data.ReadBytes(4));
      EndOfClosingCopulaAnimation = BitConverter.ToInt32(data.ReadBytes(4));
      LaserId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      SpaceStationType = BitConverter.ToInt32(data.ReadBytes(4));
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
  }
}
