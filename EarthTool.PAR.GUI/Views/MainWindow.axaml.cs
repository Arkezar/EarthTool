using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using EarthTool.PAR.GUI.ViewModels;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace EarthTool.PAR.GUI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
  public MainWindow()
  {
    InitializeComponent();
    this.WhenActivated(action => 
      action(ViewModel!.FileDialog.RegisterHandler(DoShowFileDialogAsync)));
  }
  
  private async Task DoShowFileDialogAsync(IInteractionContext<Unit, string?> interaction)
  {
    var file = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
    {
      Title = "Open PAR File",
      AllowMultiple = false,
      FileTypeFilter = new List<FilePickerFileType>
      {
        new FilePickerFileType("PAR Files")
        {
          Patterns = new [] { "*.par" }
        }
      }
    });
    
    interaction.SetOutput(file.SingleOrDefault()?.Path.AbsolutePath);
  }
}