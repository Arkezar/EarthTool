#nullable enable

using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.GLTF
{
  public static class HostExtensions
  {
    public static IServiceCollection AddGltfServices(this IServiceCollection services)
    {
      return services
        .AddSingleton<GltfInterchange>()
        .AddSingleton<GltfImportPlanSerializer>()
        .AddSingleton<GltfCliReportSerializer>();
    }
  }
}
