using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class PlayerTalkPackViewModel : EntityViewModel
{
  private string _baseUnderAttack;
  private string _buildingUnderAttack;
  private string _spacePortUnderAttack;
  private string _enemyLandInBase;
  private string _lowMaterials;
  private string _lowMaterialsInBase;
  private string _lowPower;
  private string _lowPowerInBase;
  private string _researchComplete;
  private string _productionStarted;
  private string _productionCompleted;
  private string _productionCanceled;
  private string _platoonLost;
  private string _platoonCreated;
  private string _platoonDisbanded;
  private string _unitLost;
  private string _transporterArrived;
  private string _artefactLocated;
  private string _artefactRecovered;
  private string _newAreaLocationFound;
  private string _enemyMainBaseLocated;
  private string _newSourceFieldLocated;
  private string _sourceFieldExploited;
  private string _buildingLost;

  public PlayerTalkPackViewModel(PlayerTalkPack entry)
    : base(entry)
  {
    _baseUnderAttack = entry.BaseUnderAttack;
    _buildingUnderAttack = entry.BuildingUnderAttack;
    _spacePortUnderAttack = entry.SpacePortUnderAttack;
    _enemyLandInBase = entry.EnemyLandInBase;
    _lowMaterials = entry.LowMaterials;
    _lowMaterialsInBase = entry.LowMaterialsInBase;
    _lowPower = entry.LowPower;
    _lowPowerInBase = entry.LowPowerInBase;
    _researchComplete = entry.ResearchComplete;
    _productionStarted = entry.ProductionStarted;
    _productionCompleted = entry.ProductionCompleted;
    _productionCanceled = entry.ProductionCanceled;
    _platoonLost = entry.PlatoonLost;
    _platoonCreated = entry.PlatoonCreated;
    _platoonDisbanded = entry.PlatoonDisbanded;
    _unitLost = entry.UnitLost;
    _transporterArrived = entry.TransporterArrived;
    _artefactLocated = entry.ArtefactLocated;
    _artefactRecovered = entry.ArtefactRecovered;
    _newAreaLocationFound = entry.NewAreaLocationFound;
    _enemyMainBaseLocated = entry.EnemyMainBaseLocated;
    _newSourceFieldLocated = entry.NewSourceFieldLocated;
    _sourceFieldExploited = entry.SourceFieldExploited;
    _buildingLost = entry.BuildingLost;
  }

  public string BaseUnderAttack
  {
    get => _baseUnderAttack;
    set => this.RaiseAndSetIfChanged(ref _baseUnderAttack, value);
  }

  public string BuildingUnderAttack
  {
    get => _buildingUnderAttack;
    set => this.RaiseAndSetIfChanged(ref _buildingUnderAttack, value);
  }

  public string SpacePortUnderAttack
  {
    get => _spacePortUnderAttack;
    set => this.RaiseAndSetIfChanged(ref _spacePortUnderAttack, value);
  }

  public string EnemyLandInBase
  {
    get => _enemyLandInBase;
    set => this.RaiseAndSetIfChanged(ref _enemyLandInBase, value);
  }

  public string LowMaterials
  {
    get => _lowMaterials;
    set => this.RaiseAndSetIfChanged(ref _lowMaterials, value);
  }

  public string LowMaterialsInBase
  {
    get => _lowMaterialsInBase;
    set => this.RaiseAndSetIfChanged(ref _lowMaterialsInBase, value);
  }

  public string LowPower
  {
    get => _lowPower;
    set => this.RaiseAndSetIfChanged(ref _lowPower, value);
  }

  public string LowPowerInBase
  {
    get => _lowPowerInBase;
    set => this.RaiseAndSetIfChanged(ref _lowPowerInBase, value);
  }

  public string ResearchComplete
  {
    get => _researchComplete;
    set => this.RaiseAndSetIfChanged(ref _researchComplete, value);
  }

  public string ProductionStarted
  {
    get => _productionStarted;
    set => this.RaiseAndSetIfChanged(ref _productionStarted, value);
  }

  public string ProductionCompleted
  {
    get => _productionCompleted;
    set => this.RaiseAndSetIfChanged(ref _productionCompleted, value);
  }

  public string ProductionCanceled
  {
    get => _productionCanceled;
    set => this.RaiseAndSetIfChanged(ref _productionCanceled, value);
  }

  public string PlatoonLost
  {
    get => _platoonLost;
    set => this.RaiseAndSetIfChanged(ref _platoonLost, value);
  }

  public string PlatoonCreated
  {
    get => _platoonCreated;
    set => this.RaiseAndSetIfChanged(ref _platoonCreated, value);
  }

  public string PlatoonDisbanded
  {
    get => _platoonDisbanded;
    set => this.RaiseAndSetIfChanged(ref _platoonDisbanded, value);
  }

  public string UnitLost
  {
    get => _unitLost;
    set => this.RaiseAndSetIfChanged(ref _unitLost, value);
  }

  public string TransporterArrived
  {
    get => _transporterArrived;
    set => this.RaiseAndSetIfChanged(ref _transporterArrived, value);
  }

  public string ArtefactLocated
  {
    get => _artefactLocated;
    set => this.RaiseAndSetIfChanged(ref _artefactLocated, value);
  }

  public string ArtefactRecovered
  {
    get => _artefactRecovered;
    set => this.RaiseAndSetIfChanged(ref _artefactRecovered, value);
  }

  public string NewAreaLocationFound
  {
    get => _newAreaLocationFound;
    set => this.RaiseAndSetIfChanged(ref _newAreaLocationFound, value);
  }

  public string EnemyMainBaseLocated
  {
    get => _enemyMainBaseLocated;
    set => this.RaiseAndSetIfChanged(ref _enemyMainBaseLocated, value);
  }

  public string NewSourceFieldLocated
  {
    get => _newSourceFieldLocated;
    set => this.RaiseAndSetIfChanged(ref _newSourceFieldLocated, value);
  }

  public string SourceFieldExploited
  {
    get => _sourceFieldExploited;
    set => this.RaiseAndSetIfChanged(ref _sourceFieldExploited, value);
  }

  public string BuildingLost
  {
    get => _buildingLost;
    set => this.RaiseAndSetIfChanged(ref _buildingLost, value);
  }
}