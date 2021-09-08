using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthTool.GUI.Core.ViewModels
{
  public class MainViewModel : MvxNavigationViewModel
  {
    public IMvxAsyncCommand ShowWDCommand => new MvxAsyncCommand(ShowWD);

    public MainViewModel(ILoggerFactory logFactory, IMvxNavigationService navigationService) : base(logFactory, navigationService)
    {
    }

    private Task ShowWD()
    {
      return NavigationService.Navigate<WdViewModel>();
    }
  }
}
