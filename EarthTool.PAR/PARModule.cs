using Autofac;
using System.CommandLine;

namespace EarthTool.PAR
{
  public class PARModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<PARCommand>().As<Command>().SingleInstance();
      builder.RegisterType<PARConverter>().AsImplementedInterfaces();
    }
  }
}
