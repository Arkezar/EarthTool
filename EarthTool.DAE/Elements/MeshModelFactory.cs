using Collada141;
using EarthTool.MSH.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace EarthTool.DAE.Elements
{
  public class MeshModelFactory
  {
    public MeshModelFactory()
    {

    }

    public EarthMesh GetColladaModel(string filePath)
    {
      var model = LoadColladaModel(filePath);
      return null;
    }

    private COLLADA LoadColladaModel(string filePath)
    {
      var serializer = new XmlSerializer(typeof(COLLADA));
      using (var stream = new FileStream(filePath, FileMode.Open))
      {
        return (COLLADA)serializer.Deserialize(stream);
      }
    }
  }
}
