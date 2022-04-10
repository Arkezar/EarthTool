using Collada141;
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
    private readonly EarthMeshReader _earthMeshReader;
    private readonly ILogger<ModelFactory> _logger;

    public ModelFactory(ColladaModelFactory colladaModelFactory, EarthMeshReader earthMeshReader, ILogger<ModelFactory> logger)
    {
      _colladaModelFactory = colladaModelFactory;
      _earthMeshReader = earthMeshReader;
      _logger = logger;
    }

    public IMesh GetMeshModel(string filePath)
      => _earthMeshReader.Read(filePath);

    public IMesh GetColladaModel(string filePath)
      => throw new NotImplementedException();// _earthMeshReader.GetColladaModel(filePath);

    public COLLADA GetColladaModel(IMesh model, string modelName)
      => _colladaModelFactory.GetColladaModel(model, modelName);
  }
}
