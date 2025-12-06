using EarthTool.Common;
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
        .AddCommonServices()
        .AddSingleton<IReader<ParFile>, ParameterReader>()
        .AddSingleton<IWriter<ParFile>, ParameterWriter>();
  }
}
