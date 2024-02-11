﻿using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Passive : PassiveEntity
  {
    public Passive(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
    }
  }
}
