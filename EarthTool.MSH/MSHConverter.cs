using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using EarthTool.MSH.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.MSH
{
  public abstract class MSHConverter : IMSHConverter
  {
    private readonly ILogger _logger;

    public MSHConverter(ILogger logger)
    {
      _logger = logger;
    }

    public Task Convert(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);
      var model = LoadModel(filePath);

      _logger.LogDebug("Loaded {VerticesNumber} vertices, {FacesNumber} faces",
                       model.Geometries.Sum(p => p.Vertices.Count()),
                       model.Geometries.Sum(p => p.Faces.Count()));

      return InternalConvert(GetOutputType(filePath), model, outputPath);
    }

    public abstract Task InternalConvert(ModelType modelType, IMesh model, string outputPath = null);

    public virtual IConverter WithOptions(IReadOnlyCollection<Option> options)
    {
      throw new System.NotImplementedException();
    }

    protected abstract ModelType GetOutputType(string filePath);

    protected ModelType GetInputType(string filePath)
      => Enum.Parse<ModelType>(Path.GetExtension(filePath).Trim('.'), true);

    protected abstract IMesh LoadModel(string filePath);
  }
}
