using Autofac;
using EarthTool.Commands;
using System.CommandLine;

namespace EarthTool.WD
{
  public class WDModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<WDCommand>().As<Command>().SingleInstance();
      builder.RegisterType<WDExtractor>().AsImplementedInterfaces();
    }
  }
}
