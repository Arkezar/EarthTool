using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using DynamicData;
using EarthTool.Common.Interfaces;
using EarthTool.PAR.GUI.Extensions;
using EarthTool.PAR.GUI.Models;
using EarthTool.PAR.GUI.Services;
using EarthTool.PAR.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;

namespace EarthTool.PAR.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
  private readonly IReader<ParFile>     _reader;
  private readonly IWriter<ParFile>     _writer;
  private readonly ParameterTreeBuilder _treeBuilder;
  private          string               _filePath;
  private          object               _selectedItem;
  private          int                  _totalEntries;

  public string Title => "EarthTool PAR Editor" + (string.IsNullOrEmpty(FilePath) ? string.Empty : $" [{FilePath}]");

  public string TotalEntities => _totalEntries == 0 ? string.Empty : $"Total Parameter Entries: {_totalEntries}";

  public string FilePath
  {
    get => _filePath;
    set => this.RaiseAndSetIfChanged(ref _filePath, value);
  }

  public HierarchicalTreeDataGridSource<ParameterTreeNode> ParameterTree { get; }

  public ObservableCollection<ParameterTreeNode> Parameters { get; }

  public object SelectedItem
  {
    get => _selectedItem;
    set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
  }

  public ICommand OpenFileCommand { get; }

  public Interaction<Unit, string?> FileDialog { get; }

  public MainWindowViewModel(IReader<ParFile> reader, IWriter<ParFile> writer, ParameterTreeBuilder treeBuilder)
  {
    _reader = reader;
    _writer = writer;
    _treeBuilder = treeBuilder;

    FileDialog = new Interaction<Unit, string?>();
    OpenFileCommand = CreateOpenFileCommand();
    Parameters = new ObservableCollection<ParameterTreeNode>();
    ParameterTree = new HierarchicalTreeDataGridSource<ParameterTreeNode>(Parameters)
    {
      Columns =
      {
        new HierarchicalExpanderColumn<ParameterTreeNode>(
          new TextColumn<ParameterTreeNode, string>("Name", x => x.Name),
          x => x.Children,
          x => x.HasChildren)
      },
    };
    ParameterTree.RowSelection.SelectionChanged += OnRowSelectionChanged;
  }

  private void OnRowSelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs<ParameterTreeNode> e)
  {
    SelectedItem = e.SelectedItems.SingleOrDefault();
  }

  private ReactiveCommand<Unit, Unit> CreateOpenFileCommand()
    => ReactiveCommand.CreateFromTask(async () =>
    {
      FilePath = await FileDialog.Handle(Unit.Default);

      if (FilePath != null)
      {
        this.RaisePropertyChanged(nameof(Title));

        var parameters = _reader.Read(FilePath);

        Parameters.Clear();
        Parameters.AddRange(_treeBuilder.WithResearch(parameters.Research.Select(r => r.ToViewModel()))
          .WithEntityGroups(parameters.Groups.Select(g => g.ToViewModel()))
          .Build(true));
        _totalEntries = parameters.Research.Count() + parameters.Groups.Sum(g => g.Entities.Count());
        this.RaisePropertyChanged(nameof(TotalEntities));
      }
    });
}