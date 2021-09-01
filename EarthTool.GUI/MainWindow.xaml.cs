using Autofac.Features.Indexed;
using EarthTool.Common.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EarthTool.GUI
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private readonly IWDExtractor _wdExtractor;
    private readonly ITEXConverter _texConverter;
    private readonly IIndex<string, IMSHConverter> _mshConverters;

    public MainWindow(IWDExtractor wdExtractor, ITEXConverter texConverter, IIndex<string, IMSHConverter> mshConverters)
    {
      InitializeComponent();
      _wdExtractor = wdExtractor;
      _texConverter = texConverter;
      _mshConverters = mshConverters;
    }

    private void Execute_Click(object sender, RoutedEventArgs e)
    {
      var inputFile = InputFileSelector.Content.ToString();
      var outputFolder = OutputDirectorySelector.Content.ToString();
      var fileType = System.IO.Path.GetExtension(inputFile);
      switch (fileType)
      {
        case ".wd":
          _wdExtractor.Extract(inputFile, outputFolder);
          break;
        case ".tex":
          _texConverter.Convert(inputFile, outputFolder);
          break;
        case ".msh":
          _mshConverters["dae"].Convert(inputFile, outputFolder);
          break;
      }
    }

    private void InputFileSelector_Click(object sender, RoutedEventArgs e)
    {
      var fileDialog = new OpenFileDialog();
      fileDialog.Multiselect = false;
      fileDialog.Filter = "Earth data files (*.msh, *.tex, *.wd)|*.msh;*.tex;*.wd";
      if (fileDialog.ShowDialog() ?? false)
      {
        InputFileSelector.Content = fileDialog.FileName;
      }
    }

    private void OutputDirectorySelecter_Click(object sender, RoutedEventArgs e)
    {
      var fileDialog = new SaveFileDialog();
      fileDialog.FileName = System.IO.Path.GetFileName(InputFileSelector.Content.ToString());
      fileDialog.Filter = "All files (*.*)|*.*";
      if (fileDialog.ShowDialog() ?? false)
      {
        OutputDirectorySelector.Content = System.IO.Path.GetDirectoryName(fileDialog.FileName);
      }
    }
  }
}
