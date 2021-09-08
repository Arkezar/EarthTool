using Autofac;
using EarthTool.Commands;
using EarthTool.WD.Services;
using System.CommandLine;

namespace EarthTool.WD
{
  public class WDModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<WDCommand>().As<Command>().SingleInstance();
      builder.RegisterType<WDExtractor>().AsImplementedInterfaces();
      builder.RegisterType<ArchivizerService>().AsImplementedInterfaces();
      builder.RegisterType<EncryptionService>().AsImplementedInterfaces();
    }
  }
}
