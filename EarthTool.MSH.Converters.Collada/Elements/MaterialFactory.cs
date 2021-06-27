using Collada141;
using EarthTool.MSH.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.MSH.Converters.Collada.Elements
{
  public class MaterialFactory
  {
    public IEnumerable<Image> GetImages(IEnumerable<ModelPart> parts, string modelName)
    {
      var id = $"{modelName}-Part";
      return parts.Select((p, i) => new Image
      {
        Id = $"{id}-{i}-texture",
        Name = $"{id}-{i}-texture",
        Init_From = Path.ChangeExtension(p.Texture.FileName, "png")
      });
    }

    public IEnumerable<(Material Material, Effect Effect)> GetMaterials(IEnumerable<ModelPart> parts, string modelName)
    {
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
  }
}
