using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class PlayerTalkPack : Entity
  {
    public PlayerTalkPack(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type)
    {
      BaseUnderAttack = GetString(data);
      BuildingUnderAttack = GetString(data);
      SpacePortUnderAttack = GetString(data);
      EnemyLandInBase = GetString(data);
      LowMaterials = GetString(data);
      LowMaterialsInBase = GetString(data);
      LowPower = GetString(data);
      LowPowerInBase = GetString(data);
      ResearchComplete = GetString(data);
      ProductionStarted = GetString(data);
      ProductionCompleted = GetString(data);
      ProductionCanceled = GetString(data);
      PlatoonLost = GetString(data);
      PlatoonCreated = GetString(data);
      PlatoonDisbanded = GetString(data);
      UnitLost = GetString(data);
      TransporterArrived = GetString(data);
      ArtefactLocated = GetString(data);
      ArtefactRecovered = GetString(data);
      NewAreaLocationFound = GetString(data);
      EnemyMainBaseLocated = GetString(data);
      NewSourceFieldLocated = GetString(data);
      SourceFieldExploited = GetString(data);
      BuildingLost = GetString(data);

    }

    public string BaseUnderAttack { get; }

    public string BuildingUnderAttack { get; }

    public string SpacePortUnderAttack { get; }

    public string EnemyLandInBase { get; }

    public string LowMaterials { get; }

    public string LowMaterialsInBase { get; }

    public string LowPower { get; }

    public string LowPowerInBase { get; }

    public string ResearchComplete { get; }

    public string ProductionStarted { get; }

    public string ProductionCompleted { get; }

    public string ProductionCanceled { get; }

    public string PlatoonLost { get; }

    public string PlatoonCreated { get; }

    public string PlatoonDisbanded { get; }

    public string UnitLost { get; }

    public string TransporterArrived { get; }

    public string ArtefactLocated { get; }

    public string ArtefactRecovered { get; }

    public string NewAreaLocationFound { get; }

    public string EnemyMainBaseLocated { get; }

    public string NewSourceFieldLocated { get; }

    public string SourceFieldExploited { get; }

    public string BuildingLost { get; }

  }
}
