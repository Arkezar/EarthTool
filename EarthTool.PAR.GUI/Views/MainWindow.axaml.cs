using Avalonia.Controls;
using Avalonia.Input;
using EarthTool.PAR.GUI.ViewModels;

namespace EarthTool.PAR.GUI.Views;

public partial class MainWindow : Window
{
  public MainWindow()
  {
    InitializeComponent();
  }
  
  private void Entity_PointerPressed(object? sender, PointerPressedEventArgs e)
  {
    if (sender is Border border && border.DataContext is EntityListItemViewModel entityVm)
    {
      // Set the selected entity in the parent ViewModel
      if (DataContext is MainWindowViewModel viewModel)
      {
        viewModel.SelectedEntity = entityVm;
      }
    }
  }
}
