using Autofac;
using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Converters.Collada
{
  public class MSHColladaModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<MSHColladaConverter>().AsImplementedInterfaces().Keyed<IMSHConverter>("dae");
    }
  }
}
