using Autofac;
using EarthTool.MSH.Services;

namespace EarthTool.MSH
{
  public class MSHModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<EarthMeshReader>().AsImplementedInterfaces();
      builder.RegisterType<EarthMeshWriter>().AsImplementedInterfaces();
      builder.RegisterType<HierarchyBuilder>().AsImplementedInterfaces().SingleInstance();
    }
  }
}
