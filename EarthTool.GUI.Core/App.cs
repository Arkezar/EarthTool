using EarthTool.Common.Interfaces;
using EarthTool.GUI.Core.ViewModels;
using EarthTool.WD.Factories;
using EarthTool.WD.Services;
using MvvmCross;
using MvvmCross.IoC;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthTool.GUI.Core
{
  public class App : MvxApplication
  {
    public override void Initialize()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      Mvx.IoCProvider.RegisterType<IArchiver, ArchiverService>();
      Mvx.IoCProvider.RegisterType<IArchiveFactory, ArchiveFactory>();
      Mvx.IoCProvider.RegisterType<ICompressor, CompressorService>();
      Mvx.IoCProvider.RegisterType<IDecompressor, DecompressorService>();

      RegisterAppStart<MainViewModel>();
    }
  }
}
