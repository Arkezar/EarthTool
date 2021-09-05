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
    public MultiExplosion(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      UseDownBuilding = BitConverter.ToInt32(data.ReadBytes(4));
      DownBuildingStart = BitConverter.ToInt32(data.ReadBytes(4));
      DownBuildingTime = BitConverter.ToInt32(data.ReadBytes(4));
      SubObject1 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      Time1 = BitConverter.ToInt32(data.ReadBytes(4));
      Angle1 = BitConverter.ToInt32(data.ReadBytes(4));
      Dist4X1 = BitConverter.ToInt32(data.ReadBytes(4));
      SubObject2 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      Time2 = BitConverter.ToInt32(data.ReadBytes(4));
      Angle2 = BitConverter.ToInt32(data.ReadBytes(4));
      Dist4X2 = BitConverter.ToInt32(data.ReadBytes(4));
      SubObject3 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      Time3 = BitConverter.ToInt32(data.ReadBytes(4));
      Angle3 = BitConverter.ToInt32(data.ReadBytes(4));
      Dist4X3 = BitConverter.ToInt32(data.ReadBytes(4));
      SubObject4 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      Time4 = BitConverter.ToInt32(data.ReadBytes(4));
      Angle4 = BitConverter.ToInt32(data.ReadBytes(4));
      Dist4X4 = BitConverter.ToInt32(data.ReadBytes(4));
      SubObject5 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      Time5 = BitConverter.ToInt32(data.ReadBytes(4));
      Angle5 = BitConverter.ToInt32(data.ReadBytes(4));
      Dist4X5 = BitConverter.ToInt32(data.ReadBytes(4));
      SubObject6 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      Time6 = BitConverter.ToInt32(data.ReadBytes(4));
      Angle6 = BitConverter.ToInt32(data.ReadBytes(4));
      Dist4X6 = BitConverter.ToInt32(data.ReadBytes(4));
      SubObject7 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      Time7 = BitConverter.ToInt32(data.ReadBytes(4));
      Angle7 = BitConverter.ToInt32(data.ReadBytes(4));
      Dist4X7 = BitConverter.ToInt32(data.ReadBytes(4));
      SubObject8 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      Time8 = BitConverter.ToInt32(data.ReadBytes(4));
      Angle8 = BitConverter.ToInt32(data.ReadBytes(4));
      Dist4X8 = BitConverter.ToInt32(data.ReadBytes(4));
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
  }
}
