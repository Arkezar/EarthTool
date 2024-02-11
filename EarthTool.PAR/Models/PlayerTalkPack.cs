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
    public PlayerTalkPack(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, fieldTypes)
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

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(BaseUnderAttack.Length);
          bw.Write(encoding.GetBytes(BaseUnderAttack));
          bw.Write(BuildingUnderAttack.Length);
          bw.Write(encoding.GetBytes(BuildingUnderAttack));
          bw.Write(SpacePortUnderAttack.Length);
          bw.Write(encoding.GetBytes(SpacePortUnderAttack));
          bw.Write(EnemyLandInBase.Length);
          bw.Write(encoding.GetBytes(EnemyLandInBase));
          bw.Write(LowMaterials.Length);
          bw.Write(encoding.GetBytes(LowMaterials));
          bw.Write(LowMaterialsInBase.Length);
          bw.Write(encoding.GetBytes(LowMaterialsInBase));
          bw.Write(LowPower.Length);
          bw.Write(encoding.GetBytes(LowPower));
          bw.Write(LowPowerInBase.Length);
          bw.Write(encoding.GetBytes(LowPowerInBase));
          bw.Write(ResearchComplete.Length);
          bw.Write(encoding.GetBytes(ResearchComplete));
          bw.Write(ProductionStarted.Length);
          bw.Write(encoding.GetBytes(ProductionStarted));
          bw.Write(ProductionCompleted.Length);
          bw.Write(encoding.GetBytes(ProductionCompleted));
          bw.Write(ProductionCanceled.Length);
          bw.Write(encoding.GetBytes(ProductionCanceled));
          bw.Write(PlatoonLost.Length);
          bw.Write(encoding.GetBytes(PlatoonLost));
          bw.Write(PlatoonCreated.Length);
          bw.Write(encoding.GetBytes(PlatoonCreated));
          bw.Write(PlatoonDisbanded.Length);
          bw.Write(encoding.GetBytes(PlatoonDisbanded));
          bw.Write(UnitLost.Length);
          bw.Write(encoding.GetBytes(UnitLost));
          bw.Write(TransporterArrived.Length);
          bw.Write(encoding.GetBytes(TransporterArrived));
          bw.Write(ArtefactLocated.Length);
          bw.Write(encoding.GetBytes(ArtefactLocated));
          bw.Write(ArtefactRecovered.Length);
          bw.Write(encoding.GetBytes(ArtefactRecovered));
          bw.Write(NewAreaLocationFound.Length);
          bw.Write(encoding.GetBytes(NewAreaLocationFound));
          bw.Write(EnemyMainBaseLocated.Length);
          bw.Write(encoding.GetBytes(EnemyMainBaseLocated));
          bw.Write(NewSourceFieldLocated.Length);
          bw.Write(encoding.GetBytes(NewSourceFieldLocated));
          bw.Write(SourceFieldExploited.Length);
          bw.Write(encoding.GetBytes(SourceFieldExploited));
          bw.Write(BuildingLost.Length);
          bw.Write(encoding.GetBytes(BuildingLost));
        }
        return output.ToArray();
      }
    }
  }
}
