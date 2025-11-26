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
        .AddScoped<IWDExtractor, WDExtractor>()
        .AddScoped<IArchiver, ArchiverService>()
        .AddScoped<IArchiveFactory, ArchiveFactory>()
        .AddScoped<ICompressor, CompressorService>()
        .AddScoped<IDecompressor, DecompressorService>();
  }
}
