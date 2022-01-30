using Autofac;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Converters.Collada.Elements;

namespace EarthTool.MSH.Converters.Collada
{
  public class MSHColladaModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<ModelFactory>();
      builder.RegisterType<MeshModelFactory>();
      builder.RegisterType<ColladaModelFactory>();
      builder.RegisterType<MaterialFactory>();
      builder.RegisterType<LightingFactory>();
      builder.RegisterType<GeometriesFactory>();
      builder.RegisterType<AnimationsFactory>();
      builder.RegisterType<SlotFactory>();
      builder.RegisterType<MSHColladaConverter>().AsImplementedInterfaces().Keyed<IMSHConverter>("dae");
    }
  }
}
