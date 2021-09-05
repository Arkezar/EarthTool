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
    public PlayerTalkPack(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type)
    {
      BaseUnderAttack = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      BuildingUnderAttack = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      SpacePortUnderAttack = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      EnemyLandInBase = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      LowMaterials = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      LowMaterialsInBase = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      LowPower = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      LowPowerInBase = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      ResearchComplete = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      ProductionStarted = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      ProductionCompleted = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      ProductionCanceled = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      PlatoonLost = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      PlatoonCreated = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      PlatoonDisbanded = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      UnitLost = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      TransporterArrived = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      ArtefactLocated = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      ArtefactRecovered = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      NewAreaLocationFound = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      EnemyMainBaseLocated = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      NewSourceFieldLocated = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      SourceFieldExploited = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      BuildingLost = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));

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
