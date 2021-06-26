﻿using Collada141;
using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Serialization;

namespace EarthTool.MSH
{
  public class MSHColladaConverter : MSHConverter
  {
    public MSHColladaConverter(ILogger<MSHColladaConverter> logger) : base(logger)
    {
    }

    public override int InternalConvert(Model model, string outputPath = null)
    {
      WriteColladaModel(model, outputPath);
      return 1;
    }

    private void WriteColladaModel(Model model, string outputPath)
    {
      var modelName = GetModelName(model);
      var resultDir = Path.Combine(outputPath, modelName);

      if (!Directory.Exists(resultDir))
      {
        Directory.CreateDirectory(resultDir);
      }

      var colladaModel = GetColladaModel(model, modelName);

      var serializer = new XmlSerializer(typeof(COLLADA));
      using (var stream = new FileStream(Path.Combine(resultDir, $"{modelName}.dae"), FileMode.Create))
      {

        serializer.Serialize(stream, colladaModel);
      }

      File.WriteAllText(Path.Combine(resultDir, $"{modelName}.template"), model.Template.ToString());
      File.WriteAllText(Path.Combine(resultDir, $"{modelName}.slots"), model.MountPoints.ToString());
    }

    private COLLADA GetColladaModel(Model model, string modelName)
    {
      var images = GetImages(model.Parts, modelName);
      var materials = GetMaterials(model.Parts, modelName);
      var geometries = GetGeometries(model.Parts, modelName);
      var lights = GetLights(model);
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

    private IEnumerable<(Material Material, Effect Effect)> GetMaterials(IEnumerable<ModelPart> parts, string modelName)
    {
      //var id = $"{modelName}-Part-{i}";
      return parts.Select((p, i) => (GetMaterial(p, i, modelName), GetEffect(p, i, modelName)));
    }

    private Effect GetEffect(ModelPart p, int i, string modelName)
    {
      var id = $"{modelName}-Part-{i}";
      var effect = new Effect
      {
        Id = $"{id}-effect",
        Name = $"{id}-effect"
      };

      var profile = new Profile_COMMON
      {
        Technique = new Profile_COMMONTechnique
        {
          Sid = "common",
          Lambert = new Profile_COMMONTechniqueLambert
          {
            Emission = new Common_Color_Or_Texture_Type
            {
              Color = new Common_Color_Or_Texture_TypeColor
              {
                Value = "0 0 0 1"
              }
            },
            Index_Of_Refraction = new Common_Float_Or_Param_Type
            {
              Float = new Common_Float_Or_Param_TypeFloat
              {
                Value = 1
              }
            },
            Diffuse = new Common_Color_Or_Texture_Type
            {
              Texture = new Common_Color_Or_Texture_TypeTexture
              {
                Texcoord = "UVMap",
                Texture = $"{id}-sampler"
              }
            }
          }
        }
      };

      var surface = new Fx_Surface_Common
      {
        Type = Fx_Surface_Type_Enum.Item2D,
      };
      surface.Init_From.Add(new Fx_Surface_Init_From_Common
      {
        Value = $"{id}-texture"
      });

      var sampler = new Fx_Sampler2D_Common
      {
        Source = $"{id}-surface"
      };

      profile.Newparam.Add(new Common_Newparam_Type
      {
        Sid = $"{id}-surface",
        Surface = surface
      });

      profile.Newparam.Add(new Common_Newparam_Type
      {
        Sid = $"{id}-sampler",
        Sampler2D = sampler
      });

      effect.Fx_Profile_Abstract.Add(profile);
      return effect;
    }

    private Material GetMaterial(ModelPart part, int i, string modelName)
    {
      var id = $"{modelName}-Part-{i}";
      return new Material
      {
        Id = $"{id}-material",
        Name = $"{id}-material",
        Instance_Effect = new Instance_Effect
        {
          Url = $"#{id}-effect"
        }
      };
    }

    private Library_Images GetImages(IEnumerable<ModelPart> parts, string modelName)
    {
      var libraryImages = new Library_Images();
      var id = $"{modelName}-Part";
      parts.Select((p, i) => new Image
      {
        Id = $"{id}-{i}-texture",
        Name = $"{id}-{i}-texture",
        Init_From = Path.ChangeExtension(p.Texture.FileName, "png")
      }).ToList().ForEach(t => libraryImages.Image.Add(t));

      return libraryImages;
    }

    private IEnumerable<(Light Light, Node LightNode)> GetLights(Model model)
    {
      return model.Lights.Where(l => l.IsAvailable).Select((l, i) => (GetLight(l, i), GetLightNode(l, i)));
    }

    private Node GetLightNode(Models.Elements.Light light, int i)
    {
      var id = $"Light-{i}";
      var node = new Node()
      {
        Id = id,
        Name = id
      };

      //TODO: transformation matrix
      var rotationZdeg = Math.PI / 180.0 * (light.Direction * 360.0 / 255.0);
      var rotationYdeg = Math.PI / 180.0 * (-90 - 180 / Math.PI * light.Tilt);

      var rotationZcos = (float)Math.Cos(rotationZdeg);
      var rotationZsin = (float)Math.Sin(rotationZdeg);

      var rotationZ = new Matrix4x4(rotationZcos, -rotationZsin, 0, 0,
                                    rotationZsin, rotationZcos, 0, 0,
                                    0, 0, 1, 0,
                                    0, 0, 0, 1);

      var rotationYcos = (float)Math.Cos(rotationYdeg);
      var rotationYsin = (float)Math.Sin(rotationYdeg);

      var rotationY = new Matrix4x4(rotationYcos, 0, rotationYsin, 0,
                                    0, 1, 0, 0,
                                    -rotationYsin, 0, rotationYcos, 0,
                                    0, 0, 0, 1);

      var rotation = rotationZ * rotationY;

      var transformMatrix = rotation;
      transformMatrix.M14 = light.X;
      transformMatrix.M24 = light.Y;
      transformMatrix.M34 = light.Z;

      node.Matrix.Add(new Matrix()
      {
        Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}", transformMatrix.M11,
                                                                                                                                     transformMatrix.M12,
                                                                                                                                     transformMatrix.M13,
                                                                                                                                     transformMatrix.M14,
                                                                                                                                     transformMatrix.M21,
                                                                                                                                     transformMatrix.M22,
                                                                                                                                     transformMatrix.M23,
                                                                                                                                     transformMatrix.M24,
                                                                                                                                     transformMatrix.M31,
                                                                                                                                     transformMatrix.M32,
                                                                                                                                     transformMatrix.M33,
                                                                                                                                     transformMatrix.M34,
                                                                                                                                     transformMatrix.M41,
                                                                                                                                     transformMatrix.M42,
                                                                                                                                     transformMatrix.M43,
                                                                                                                                     transformMatrix.M44)
      });

      var instanceGeometry = new Instance_Light()
      {
        Url = $"#{id}"
      };

      node.Instance_Light.Add(instanceGeometry);

      return node;
    }

    private Light GetLight(Models.Elements.Light light, int i)
    {
      return new Light()
      {
        Id = $"Light-{i}",
        Name = $"Light-{i}",
        Technique_Common = i != 4 ? GetSpotLight(light) : GetPointLight(light)
      };
    }

    private LightTechnique_Common GetSpotLight(Models.Elements.Light light)
    {
      return new LightTechnique_Common()
      {
        Spot = new LightTechnique_CommonSpot()
        {
          Color = new TargetableFloat3()
          {
            Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", light.Color.R / 255f, light.Color.G / 255f, light.Color.B / 255f)
          },
          Constant_Attenuation = new TargetableFloat()
          {
            Value = light.Length
          },
          Linear_Attenuation = new TargetableFloat()
          {
            Value = light.Ambience
          },
          Quadratic_Attenuation = new TargetableFloat()
          {
            Value = 0
          },
          Falloff_Angle = new TargetableFloat()
          {
            Value = light.Width * 180.0 / Math.PI
          }
        }
      };
    }

    private LightTechnique_Common GetPointLight(Models.Elements.Light light)
    {
      return new LightTechnique_Common()
      {
        Point = new LightTechnique_CommonPoint()
        {
          Color = new TargetableFloat3()
          {
            Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", light.Color.R / 255f, light.Color.G / 255f, light.Color.B / 255f)
          },
          Constant_Attenuation = new TargetableFloat()
          {
            Value = light.Length
          },
          Linear_Attenuation = new TargetableFloat()
          {
            Value = 0
          },
          Quadratic_Attenuation = new TargetableFloat()
          {
            Value = 0
          }
        }
      };
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

    private string GetModelName(Model model)
    {
      return Path.GetFileNameWithoutExtension(model.FilePath);
    }

    private IEnumerable<(Geometry Geometry, Node GeometryNode)> GetGeometries(IEnumerable<ModelPart> parts, string modelName)
    {
      return parts.Select((part, i) =>
      {
        return (GetGeometry(part, i, modelName), GetGeometryNode(part, i, modelName));
      });
    }

    private Node GetGeometryNode(ModelPart part, int i, string modelName)
    {
      var id = $"{modelName}-Part-{i}";
      var node = new Node()
      {
        Id = id,
        Name = id
      };

      node.Translate.Add(new TargetableFloat3()
      {
        Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", part.Offset.X, part.Offset.Y, part.Offset.Z)
      });

      var instanceGeometry = new Instance_Geometry()
      {
        Url = $"#{id}",
        Bind_Material = new Bind_Material()
      };

      var instanceMaterial = new Instance_Material
      {
        Symbol = $"{id}-material",
        Target = $"#{id}-material",
      };
      instanceMaterial.Bind_Vertex_Input.Add(new Instance_MaterialBind_Vertex_Input
      {
        Input_Set = 0,
        Input_Semantic = "TEXCOORD",
        Semantic = "UVMap"
      });

      instanceGeometry.Bind_Material.Technique_Common.Add(instanceMaterial);

      node.Instance_Geometry.Add(instanceGeometry);

      return node;
    }

    private Geometry GetGeometry(ModelPart part, int i, string modelName)
    {
      var id = $"{modelName}-Part-{i}";

      var geometry = new Geometry
      {
        Name = id,
        Id = id
      };

      var positions = GetSource("positions", part.Vertices, v => new float[] { v.Position.X, v.Position.Y, v.Position.Z });
      var normals = GetSource("normals", part.Vertices, v => new float[] { v.Normal.X, v.Normal.Y, v.Normal.Z });
      var uv = GetMapSource("map", part.Vertices, v => new float[] { v.U, v.V });

      var vertices = new Vertices()
      {
        Id = "vertices"
      };

      vertices.Input.Add(new InputLocal()
      {
        Semantic = "POSITION",
        Source = "#positions"
      });

      var poly = new Polylist
      {
        Count = (ulong)part.Faces.Count,
        Vcount = string.Join(' ', Enumerable.Repeat(3, part.Faces.Count)),
        P = string.Join(' ', part.Faces.SelectMany(f => new string[] { f.V1.ToString(), f.V2.ToString(), f.V3.ToString() })),
        Material = id + "-material"
      };

      poly.Input.Add(new InputLocalOffset()
      {
        Semantic = "VERTEX",
        Source = "#vertices",
        Offset = 0
      });

      poly.Input.Add(new InputLocalOffset()
      {
        Semantic = "NORMAL",
        Source = "#normals",
        Offset = 0
      });

      poly.Input.Add(new InputLocalOffset()
      {
        Semantic = "TEXCOORD",
        Source = "#map",
        Offset = 0
      });

      var mesh = new Mesh
      {
        Vertices = vertices
      };

      mesh.Source.Add(positions);
      mesh.Source.Add(normals);
      mesh.Source.Add(uv);
      mesh.Polylist.Add(poly);

      geometry.Mesh = mesh;

      return geometry;
    }

    private Source GetSource<T>(string name, IList<T> data, Func<T, IEnumerable<float>> func)
    {
      var source = new Source
      {
        Id = name,
        Name = name
      };
      var sourceArray = new Float_Array
      {
        Id = $"{name}-array",
        Count = (ulong)data.Count * 3,
        Value = string.Join(" ", data.SelectMany(func).Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)))
      };
      source.Float_Array = sourceArray;
      source.Technique_Common = new SourceTechnique_Common
      {
        Accessor = new Accessor
        {
          Source = $"#{name}-array",
          Count = (ulong)data.Count,
          Stride = 3
        }
      };
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "X",
        Type = "float"
      });
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "Y",
        Type = "float"
      });
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "Z",
        Type = "float"
      });
      return source;
    }

    private Source GetMapSource<T>(string name, IList<T> data, Func<T, IEnumerable<float>> func)
    {
      var source = new Source
      {
        Id = name,
        Name = name
      };
      var sourceArray = new Float_Array
      {
        Id = $"{name}-array",
        Count = (ulong)data.Count * 2,
        Value = string.Join(" ", data.SelectMany(func).Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)))
      };
      source.Float_Array = sourceArray;
      source.Technique_Common = new SourceTechnique_Common
      {
        Accessor = new Accessor
        {
          Source = $"#{name}-array",
          Count = (ulong)data.Count,
          Stride = 2
        }
      };
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "S",
        Type = "float"
      });
      source.Technique_Common.Accessor.Param.Add(new Param()
      {
        Name = "T",
        Type = "float"
      });
      return source;
    }
  }
}
