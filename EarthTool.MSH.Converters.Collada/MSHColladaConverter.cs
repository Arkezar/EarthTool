using Collada141;
using EarthTool.MSH.Converters.Collada.Elements;
using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EarthTool.MSH.Converters.Collada
{
  public class MSHColladaConverter : MSHConverter
  {
    private readonly ModelFactory _modelFactory;

    public MSHColladaConverter(ModelFactory modelFactory, ILogger<MSHColladaConverter> logger) : base(logger)
    {
      _modelFactory = modelFactory;
    }

    public override Task InternalConvert(Model model, string outputPath = null)
    {
      WriteColladaModel(model, outputPath);
      return Task.CompletedTask;
    }

    private void WriteColladaModel(Model model, string outputPath)
    {
      var modelName = GetModelName(model);

      if (!Directory.Exists(outputPath))
      {
        Directory.CreateDirectory(outputPath);
      }

      var colladaModel = _modelFactory.GetColladaModel(model, modelName);

      var serializer = new XmlSerializer(typeof(COLLADA));
      using (var stream = new FileStream(Path.Combine(outputPath, $"{modelName}.dae"), FileMode.Create))
      {

        serializer.Serialize(stream, colladaModel);
      }

      File.WriteAllText(Path.Combine(outputPath, $"{modelName}.template"), model.Template.ToString());
      File.WriteAllText(Path.Combine(outputPath, $"{modelName}.slots"), model.MountPoints.ToString());
    }

    private string GetModelName(Model model)
    {
      return Path.GetFileNameWithoutExtension(model.FilePath);
    }
  }
}
