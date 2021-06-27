using Autofac;
using EarthTool.Commands;
using System.CommandLine;

namespace EarthTool.MSH
{
  public class MSHModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<MSHCommand>().As<Command>().SingleInstance();
    }
  }
}
