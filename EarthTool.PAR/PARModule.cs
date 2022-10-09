using Autofac;

namespace EarthTool.PAR
{
  public class PARModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<PARConverter>().AsImplementedInterfaces();
    }
  }
}
