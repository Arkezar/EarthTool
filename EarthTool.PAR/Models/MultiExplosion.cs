using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class MultiExplosion : InteractableEntity
  {
    public MultiExplosion(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      UseDownBuilding = GetInteger(data);
      DownBuildingStart = GetInteger(data);
      DownBuildingTime = GetInteger(data);
      SubObject1 = GetString(data);
      data.ReadBytes(4);
      Time1 = GetInteger(data);
      Angle1 = GetInteger(data);
      Dist4X1 = GetInteger(data);
      SubObject2 = GetString(data);
      data.ReadBytes(4);
      Time2 = GetInteger(data);
      Angle2 = GetInteger(data);
      Dist4X2 = GetInteger(data);
      SubObject3 = GetString(data);
      data.ReadBytes(4);
      Time3 = GetInteger(data);
      Angle3 = GetInteger(data);
      Dist4X3 = GetInteger(data);
      SubObject4 = GetString(data);
      data.ReadBytes(4);
      Time4 = GetInteger(data);
      Angle4 = GetInteger(data);
      Dist4X4 = GetInteger(data);
      SubObject5 = GetString(data);
      data.ReadBytes(4);
      Time5 = GetInteger(data);
      Angle5 = GetInteger(data);
      Dist4X5 = GetInteger(data);
      SubObject6 = GetString(data);
      data.ReadBytes(4);
      Time6 = GetInteger(data);
      Angle6 = GetInteger(data);
      Dist4X6 = GetInteger(data);
      SubObject7 = GetString(data);
      data.ReadBytes(4);
      Time7 = GetInteger(data);
      Angle7 = GetInteger(data);
      Dist4X7 = GetInteger(data);
      SubObject8 = GetString(data);
      data.ReadBytes(4);
      Time8 = GetInteger(data);
      Angle8 = GetInteger(data);
      Dist4X8 = GetInteger(data);
    }

    public int UseDownBuilding { get; }
    
    public int DownBuildingStart { get; }
    
    public int DownBuildingTime { get; }
    
    public string SubObject1 { get; }
    
    public int Time1 { get; }
    
    public int Angle1 { get; }
    
    public int Dist4X1 { get; }
    
    public string SubObject2 { get; }
    
    public int Time2 { get; }
    
    public int Angle2 { get; }
    
    public int Dist4X2 { get; }
    
    public string SubObject3 { get; }
    
    public int Time3 { get; }
    
    public int Angle3 { get; }
    
    public int Dist4X3 { get; }
    
    public string SubObject4 { get; }
    
    public int Time4 { get; }
    
    public int Angle4 { get; }
    
    public int Dist4X4 { get; }
    
    public string SubObject5 { get; }
    
    public int Time5 { get; }
    
    public int Angle5 { get; }
    
    public int Dist4X5 { get; }
    
    public string SubObject6 { get; }
    
    public int Time6 { get; }
    
    public int Angle6 { get; }
    
    public int Dist4X6 { get; }
    
    public string SubObject7 { get; }
    
    public int Time7 { get; }
    
    public int Angle7 { get; }
    
    public int Dist4X7 { get; }
    
    public string SubObject8 { get; }
    public int Time8 { get; }
    public int Angle8 { get; }
    public int Dist4X8 { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(UseDownBuilding);
          bw.Write(DownBuildingStart);
          bw.Write(DownBuildingTime);
          bw.Write(SubObject1.Length);
          bw.Write(encoding.GetBytes(SubObject1));
          bw.Write(-1);
          bw.Write(Time1);
          bw.Write(Angle1);
          bw.Write(Dist4X1);
          bw.Write(SubObject2.Length);
          bw.Write(encoding.GetBytes(SubObject2));
          bw.Write(-1);
          bw.Write(Time2);
          bw.Write(Angle2);
          bw.Write(Dist4X2);
          bw.Write(SubObject3.Length);
          bw.Write(encoding.GetBytes(SubObject3));
          bw.Write(-1);
          bw.Write(Time3);
          bw.Write(Angle3);
          bw.Write(Dist4X3);
          bw.Write(SubObject4.Length);
          bw.Write(encoding.GetBytes(SubObject4));
          bw.Write(-1);
          bw.Write(Time4);
          bw.Write(Angle4);
          bw.Write(Dist4X4);
          bw.Write(SubObject5.Length);
          bw.Write(encoding.GetBytes(SubObject5));
          bw.Write(-1);
          bw.Write(Time5);
          bw.Write(Angle5);
          bw.Write(Dist4X5);
          bw.Write(SubObject6.Length);
          bw.Write(encoding.GetBytes(SubObject6));
          bw.Write(-1);
          bw.Write(Time6);
          bw.Write(Angle6);
          bw.Write(Dist4X6);
          bw.Write(SubObject7.Length);
          bw.Write(encoding.GetBytes(SubObject7));
          bw.Write(-1);
          bw.Write(Time7);
          bw.Write(Angle7);
          bw.Write(Dist4X7);
          bw.Write(SubObject8.Length);
          bw.Write(encoding.GetBytes(SubObject8));
          bw.Write(-1);
          bw.Write(Time8);
          bw.Write(Angle8);
          bw.Write(Dist4X8);
        }
        return output.ToArray();
      }
    }
  }
}
