using EarthTool.Common.Interfaces;
using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace EarthTool.GUI.Core.ViewModels
{
  public class WdViewModel : MvxViewModel
  {
    private readonly IArchiver _archivizer;

    public WdViewModel(IArchiver archivizer)
    {
      _archivizer = archivizer;

      Title = "WD Archivizer";
    }

    private string _title;
    public string Title
    {
      get => _title;
      set
      {
        SetProperty(ref _title, value);
        RaisePropertyChanged(() => Title);
      }
    }

    private string _filePath;
    public string FilePath
    {
      get => _filePath;
      set
      {
        SetProperty(ref _filePath, value, () => Refresh());
        RaisePropertyChanged(() => FilePath);
      }
    }

    private ObservableCollection<IArchiveFileHeader> _resources = new ObservableCollection<IArchiveFileHeader>();
    public ObservableCollection<IArchiveFileHeader> Resources
    {
      get => _resources;
      set => SetProperty(ref _resources, value);
    }

    private IArchiveFileHeader _selectedResource;
    public IArchiveFileHeader SelectedResource
    {
      get => _selectedResource;
      set
      {
        SetProperty(ref _selectedResource, value);
        RaisePropertyChanged(() => SelectedResource);
      }
    }

    private IArchive _archive;
    public IArchive Archive
    {
      get => _archive;
      set
      {
        SetProperty(ref _archive, value, () => RefreshResources());
        RaisePropertyChanged(() => Archive);
      }
    }

    public void Extract(string outputPath)
    {
      _archivizer.Extract(SelectedResource, outputPath);
    }

    public void ExtractAll(string outputPath)
    {
      _archivizer.ExtractAll(outputPath);
    }

    private void RefreshResources()
    {
      Resources = new ObservableCollection<IArchiveFileHeader>(_archive.CentralDirectory.FileHeaders);
    }

    private void Refresh()
    {
      if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
      {
        Archive = _archivizer.OpenArchive(FilePath);
      }
    }
  }
}
