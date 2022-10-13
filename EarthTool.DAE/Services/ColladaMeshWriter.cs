using Collada141;
using EarthTool.Common.Bases;
using EarthTool.Common.Enums;
using EarthTool.DAE.Elements;
using EarthTool.MSH.Interfaces;
using System.IO;
using System.Xml.Serialization;

namespace EarthTool.DAE.Services
{
  public class ColladaMeshWriter : Writer<IMesh>
  {
    private readonly ColladaModelFactory _modelFactory;

    public ColladaMeshWriter(ColladaModelFactory modelFactory)
    {
      _modelFactory = modelFactory;
    }

    public override FileType OutputFileExtension => FileType.DAE;

    protected override string InternalWrite(IMesh data, string filePath)
    {
      var modelName = Path.GetFileNameWithoutExtension(filePath);

      WriteColladaModel(data, modelName, filePath);

      return filePath;
    }
    
    private void WriteColladaModel(IMesh model, string modelName, string outputFile)
    {
      var colladaModel = _modelFactory.GetColladaModel(model, modelName);
      var serializer = new XmlSerializer(typeof(COLLADA));
      using (var stream = new FileStream(outputFile, FileMode.Create))
      {
        serializer.Serialize(stream, colladaModel);
      }
    }
  }
}