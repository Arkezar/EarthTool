using Autofac;
using EarthTool.WD.Legacy.Factories;
using EarthTool.WD.Legacy.Services;

namespace EarthTool.WD.Legacy
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
