﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class Lights : List<Light>
  {
    const int NUMBER_OF_LIGHTS = 5;

    public int NumberOfAvailableLights =>
      this.Count(l => l.IsAvailable);

    public Lights(Stream stream)
    {
      AddRange(Enumerable.Range(0, NUMBER_OF_LIGHTS).Select(_ => new Light(stream)));
    }
  }
}
