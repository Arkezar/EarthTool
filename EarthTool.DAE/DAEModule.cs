using EarthTool.Common.Interfaces;
using EarthTool.DAE.Elements;
using EarthTool.DAE.Services;
using EarthTool.MSH.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.DAE
{
  public static class DAEModule
  {
    public static IServiceCollection AddDaeServices(this IServiceCollection services)
      => services
        .AddTransient<ColladaModelFactory>()
        .AddTransient<MaterialFactory>()
        .AddTransient<LightingFactory>()
        .AddTransient<GeometriesFactory>()
        .AddTransient<AnimationsFactory>()
        .AddTransient<SlotFactory>()
        .AddTransient<IReader<IMesh>, ColladaMeshReader>()
        .AddTransient<IWriter<IMesh>, ColladaMeshWriter>();
  }
}