using Autofac;
using EarthTool.WD.Factories;
using EarthTool.WD.Services;

namespace EarthTool.WD
{
  public class WDModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<WDExtractor>().AsImplementedInterfaces();
      builder.RegisterType<ArchiverService>().AsImplementedInterfaces();
      builder.RegisterType<ArchiveFactory>().AsImplementedInterfaces();
      builder.RegisterType<CompressorService>().AsImplementedInterfaces();
      builder.RegisterType<DecompressorService>().AsImplementedInterfaces();
    }
  }
}
