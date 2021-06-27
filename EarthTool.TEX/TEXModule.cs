using Autofac;
using EarthTool.Commands;
using System.CommandLine;

namespace EarthTool.TEX
{
  public class TEXModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<TEXCommand>().As<Command>().SingleInstance();
      builder.RegisterType<TEXConverter>().AsImplementedInterfaces();
    }
  }
}
