using EarthTool.PAR.Enums;
using EarthTool.PAR.GUI.Extensions;
using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class EntityGroupViewModel : ViewModelBase
{
  public Faction Faction { get; set; }

  public EntityGroupType GroupType { get; set; }

  public IEnumerable<EntityViewModel> Entities { get; set; }

  public EntityGroupViewModel(EntityGroup entityGroup)
  {
    Faction = entityGroup.Faction;
    GroupType = entityGroup.GroupType;
    Entities = entityGroup.Entities.Select(e => e.ToViewModel()).OfType<EntityViewModel>();
  }
}