using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using MvvmCross.IoC;
using MvvmCross.Platforms.Wpf.Binding;
using MvvmCross.Platforms.Wpf.Core;
using MvvmCross.Plugin;

namespace EarthTool.GUI.WPF
{
  public class Setup : MvxWpfSetup<Core.App>
  {
    protected override ILoggerFactory CreateLogFactory()
    {
      return new LoggerFactory();
    }

    protected override ILoggerProvider CreateLogProvider()
    {
      return new DebugLoggerProvider();
    }
  }
}
