using Collada141;
using EarthTool.MSH.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.DAE.Elements
{
  public class ColladaModelFactory
  {
    private readonly AnimationsFactory _animationsFactory;
    private readonly GeometriesFactory _geometriesFactory;
    private readonly MaterialFactory _materialFactory;
    private readonly LightingFactory _lightingFactory;
    private readonly SlotFactory _slotFactory;

    public ColladaModelFactory(AnimationsFactory animationsFactory,
      GeometriesFactory geometriesFactory,
      MaterialFactory materialFactory,
      LightingFactory lightingFactory,
      SlotFactory slotFactory)
    {
      _animationsFactory = animationsFactory;
      _geometriesFactory = geometriesFactory;
      _materialFactory = materialFactory;
      _lightingFactory = lightingFactory;
      _slotFactory = slotFactory;
    }

    public COLLADA GetColladaModel(IMesh model, string modelName)
    {
      var collada = CreateColladaObject();

      var images = _materialFactory.GetImages(model.PartsTree, modelName);
      var imagesLibrary = new Library_Images();
      images.ToList().ForEach(i => imagesLibrary.Image.Add(i));

      var animations = _animationsFactory.GetAnimations(model.PartsTree, modelName);
      var animationsLibrary = new Library_Animations();
      animations.ToList().ForEach(a => animationsLibrary.Animation.Add(a));

      var materials = _materialFactory.GetMaterials(model.PartsTree, modelName);
      var effectsLibrary = new Library_Effects();
      var materialsLibrary = new Library_Materials();
      materials.ToList().ForEach(m =>
      {
        effectsLibrary.Effect.Add(m.Effect);
        materialsLibrary.Material.Add(m.Material);
      });

      var geometries = _geometriesFactory.GetGeometries(model.PartsTree, modelName);
      var geometriesLibrary = new Library_Geometries();
      geometries.ToList().ForEach(g => geometriesLibrary.Geometry.Add(g));

      var lights = _lightingFactory.GetLights(model);
      var slots = _slotFactory.GetSlots(model);
      var lightsLibrary = new Library_Lights();
      lights.Select(l => l.Light).ToList().ForEach(l => lightsLibrary.Light.Add(l));
      slots.Select(l => l.Slot).ToList().ForEach(l => lightsLibrary.Light.Add(l));

      var emitterNodes = slots.Where(s => s.SlotNode.Name.StartsWith("BarrelMuzzle")).Select(s => s.SlotNode).ToList();

      var geometryNodes = _geometriesFactory.GetGeometryNodes(model.PartsTree, emitterNodes, modelName);
      var geometryRootNode = _geometriesFactory.GetGeometryRootNode(geometryNodes, model.PartsTree, modelName);
      var slotNodes = lights.Select(l => l.LightNode).ToList().Concat(slots.Select(s => s.SlotNode)).Except(emitterNodes).ToList();
      var scenes = GetScenes(geometryRootNode, slotNodes, modelName);
      var scene = GetScene(scenes);

      collada.Library_Lights.Add(lightsLibrary);
      collada.Library_Geometries.Add(geometriesLibrary);
      collada.Library_Visual_Scenes.Add(scenes);
      collada.Library_Images.Add(imagesLibrary);
      collada.Library_Effects.Add(effectsLibrary);
      collada.Library_Materials.Add(materialsLibrary);
      collada.Library_Animations.Add(animationsLibrary);
      collada.Scene = scene;

      return collada;
    }

    private static COLLADA CreateColladaObject()
    {
      var collada = new COLLADA { Asset = new Asset { Created = DateTime.Now, Up_Axis = UpAxisType.Z_UP } };
      return collada;
    }

    private COLLADAScene GetScene(Library_Visual_Scenes scenes)
    {
      var scene = new COLLADAScene();
      scene.Instance_Visual_Scene = new InstanceWithExtra() { Url = $"#{scenes.Visual_Scene.First().Id}" };

      return scene;
    }

    private Library_Visual_Scenes GetScenes(Node geometryNode, IEnumerable<Node> nodes, string modelName)
    {
      var visualScenes = new Library_Visual_Scenes();
      var visualScene = new Visual_Scene() { Id = "scene" };

      var masterNode = new Node() { Id = modelName, Name = modelName };
      masterNode.NodeProperty.Add(geometryNode);

      nodes.ToList().ForEach(l => geometryNode.NodeProperty.Add(l));

      visualScene.Node.Add(masterNode);
      visualScenes.Visual_Scene.Add(visualScene);

      return visualScenes;
    }
  }
}
