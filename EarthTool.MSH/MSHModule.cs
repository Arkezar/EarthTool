using EarthTool.Common.Interfaces;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.MSH
{
  public static class MSHModule
  {
    public static IServiceCollection AddMshServices(this IServiceCollection services)
      => services
        .AddTransient<IReader<IMesh>, EarthMeshReader>()
        .AddTransient<IWriter<IMesh>, EarthMeshWriter>()
        .AddSingleton<IHierarchyBuilder, HierarchyBuilder>();
  }
}
