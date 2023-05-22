using EarthTool.Common.Interfaces;
using EarthTool.WD.Factories;
using EarthTool.WD.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.WD
{
  public static class WDModule
  {
    public static IServiceCollection AddWdServices(this IServiceCollection services)
      => services
        .AddTransient<IWDExtractor, WDExtractor>()
        .AddTransient<IArchiver, ArchiverService>()
        .AddTransient<IArchiveFactory, ArchiveFactory>()
        .AddTransient<ICompressor, CompressorService>()
        .AddTransient<IDecompressor, DecompressorService>();
  }
}
