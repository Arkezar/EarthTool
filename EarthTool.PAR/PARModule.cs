using Autofac;
using EarthTool.PAR.Services;

namespace EarthTool.PAR
{
  public class PARModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<ParameterReader>().AsImplementedInterfaces();
    }
  }
}
