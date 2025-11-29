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
      BaseUnderAttack = ReadString(data);
      BuildingUnderAttack = ReadString(data);
      SpacePortUnderAttack = ReadString(data);
      EnemyLandInBase = ReadString(data);
      LowMaterials = ReadString(data);
      LowMaterialsInBase = ReadString(data);
      LowPower = ReadString(data);
      LowPowerInBase = ReadString(data);
      ResearchComplete = ReadString(data);
      ProductionStarted = ReadString(data);
      ProductionCompleted = ReadString(data);
      ProductionCanceled = ReadString(data);
      PlatoonLost = ReadString(data);
      PlatoonCreated = ReadString(data);
      PlatoonDisbanded = ReadString(data);
      UnitLost = ReadString(data);
      TransporterArrived = ReadString(data);
      ArtefactLocated = ReadString(data);
      ArtefactRecovered = ReadString(data);
      NewAreaLocationFound = ReadString(data);
      EnemyMainBaseLocated = ReadString(data);
      NewSourceFieldLocated = ReadString(data);
      SourceFieldExploited = ReadString(data);
      BuildingLost = ReadString(data);
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
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      WriteString(bw, BaseUnderAttack, encoding);
      WriteString(bw, BuildingUnderAttack, encoding);
      WriteString(bw, SpacePortUnderAttack, encoding);
      WriteString(bw, EnemyLandInBase, encoding);
      WriteString(bw, LowMaterials, encoding);
      WriteString(bw, LowMaterialsInBase, encoding);
      WriteString(bw, LowPower, encoding);
      WriteString(bw, LowPowerInBase, encoding);
      WriteString(bw, ResearchComplete, encoding);
      WriteString(bw, ProductionStarted, encoding);
      WriteString(bw, ProductionCompleted, encoding);
      WriteString(bw, ProductionCanceled, encoding);
      WriteString(bw, PlatoonLost, encoding);
      WriteString(bw, PlatoonCreated, encoding);
      WriteString(bw, PlatoonDisbanded, encoding);
      WriteString(bw, UnitLost, encoding);
      WriteString(bw, TransporterArrived, encoding);
      WriteString(bw, ArtefactLocated, encoding);
      WriteString(bw, ArtefactRecovered, encoding);
      WriteString(bw, NewAreaLocationFound, encoding);
      WriteString(bw, EnemyMainBaseLocated, encoding);
      WriteString(bw, NewSourceFieldLocated, encoding);
      WriteString(bw, SourceFieldExploited, encoding);
      WriteString(bw, BuildingLost, encoding);

      return output.ToArray();
    }
  }
}
