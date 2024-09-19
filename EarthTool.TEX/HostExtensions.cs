using EarthTool.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.TEX
{
  public static class HostExtensions
  {
    public static IServiceCollection AddTexServices(this IServiceCollection services)
      => services
        .AddScoped<ITEXConverter, TEXConverter>();
  }
}