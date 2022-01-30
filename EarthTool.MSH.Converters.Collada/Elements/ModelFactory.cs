using Collada141;
using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.MSH.Converters.Collada.Elements
{
  public class ModelFactory
  {
    private readonly ColladaModelFactory _colladaModelFactory;
    private readonly MeshModelFactory _meshModelFactory;
    private readonly ILogger<ModelFactory> _logger;

    public ModelFactory(ColladaModelFactory colladaModelFactory, MeshModelFactory meshModelFactory, ILogger<ModelFactory> logger)
    {
      _colladaModelFactory = colladaModelFactory;
      _meshModelFactory = meshModelFactory;
      _logger = logger;
    }

    public Model GetMeshModel(string filePath)
      => _meshModelFactory.GetMeshModel(filePath);

    public Model GetColladaModel(string filePath)
      => _meshModelFactory.GetColladaModel(filePath);

    public COLLADA GetColladaModel(Model model, string modelName)
      => _colladaModelFactory.GetColladaModel(model, modelName);
  }
}
