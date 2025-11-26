using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System;

namespace EarthTool.CLI.Commands;

public class CommandTypeResolver : ITypeResolver, IDisposable
{
  private readonly IHost _provider;

  public CommandTypeResolver(IHost provider)
  {
    _provider = provider;
  }

  public object Resolve(Type type)
  {
    return _provider.Services.GetService(type);
  }

  public void Dispose()
  {
    _provider.Dispose();
  }
}