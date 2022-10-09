using Autofac;

namespace EarthTool.TEX
{
  public class TEXModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<TEXConverter>().AsImplementedInterfaces();
    }
  }
}
