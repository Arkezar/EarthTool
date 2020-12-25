using Collada141;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EarthTool.MSH
{
  public abstract class MSHConverter : IMSHConverter
  {
    private readonly ILogger _logger;

    public MSHConverter(ILogger logger)
    {
      _logger = logger;
    }

    public int Convert(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);
      var model = new Model(filePath);

      _logger.LogDebug("Loaded {VerticesNumber} vertices, {FacesNumber} faces",
                       model.Parts.Sum(p => p.Vertices.Count),
                       model.Parts.Sum(p => p.Faces.Count));

      return InternalConvert(model, outputPath);
    }

    public abstract int InternalConvert(Model model, string outputPath = null);
  }
}
