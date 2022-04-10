using Autofac;
using EarthTool.Common.Factories;
using System;
using System.Collections.Generic;
using System.Text;

namespace EarthTool.Common
{
  public class CommonModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.Register(_ => Encoding.GetEncoding("ISO-8859-2")).SingleInstance();
      builder.RegisterType<EarthInfoFactory>().AsImplementedInterfaces().SingleInstance();
    }
  }
}
