using Autofac;
using AutoFixture;
using AutoFixture.Kernel;
using EarthTool.Common.Interfaces;
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

    public ArchiveTestsBase()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      Fixture = new Fixture();

      var cb = new ContainerBuilder();
      cb.RegisterModule<WDModule>();
      cb.RegisterInstance(new NullLoggerFactory()).AsImplementedInterfaces();
      cb.RegisterGeneric(typeof(NullLogger<>)).As(typeof(ILogger<>)).SingleInstance();
      var container = cb.Build();

      ArchiveFactory = container.Resolve<IArchiveFactory>();
      Compressor = container.Resolve<ICompressor>();
      Decompressor = container.Resolve<IDecompressor>();
    }
  }
}
