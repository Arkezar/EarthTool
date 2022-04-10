using System;
using System.Collections.Generic;
using System.Text;

namespace EarthTool.MSH.Interfaces
{
  public interface IMeshWriter
  {
    void Write(string fileName, IMesh mesh);
  }
}
