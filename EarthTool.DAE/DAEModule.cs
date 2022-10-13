using Autofac;
using EarthTool.Common.Interfaces;
using EarthTool.DAE.Elements;
using EarthTool.DAE.Services;

namespace EarthTool.DAE
{
  public class DAEModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<ColladaModelFactory>();
      builder.RegisterType<MaterialFactory>();
      builder.RegisterType<LightingFactory>();
      builder.RegisterType<GeometriesFactory>();
      builder.RegisterType<AnimationsFactory>();
      builder.RegisterType<SlotFactory>();
      builder.RegisterType<ColladaMeshReader>().AsImplementedInterfaces();
      builder.RegisterType<ColladaMeshWriter>().AsImplementedInterfaces();
    }
  }
}
