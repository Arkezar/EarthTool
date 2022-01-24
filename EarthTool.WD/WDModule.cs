using Autofac;
using EarthTool.Commands;
using EarthTool.WD.Factories;
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
      builder.RegisterType<ArchiverService>().AsImplementedInterfaces();
      builder.RegisterType<ArchiveFactory>().AsImplementedInterfaces();
      builder.RegisterType<CompressorService>().AsImplementedInterfaces();
      builder.RegisterType<DecompressorService>().AsImplementedInterfaces();
    }
  }
}
