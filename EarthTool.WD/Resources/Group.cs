﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EarthTool.WD.Resources
{
  public class Group : Resource
  {
    public Group(string filename, (uint, uint, uint) fileInfo, byte[] data) : base(filename, fileInfo, data)
    {
    }
  }
}
