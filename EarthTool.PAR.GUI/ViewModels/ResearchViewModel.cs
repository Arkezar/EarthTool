using ReactiveUI;
using EarthTool.PAR.Models;
using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.PAR.GUI.ViewModels
{
    public class ResearchViewModel : ReactiveObject
    {
        private Research _research;

        public ResearchViewModel(Research research)
        {
            _research = research;
        }

        public Research Model => _research;

        public int Id
        {
            get => _research.Id;
            set => this.RaiseAndSetIfChanged(ref _research.Id, value);
        }

        public Faction Faction
        {
            get => _research.Faction;
            set => this.RaiseAndSetIfChanged(ref _research.Faction, value);
        }

        public string Name
        {
            get => _research.Name;
            set => this.RaiseAndSetIfChanged(ref _research.Name, value);
        }

        public int CampaignCost
        {
            get => _research.CampaignCost;
            set => this.RaiseAndSetIfChanged(ref _research.CampaignCost, value);
        }

        public int CampaignTime
        {
            get => _research.CampaignTime;
            set => this.RaiseAndSetIfChanged(ref _research.CampaignTime, value);
        }

        public int SkirmishCost
        {
            get => _research.SkirmishCost;
            set => this.RaiseAndSetIfChanged(ref _research.SkirmishCost, value);
        }

        public int SkirmishTime
        {
            get => _research.SkirmishTime;
            set => this.RaiseAndSetIfChanged(ref _research.SkirmishTime, value);
        }

        public string Video
        {
            get => _research.Video;
            set => this.RaiseAndSetIfChanged(ref _research.Video, value);
        }

        public ResearchType Type
        {
            get => _research.Type;
            set => this.RaiseAndSetIfChanged(ref _research.Type, value);
        }

        public string Mesh
        {
            get => _research.Mesh;
            set => this.RaiseAndSetIfChanged(ref _research.Mesh, value);
        }

        public int MeshParamsIndex
        {
            get => _research.MeshParamsIndex;
            set => this.RaiseAndSetIfChanged(ref _research.MeshParamsIndex, value);
        }

        public IEnumerable<int> RequiredResearch
        {
            get => _research.RequiredResearch;
            set => this.RaiseAndSetIfChanged(ref _research.RequiredResearch, value);
        }
    }
}
