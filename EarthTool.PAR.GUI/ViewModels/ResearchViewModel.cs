using ReactiveUI;
using EarthTool.PAR.Enums;
using EarthTool.PAR.Models;
using System.Collections.Generic;

namespace EarthTool.PAR.GUI.ViewModels
{
  public class ResearchViewModel : ReactiveObject
  {
    private int              _id;
    private Faction          _faction;
    private string           _name;
    private int              _campaignCost;
    private int              _campaignTime;
    private int              _skirmishCost;
    private int              _skirmishTime;
    private string           _video;
    private ResearchType     _type;
    private string           _mesh;
    private int              _meshParamsIndex;
    private IEnumerable<int> _requiredResearch;

    public ResearchViewModel(Research research)
    {
      _id = research.Id;
      _faction = research.Faction;
      _name = research.Name;
      _campaignCost = research.CampaignCost;
      _campaignTime = research.CampaignTime;
      _skirmishCost = research.SkirmishCost;
      _skirmishTime = research.SkirmishTime;
      _video = research.Video;
      _type = research.Type;
      _mesh = research.Mesh;
      _meshParamsIndex = research.MeshParamsIndex;
      _requiredResearch = research.RequiredResearch;
    }

    public int Id
    {
      get => _id;
      set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    public Faction Faction
    {
      get => _faction;
      set => this.RaiseAndSetIfChanged(ref _faction, value);
    }

    public string Name
    {
      get => _name;
      set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public int CampaignCost
    {
      get => _campaignCost;
      set => this.RaiseAndSetIfChanged(ref _campaignCost, value);
    }

    public int CampaignTime
    {
      get => _campaignTime;
      set => this.RaiseAndSetIfChanged(ref _campaignTime, value);
    }

    public int SkirmishCost
    {
      get => _skirmishCost;
      set => this.RaiseAndSetIfChanged(ref _skirmishCost, value);
    }

    public int SkirmishTime
    {
      get => _skirmishTime;
      set => this.RaiseAndSetIfChanged(ref _skirmishTime, value);
    }

    public string Video
    {
      get => _video;
      set => this.RaiseAndSetIfChanged(ref _video, value);
    }

    public ResearchType Type
    {
      get => _type;
      set => this.RaiseAndSetIfChanged(ref _type, value);
    }

    public string Mesh
    {
      get => _mesh;
      set => this.RaiseAndSetIfChanged(ref _mesh, value);
    }

    public int MeshParamsIndex
    {
      get => _meshParamsIndex;
      set => this.RaiseAndSetIfChanged(ref _meshParamsIndex, value);
    }

    public IEnumerable<int> RequiredResearch
    {
      get => _requiredResearch;
      set => this.RaiseAndSetIfChanged(ref _requiredResearch, value);
    }
  }
}