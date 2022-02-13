using Collada141;
using EarthTool.MSH.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace EarthTool.MSH.Converters.Collada.Elements
{
  public class MeshModelFactory
  {
    public MeshModelFactory()
    {

    }

    public Model GetMeshModel(string filePath)
    {
      using (var stream = new FileStream(filePath, FileMode.Open))
      {
        return new Model(filePath, stream);
      }
    }

    public Model GetColladaModel(string filePath)
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
