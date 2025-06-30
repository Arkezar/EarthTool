using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class BuildingViewModel : EquipableEntityViewModel
{
  private int _buildingType;
  private int _buildingTypeEx;
  private int _buildingTabType;
  private string _initCannonId1;
  private string _initCannonId2;
  private string _initCannonId3;
  private string _initCannonId4;
  private string _copulaId;
  private int _buildingTunnelNumber;
  private string _upgradeCopulaSmallId;
  private string _upgradeCopulaBigId;
  private string _buildLCTransporterId;
  private string _chimneySmokeId;
  private int _needPower;
  private string _slaveBuildingId;
  private int _maxSubBuildingCount;
  private int _powerLevel;
  private int _powerTransmitterRange;
  private int _connectTransmitterRange;
  private int _fullEnergyPowerInDay;
  private int _resourceInputOutput;
  private int _tickPerContainer;
  private string _containerId;
  private int _containerSmeltingTicks;
  private int _resourcesPerTransport;
  private string _transporterId;
  private string _buildingAmmoId;
  private int _rangeOfBuildingFire;
  private string _shootExplosionId;
  private int _ammoReloadingTime;
  private string _buildExplosionId;
  private int _copulaAnimationFlags;
  private int _endOfClosingCopulaAnimation;
  private string _laserId;
  private int _spaceStationType;

  public BuildingViewModel(Building building)
    : base(building)
  {
    _buildingType = building.BuildingType;
    _buildingTypeEx = building.BuildingTypeEx;
    _buildingTabType = building.BuildingTabType;
    _initCannonId1 = building.InitCannonId1;
    _initCannonId2 = building.InitCannonId2;
    _initCannonId3 = building.InitCannonId3;
    _initCannonId4 = building.InitCannonId4;
    _copulaId = building.CopulaId;
    _buildingTunnelNumber = building.BuildingTunnelNumber;
    _upgradeCopulaSmallId = building.UpgradeCopulaSmallId;
    _upgradeCopulaBigId = building.UpgradeCopulaBigId;
    _buildLCTransporterId = building.BuildLCTransporterId;
    _chimneySmokeId = building.ChimneySmokeId;
    _needPower = building.NeedPower;
    _slaveBuildingId = building.SlaveBuildingId;
    _maxSubBuildingCount = building.MaxSubBuildingCount;
    _powerLevel = building.PowerLevel;
    _powerTransmitterRange = building.PowerTransmitterRange;
    _connectTransmitterRange = building.ConnectTransmitterRange;
    _fullEnergyPowerInDay = building.FullEnergyPowerInDay;
    _resourceInputOutput = building.ResourceInputOutput;
    _tickPerContainer = building.TickPerContainer;
    _containerId = building.ContainerId;
    _containerSmeltingTicks = building.ContainerSmeltingTicks;
    _resourcesPerTransport = building.ResourcesPerTransport;
    _transporterId = building.TransporterId;
    _buildingAmmoId = building.BuildingAmmoId;
    _rangeOfBuildingFire = building.RangeOfBuildingFire;
    _shootExplosionId = building.ShootExplosionId;
    _ammoReloadingTime = building.AmmoReloadingTime;
    _buildExplosionId = building.BuildExplosionId;
    _copulaAnimationFlags = building.CopulaAnimationFlags;
    _endOfClosingCopulaAnimation = building.EndOfClosingCopulaAnimation;
    _laserId = building.LaserId;
    _spaceStationType = building.SpaceStationType;
  }

  public int BuildingType
  {
    get => _buildingType;
    set => this.RaiseAndSetIfChanged(ref _buildingType, value);
  }

  public int BuildingTypeEx
  {
    get => _buildingTypeEx;
    set => this.RaiseAndSetIfChanged(ref _buildingTypeEx, value);
  }

  public int BuildingTabType
  {
    get => _buildingTabType;
    set => this.RaiseAndSetIfChanged(ref _buildingTabType, value);
  }

  public string InitCannonId1
  {
    get => _initCannonId1;
    set => this.RaiseAndSetIfChanged(ref _initCannonId1, value);
  }

  public string InitCannonId2
  {
    get => _initCannonId2;
    set => this.RaiseAndSetIfChanged(ref _initCannonId2, value);
  }

  public string InitCannonId3
  {
    get => _initCannonId3;
    set => this.RaiseAndSetIfChanged(ref _initCannonId3, value);
  }

  public string InitCannonId4
  {
    get => _initCannonId4;
    set => this.RaiseAndSetIfChanged(ref _initCannonId4, value);
  }

  public string CopulaId
  {
    get => _copulaId;
    set => this.RaiseAndSetIfChanged(ref _copulaId, value);
  }

  public int BuildingTunnelNumber
  {
    get => _buildingTunnelNumber;
    set => this.RaiseAndSetIfChanged(ref _buildingTunnelNumber, value);
  }

  public string UpgradeCopulaSmallId
  {
    get => _upgradeCopulaSmallId;
    set => this.RaiseAndSetIfChanged(ref _upgradeCopulaSmallId, value);
  }

  public string UpgradeCopulaBigId
  {
    get => _upgradeCopulaBigId;
    set => this.RaiseAndSetIfChanged(ref _upgradeCopulaBigId, value);
  }

  public string BuildLCTransporterId
  {
    get => _buildLCTransporterId;
    set => this.RaiseAndSetIfChanged(ref _buildLCTransporterId, value);
  }

  public string ChimneySmokeId
  {
    get => _chimneySmokeId;
    set => this.RaiseAndSetIfChanged(ref _chimneySmokeId, value);
  }

  public int NeedPower
  {
    get => _needPower;
    set => this.RaiseAndSetIfChanged(ref _needPower, value);
  }

  public string SlaveBuildingId
  {
    get => _slaveBuildingId;
    set => this.RaiseAndSetIfChanged(ref _slaveBuildingId, value);
  }

  public int MaxSubBuildingCount
  {
    get => _maxSubBuildingCount;
    set => this.RaiseAndSetIfChanged(ref _maxSubBuildingCount, value);
  }

  public int PowerLevel
  {
    get => _powerLevel;
    set => this.RaiseAndSetIfChanged(ref _powerLevel, value);
  }

  public int PowerTransmitterRange
  {
    get => _powerTransmitterRange;
    set => this.RaiseAndSetIfChanged(ref _powerTransmitterRange, value);
  }

  public int ConnectTransmitterRange
  {
    get => _connectTransmitterRange;
    set => this.RaiseAndSetIfChanged(ref _connectTransmitterRange, value);
  }

  public int FullEnergyPowerInDay
  {
    get => _fullEnergyPowerInDay;
    set => this.RaiseAndSetIfChanged(ref _fullEnergyPowerInDay, value);
  }

  public int ResourceInputOutput
  {
    get => _resourceInputOutput;
    set => this.RaiseAndSetIfChanged(ref _resourceInputOutput, value);
  }

  public int TickPerContainer
  {
    get => _tickPerContainer;
    set => this.RaiseAndSetIfChanged(ref _tickPerContainer, value);
  }

  public string ContainerId
  {
    get => _containerId;
    set => this.RaiseAndSetIfChanged(ref _containerId, value);
  }

  public int ContainerSmeltingTicks
  {
    get => _containerSmeltingTicks;
    set => this.RaiseAndSetIfChanged(ref _containerSmeltingTicks, value);
  }

  public int ResourcesPerTransport
  {
    get => _resourcesPerTransport;
    set => this.RaiseAndSetIfChanged(ref _resourcesPerTransport, value);
  }

  public string TransporterId
  {
    get => _transporterId;
    set => this.RaiseAndSetIfChanged(ref _transporterId, value);
  }

  public string BuildingAmmoId
  {
    get => _buildingAmmoId;
    set => this.RaiseAndSetIfChanged(ref _buildingAmmoId, value);
  }

  public int RangeOfBuildingFire
  {
    get => _rangeOfBuildingFire;
    set => this.RaiseAndSetIfChanged(ref _rangeOfBuildingFire, value);
  }

  public string ShootExplosionId
  {
    get => _shootExplosionId;
    set => this.RaiseAndSetIfChanged(ref _shootExplosionId, value);
  }

  public int AmmoReloadingTime
  {
    get => _ammoReloadingTime;
    set => this.RaiseAndSetIfChanged(ref _ammoReloadingTime, value);
  }

  public string BuildExplosionId
  {
    get => _buildExplosionId;
    set => this.RaiseAndSetIfChanged(ref _buildExplosionId, value);
  }

  public int CopulaAnimationFlags
  {
    get => _copulaAnimationFlags;
    set => this.RaiseAndSetIfChanged(ref _copulaAnimationFlags, value);
  }

  public int EndOfClosingCopulaAnimation
  {
    get => _endOfClosingCopulaAnimation;
    set => this.RaiseAndSetIfChanged(ref _endOfClosingCopulaAnimation, value);
  }

  public string LaserId
  {
    get => _laserId;
    set => this.RaiseAndSetIfChanged(ref _laserId, value);
  }

  public int SpaceStationType
  {
    get => _spaceStationType;
    set => this.RaiseAndSetIfChanged(ref _spaceStationType, value);
  }
}