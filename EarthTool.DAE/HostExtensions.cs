using EarthTool.Common.Interfaces;
using EarthTool.DAE.Elements;
using EarthTool.DAE.Services;
using EarthTool.MSH.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.DAE
{
  public static class HostExtensions
  {
    public static IServiceCollection AddDaeServices(this IServiceCollection services)
      => services
        .AddScoped<ColladaModelFactory>()
        .AddScoped<MaterialFactory>()
        .AddScoped<LightingFactory>()
        .AddScoped<GeometriesFactory>()
        .AddScoped<AnimationsFactory>()
        .AddScoped<SlotFactory>()
        .AddScoped<IReader<IMesh>, ColladaMeshReader>()
        .AddScoped<IWriter<IMesh>, ColladaMeshWriter>();
  }
}
