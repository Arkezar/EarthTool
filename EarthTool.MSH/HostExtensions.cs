using EarthTool.Common.Interfaces;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.MSH
{
  public static class HostExtensions
  {
    public static IServiceCollection AddMshServices(this IServiceCollection services)
      => services
        .AddScoped<IReader<IMesh>, EarthMeshReader>()
        .AddScoped<IWriter<IMesh>, EarthMeshWriter>()
        .AddSingleton<IHierarchyBuilder, HierarchyBuilder>();
  }
}