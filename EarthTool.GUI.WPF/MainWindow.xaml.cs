using MvvmCross.Platforms.Wpf.Views;
using System.Reflection;

namespace EarthTool.GUI.WPF
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MvxWindow
  {
    public MainWindow()
    {
      InitializeComponent();

      Title = "EarthTool " + Assembly.GetEntryAssembly().GetName().Version;
    }
  }
}
