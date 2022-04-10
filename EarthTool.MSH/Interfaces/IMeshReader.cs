using System;
using System.Collections.Generic;
using System.Text;

namespace EarthTool.MSH.Interfaces
{
  public interface IMeshReader
  {
    IMesh Read(string filePath);
  }
}
