using Collada141;
using EarthTool.Common.Enums;
using EarthTool.MSH.Converters.Collada.Elements;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EarthTool.MSH.Converters.Collada
{
  public class MSHColladaConverter : MSHConverter
  {
    private readonly ModelFactory _modelFactory;
    private readonly EarthMeshWriter _earthMeshWriter;

    public MSHColladaConverter(ModelFactory modelFactory, EarthMeshWriter earthMeshWriter, ILogger<MSHColladaConverter> logger) : base(logger)
    {
      _modelFactory = modelFactory;
      _earthMeshWriter = earthMeshWriter;
    }

    public override Task InternalConvert(ModelType outputModelType, IMesh model, string outputPath = null)
    {
      WriteModel(model, outputModelType, outputPath);
      return Task.CompletedTask;
    }

    private void WriteModel(IMesh model, ModelType outputModelType, string outputPath)
    {
      var modelName = GetModelName(model);

      if (!Directory.Exists(outputPath))
      {
        Directory.CreateDirectory(outputPath);
      }
      var outputFileName = GetOutputFileName(outputPath, modelName, outputModelType);

      switch (outputModelType)
      {
        case ModelType.DAE:
          WriteColladaModel(model, modelName, outputFileName);
          WriteMeshModel(model, Path.ChangeExtension(outputFileName, "msh1"));
          break;
        case ModelType.MSH:
          WriteMeshModel(model, outputFileName);
          break;
      }
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

    private void WriteMeshModel(IMesh model, string outputFile)
    {
      _earthMeshWriter.Write(outputFile, model);
    }

    private string GetOutputFileName(string outputPath, string modelName, ModelType outputModelType)
      => Path.Combine(outputPath, $"{modelName}.{outputModelType.ToString().ToLower()}");

    private string GetModelName(IMesh model)
    {
      return Path.GetFileNameWithoutExtension(model.FileHeader.FilePath);
    }

    protected override ModelType GetOutputType(string filePath)
    {
      {
        var inputType = GetInputType(filePath);
        return inputType switch
        {
          ModelType.MSH => ModelType.DAE,
          ModelType.DAE => ModelType.MSH,
          _ => throw new System.NotImplementedException()
        };
      }
    }

    protected override IMesh LoadModel(string filePath)
    {
      var outputType = GetOutputType(filePath);
      return outputType switch
      {
        ModelType.DAE => _modelFactory.GetMeshModel(filePath),
        ModelType.MSH => _modelFactory.GetColladaModel(filePath),
        ModelType.OBJ => throw new System.NotImplementedException(),
        _ => throw new System.NotImplementedException()
      };
    }
  }
}
