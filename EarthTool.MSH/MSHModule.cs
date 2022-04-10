using Autofac;
using EarthTool.Commands;
using EarthTool.MSH.Services;
using System.CommandLine;

namespace EarthTool.MSH
{
  public class MSHModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<EarthMeshReader>();
      builder.RegisterType<EarthMeshWriter>();
      builder.RegisterType<MSHCommand>().As<Command>().SingleInstance();
    }
  }
}
