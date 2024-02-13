using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class PlayerTalkPack : TypelessEntity
  {
    public PlayerTalkPack()
    {
    }

    public PlayerTalkPack(string name, IEnumerable<int> requiredResearch, BinaryReader data)
      : base(name, requiredResearch)
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

    public string BaseUnderAttack { get; set; }

    public string BuildingUnderAttack { get; set; }

    public string SpacePortUnderAttack { get; set; }

    public string EnemyLandInBase { get; set; }

    public string LowMaterials { get; set; }

    public string LowMaterialsInBase { get; set; }

    public string LowPower { get; set; }

    public string LowPowerInBase { get; set; }

    public string ResearchComplete { get; set; }

    public string ProductionStarted { get; set; }

    public string ProductionCompleted { get; set; }

    public string ProductionCanceled { get; set; }

    public string PlatoonLost { get; set; }

    public string PlatoonCreated { get; set; }

    public string PlatoonDisbanded { get; set; }

    public string UnitLost { get; set; }

    public string TransporterArrived { get; set; }

    public string ArtefactLocated { get; set; }

    public string ArtefactRecovered { get; set; }

    public string NewAreaLocationFound { get; set; }

    public string EnemyMainBaseLocated { get; set; }

    public string NewSourceFieldLocated { get; set; }

    public string SourceFieldExploited { get; set; }

    public string BuildingLost { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => IsStringMember(
        () => BaseUnderAttack,
        () => BuildingUnderAttack,
        () => SpacePortUnderAttack,
        () => EnemyLandInBase,
        () => LowMaterials,
        () => LowMaterialsInBase,
        () => LowPower,
        () => LowPowerInBase,
        () => ResearchComplete,
        () => ProductionStarted,
        () => ProductionCompleted,
        () => ProductionCanceled,
        () => PlatoonLost,
        () => PlatoonCreated,
        () => PlatoonDisbanded,
        () => UnitLost,
        () => TransporterArrived,
        () => ArtefactLocated,
        () => ArtefactRecovered,
        () => NewAreaLocationFound,
        () => EnemyMainBaseLocated,
        () => NewSourceFieldLocated,
        () => SourceFieldExploited,
        () => BuildingLost
      );
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
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