using Autofac;
using EarthTool.Common.Interfaces;
using EarthTool.DAE.Elements;
using EarthTool.DAE.Services;

namespace EarthTool.DAE
{
  public class MSHColladaModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<ModelFactory>();
      builder.RegisterType<ColladaModelFactory>();
      builder.RegisterType<ColladaMeshReader>().AsSelf().AsImplementedInterfaces();
      builder.RegisterType<MaterialFactory>();
      builder.RegisterType<LightingFactory>();
      builder.RegisterType<GeometriesFactory>();
      builder.RegisterType<AnimationsFactory>();
      builder.RegisterType<SlotFactory>();
      builder.RegisterType<MSHColladaConverter>().AsImplementedInterfaces().Keyed<IMSHConverter>("dae");
    }
  }
}
