using Autofac;
using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Converters.Wavefront
{
  public class MSHWavefrontModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<MSHWavefrontConverter>().AsImplementedInterfaces().Keyed<IMSHConverter>("obj");
    }
  }
}
