using EarthTool.PAR.Extensions;
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
      BaseUnderAttack = data.ReadParameterString();
      BuildingUnderAttack = data.ReadParameterString();
      SpacePortUnderAttack = data.ReadParameterString();
      EnemyLandInBase = data.ReadParameterString();
      LowMaterials = data.ReadParameterString();
      LowMaterialsInBase = data.ReadParameterString();
      LowPower = data.ReadParameterString();
      LowPowerInBase = data.ReadParameterString();
      ResearchComplete = data.ReadParameterString();
      ProductionStarted = data.ReadParameterString();
      ProductionCompleted = data.ReadParameterString();
      ProductionCanceled = data.ReadParameterString();
      PlatoonLost = data.ReadParameterString();
      PlatoonCreated = data.ReadParameterString();
      PlatoonDisbanded = data.ReadParameterString();
      UnitLost = data.ReadParameterString();
      TransporterArrived = data.ReadParameterString();
      ArtifactLocated = data.ReadParameterString();
      ArtifactRecovered = data.ReadParameterString();
      NewAreaLocationFound = data.ReadParameterString();
      EnemyMainBaseLocated = data.ReadParameterString();
      NewSourceFieldLocated = data.ReadParameterString();
      SourceFieldExploited = data.ReadParameterString();
      BuildingLost = data.ReadParameterString();
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

    public string ArtifactLocated { get; set; }

    public string ArtifactRecovered { get; set; }

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
        () => ArtifactLocated,
        () => ArtifactRecovered,
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
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.WriteParameterString(BaseUnderAttack, encoding);
      bw.WriteParameterString(BuildingUnderAttack, encoding);
      bw.WriteParameterString(SpacePortUnderAttack, encoding);
      bw.WriteParameterString(EnemyLandInBase, encoding);
      bw.WriteParameterString(LowMaterials, encoding);
      bw.WriteParameterString(LowMaterialsInBase, encoding);
      bw.WriteParameterString(LowPower, encoding);
      bw.WriteParameterString(LowPowerInBase, encoding);
      bw.WriteParameterString(ResearchComplete, encoding);
      bw.WriteParameterString(ProductionStarted, encoding);
      bw.WriteParameterString(ProductionCompleted, encoding);
      bw.WriteParameterString(ProductionCanceled, encoding);
      bw.WriteParameterString(PlatoonLost, encoding);
      bw.WriteParameterString(PlatoonCreated, encoding);
      bw.WriteParameterString(PlatoonDisbanded, encoding);
      bw.WriteParameterString(UnitLost, encoding);
      bw.WriteParameterString(TransporterArrived, encoding);
      bw.WriteParameterString(ArtifactLocated, encoding);
      bw.WriteParameterString(ArtifactRecovered, encoding);
      bw.WriteParameterString(NewAreaLocationFound, encoding);
      bw.WriteParameterString(EnemyMainBaseLocated, encoding);
      bw.WriteParameterString(NewSourceFieldLocated, encoding);
      bw.WriteParameterString(SourceFieldExploited, encoding);
      bw.WriteParameterString(BuildingLost, encoding);

      return output.ToArray();
    }
  }
}
