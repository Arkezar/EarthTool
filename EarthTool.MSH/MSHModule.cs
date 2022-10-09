using Autofac;
using EarthTool.MSH.Services;

namespace EarthTool.MSH
{
  public class MSHModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<EarthMeshReader>().AsSelf().AsImplementedInterfaces();
      builder.RegisterType<EarthMeshWriter>().AsSelf().AsImplementedInterfaces();
    }
  }
}
