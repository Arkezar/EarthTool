using EarthTool.Common.Interfaces;
using EarthTool.WD.Factories;
using EarthTool.WD.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.WD
{
  public static class HostExtensions
  {
    public static IServiceCollection AddWdServices(this IServiceCollection services)
      => services
        .AddSingleton<IWDExtractor, WDExtractor>()
        .AddSingleton<IArchiver, ArchiverService>()
        .AddSingleton<IArchiveFactory, ArchiveFactory>()
        .AddSingleton<ICompressor, CompressorService>()
        .AddSingleton<IDecompressor, DecompressorService>();
  }
}
