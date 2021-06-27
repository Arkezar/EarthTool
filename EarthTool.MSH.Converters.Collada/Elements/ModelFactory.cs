﻿using Collada141;
using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.MSH.Converters.Collada.Elements
{
  public class ModelFactory
  {
    private readonly GeometriesFactory _geometriesFactory;
    private readonly MaterialFactory _materialFactory;
    private readonly LightingFactory _lightingFactory;
    private readonly ILogger<ModelFactory> _logger;

    public ModelFactory(GeometriesFactory geometriesFactory, MaterialFactory materialFactory, LightingFactory lightingFactory, ILogger<ModelFactory> logger)
    {
      _geometriesFactory = geometriesFactory;
      _materialFactory = materialFactory;
      _lightingFactory = lightingFactory;
      _logger = logger;
    }

    public COLLADA GetColladaModel(Model model, string modelName)
    {
      var images = _materialFactory.GetImages(model.Parts, modelName);
      var materials = _materialFactory.GetMaterials(model.Parts, modelName);
      var geometries = _geometriesFactory.GetGeometries(model.Parts, modelName);
      var lights = _lightingFactory.GetLights(model);
      var scenes = GetScenes(geometries.Select(g => g.GeometryNode), lights.Select(l => l.LightNode), modelName);
      var scene = GetScene(scenes);

      var collada = new COLLADA
      {
        Asset = new Asset
        {
          Created = DateTime.Now,
          Up_Axis = UpAxisType.Z_UP
        }
      };

      var lightsLibrary = new Library_Lights();
      lights.Select(l => l.Light).ToList().ForEach(l => lightsLibrary.Light.Add(l));
      collada.Library_Lights.Add(lightsLibrary);

      var geometriesLibrary = new Library_Geometries();
      geometries.Select(g => g.Geometry).ToList().ForEach(g => geometriesLibrary.Geometry.Add(g));

      var effectsLibrary = new Library_Effects();
      var materialsLibrary = new Library_Materials();
      materials.ToList().ForEach(m =>
      {
        effectsLibrary.Effect.Add(m.Effect);
        materialsLibrary.Material.Add(m.Material);
      });

      collada.Library_Geometries.Add(geometriesLibrary);
      collada.Library_Visual_Scenes.Add(scenes);
      collada.Library_Images.Add(images);
      collada.Library_Effects.Add(effectsLibrary);
      collada.Library_Materials.Add(materialsLibrary);
      collada.Scene = scene;

      return collada;
    }

    private COLLADAScene GetScene(Library_Visual_Scenes scenes)
    {
      var scene = new COLLADAScene();
      scene.Instance_Visual_Scene = new InstanceWithExtra()
      {
        Url = $"#{scenes.Visual_Scene.First().Id}"
      };

      return scene;
    }

    private Library_Visual_Scenes GetScenes(IEnumerable<Node> geometryNodes, IEnumerable<Node> lightNodes, string modelName)
    {
      var visualScenes = new Library_Visual_Scenes();
      var visualScene = new Visual_Scene()
      {
        Id = "scene"
      };

      var masterNode = new Node()
      {
        Id = modelName,
        Name = modelName
      };
      visualScene.Node.Add(masterNode);

      geometryNodes.ToList().ForEach(g => masterNode.NodeProperty.Add(g));
      lightNodes.ToList().ForEach(l => masterNode.NodeProperty.Add(l));
      visualScenes.Visual_Scene.Add(visualScene);

      return visualScenes;
    }
  }
}
