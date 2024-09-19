using EarthTool.Common.Factories;
using EarthTool.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace EarthTool.Common
{
  public static class HostExtensions
  {
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
      => services
        .AddSingleton(Encoding.GetEncoding("ISO-8859-2"))
        .AddScoped<IEarthInfoFactory, EarthInfoFactory>();
  }
}