﻿using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;

namespace EarthTool.PAR.Models
{
  public class Special : InteractableEntity
  {
    public Special()
    {
    }

    public Special(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
    }
  }
}