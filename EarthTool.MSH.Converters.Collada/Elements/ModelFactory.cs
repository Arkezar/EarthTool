using Collada141;
using EarthTool.MSH.Converters.Collada.Services;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.MSH.Converters.Collada.Elements
{
  public class ModelFactory
  {
    private readonly ColladaModelFactory _colladaModelFactory;
    private readonly ColladaMeshReader _colladaMeshReader;
    private readonly EarthMeshReader _earthMeshReader;
    private readonly ILogger<ModelFactory> _logger;

    public ModelFactory(ColladaModelFactory colladaModelFactory, ColladaMeshReader colladaMeshReader, EarthMeshReader earthMeshReader, ILogger<ModelFactory> logger)
    {
      _colladaModelFactory = colladaModelFactory;
      _colladaMeshReader = colladaMeshReader;
      _earthMeshReader = earthMeshReader;
      _logger = logger;
    }

    public IMesh GetMeshModel(string filePath)
      => _earthMeshReader.Read(filePath);

    public IMesh GetColladaModel(string filePath)
      => _colladaMeshReader.Read(filePath);

    public COLLADA GetColladaModel(IMesh model, string modelName)
      => _colladaModelFactory.GetColladaModel(model, modelName);
  }
}
