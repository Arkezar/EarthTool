using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.MSH
{
  public static class HostExtensions
  {
    public static IServiceCollection AddMshServices(this IServiceCollection services)
      => services
        .AddScoped<IMshReader, MshReader>()
        .AddScoped<IMshWriter, MshWriter>()
        .AddScoped<IMshValidator, MshValidator>();
  }
}
