using EarthTool.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.PAR
{
  public static class PARModule
  {
    public static IServiceCollection AddParServices(this IServiceCollection services)
      => services
        .AddTransient<IPARConverter, PARConverter>();
  }
}
