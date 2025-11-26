using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System;

namespace EarthTool.CLI.Commands;

public class CommandTypeRegistrar : ITypeRegistrar
{
  private readonly IHostBuilder _builder;

  public CommandTypeRegistrar(IHostBuilder builder)
  {
    _builder = builder;
  }

  public void Register(Type service, Type implementation)
  {
    _builder.ConfigureServices((_, services) => services.AddSingleton(service, implementation));
  }

  public void RegisterInstance(Type service, object implementation)
  {
    _builder.ConfigureServices((_, services) => services.AddSingleton(service, implementation));
  }

  public void RegisterLazy(Type service, Func<object> factory)
  {
    _builder.ConfigureServices((_, services) => services.AddSingleton(service, _ => factory()));
  }

  public ITypeResolver Build()
  {
    return new CommandTypeResolver(_builder.Build());
  }
}
