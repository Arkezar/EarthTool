using EarthTool.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EarthTool.LAN
{
  public static class LANModule
  {
    public static IServiceCollection AddLanServices(this IServiceCollection services)
      => services
        .AddTransient<ILANConverter, LANConverter>();
  }
}