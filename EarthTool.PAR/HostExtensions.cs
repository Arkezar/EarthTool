using EarthTool.Common.Interfaces;
using EarthTool.PAR.Models;
using EarthTool.PAR.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.PAR
{
  public static class HostExtensions
  {
    public static IServiceCollection AddParServices(this IServiceCollection services)
      => services
        .AddScoped<IReader<ParFile>, ParameterReader>()
        .AddScoped<IWriter<ParFile>, ParameterWriter>();
  }
}
