using AutoFixture;
using EarthTool.Common;
using EarthTool.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace EarthTool.WD.Tests
{
  public class ArchiveTestsBase
  {
    protected Fixture Fixture;

    protected IArchiveFactory ArchiveFactory { get; }

    protected ICompressor Compressor { get; }

    protected IDecompressor Decompressor { get; }
    
    protected Encoding Encoding { get; }

    public ArchiveTestsBase()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      Fixture = new Fixture();

      var sc = new ServiceCollection()
        .AddCommonServices()
        .AddWdServices()
        .AddSingleton<ILoggerFactory, NullLoggerFactory>()
        .AddScoped(typeof(ILogger<>), typeof(NullLogger<>));

      var container = sc.BuildServiceProvider();

      ArchiveFactory = container.GetRequiredService<IArchiveFactory>();
      Compressor = container.GetRequiredService<ICompressor>();
      Decompressor = container.GetRequiredService<IDecompressor>();
      Encoding = container.GetRequiredService<Encoding>();
    }
  }
}
