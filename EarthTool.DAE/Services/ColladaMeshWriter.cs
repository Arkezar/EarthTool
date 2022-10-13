using Collada141;
using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.DAE.Elements;
using EarthTool.MSH.Interfaces;
using System.IO;
using System.Xml.Serialization;

namespace EarthTool.DAE.Services
{
  public class ColladaMeshWriter : IWriter<IMesh>
  {
    private readonly ColladaModelFactory _modelFactory;

    public ColladaMeshWriter(ColladaModelFactory modelFactory)
    {
      _modelFactory = modelFactory;
    }

    public string OutputFileExtension => "dae";

    public string Write(IMesh data, string filePath)
    {
      return WriteModel(data, ModelType.DAE, filePath);
    }

    private string WriteModel(IMesh model, ModelType outputModelType, string outputPath)
    {
      var modelName = GetModelName(model);

      if (!Directory.Exists(outputPath))
      {
        Directory.CreateDirectory(outputPath);
      }

      var outputFileName = GetOutputFileName(outputPath, modelName, outputModelType);

      WriteColladaModel(model, modelName, outputFileName);

      return outputFileName;
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

    private string GetOutputFileName(string outputPath, string modelName, ModelType outputModelType)
      => Path.Combine(outputPath, $"{modelName}.{outputModelType.ToString().ToLower()}");

    private string GetModelName(IMesh model)
    {
      return Path.GetFileNameWithoutExtension(model.FileHeader.FilePath);
    }
  }
}