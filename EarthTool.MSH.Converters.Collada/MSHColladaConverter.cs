using Collada141;
using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Converters.Collada.Elements;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EarthTool.MSH.Converters.Collada
{
  public class MSHColladaConverter : MSHConverter, IWriter<IMesh>
  {
    private readonly ModelFactory _modelFactory;
    private readonly EarthMeshWriter _earthMeshWriter;

    public MSHColladaConverter(ModelFactory modelFactory, EarthMeshWriter earthMeshWriter,
      ILogger<MSHColladaConverter> logger) : base(logger)
    {
      _modelFactory = modelFactory;
      _earthMeshWriter = earthMeshWriter;
    }

    public string OutputFileExtension => "dae";
    
    public string Write(IMesh data, string filePath)
    {
      return WriteModel(data, ModelType.DAE, filePath);
    }

    public override Task InternalConvert(ModelType outputModelType, IMesh model, string outputPath = null)
    {
      WriteModel(model, outputModelType, outputPath);
      return Task.CompletedTask;
    }

    private string WriteModel(IMesh model, ModelType outputModelType, string outputPath)
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
          break;
        case ModelType.MSH:
          WriteMeshModel(model, outputFileName);
          break;
      }

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

    private void WriteMeshModel(IMesh model, string outputFile)
    {
      _earthMeshWriter.Write(model, outputFile);
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
        _ => throw new System.NotImplementedException()
      };
    }
  }
}