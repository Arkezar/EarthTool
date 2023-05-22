using EarthTool.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.TEX
{
  public static class TEXModule
  {
    public static IServiceCollection AddTexServices(this IServiceCollection services)
      => services
        .AddTransient<ITEXConverter, TEXConverter>();
  }
}
