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
    private readonly IArchivizer _archivizer;

    public WdViewModel(IArchivizer archivizer)
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
        SetProperty(ref _filePath, value);
        RaisePropertyChanged(() => FilePath);

        _archivizer.SetArchiveFilePath(FilePath);
        Refresh();
      }
    }

    private ObservableCollection<IArchiveResource> _resources = new ObservableCollection<IArchiveResource>();
    public ObservableCollection<IArchiveResource> Resources
    {
      get => _resources;
      set => SetProperty(ref _resources, value);
    }

    private IArchiveResource _selectedResource;
    public IArchiveResource SelectedResource
    {
      get => _selectedResource;
      set
      {
        SetProperty(ref _selectedResource, value);
        RaisePropertyChanged(() => SelectedResource);
      }
    }

    private IArchiveHeader _archiveHeader;
    public IArchiveHeader ArchiveHeader
    {
      get => _archiveHeader;
      set
      {
        SetProperty(ref _archiveHeader, value);
        RaisePropertyChanged(() => ArchiveHeader);
      }
    }

    private IArchive _archive;
    public IArchive Archive
    {
      get => _archive;
      set
      {
        SetProperty(ref _archive, value);
        RaisePropertyChanged(() => Archive);

        RefreshResources();
      }
    }

    public void Extract(string outputPath)
    {
      _archivizer.Extract(outputPath, SelectedResource);
    }

    public void ExtractAll(string outputPath)
    {
      _archivizer.ExtractAll(outputPath);
    }

    private void RefreshResources()
    {
      Resources = new ObservableCollection<IArchiveResource>(_archive.Resources);
    }

    private void Refresh()
    {
      if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath) && _archivizer.VerifyFile())
      {
        ArchiveHeader = _archivizer.GetArchiveHeader();
        Archive = _archivizer.GetArchiveDescriptor();
      }
    }
  }
}
