using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EarthTool.PAR.Models;
using EarthTool.PAR.Models.Abstracts;

namespace EarthTool.PAR.GUI.TemplateSelectors
{
    public class EntityTemplateSelector : IDataTemplate
    {
        public IDataTemplate? VehicleTemplate { get; set; }
        public IDataTemplate? BuildingTemplate { get; set; }
        public IDataTemplate? WeaponTemplate { get; set; }
        public IDataTemplate? EquipmentTemplate { get; set; }
        public IDataTemplate? ArtifactTemplate { get; set; }
        public IDataTemplate? MissileTemplate { get; set; }
        public IDataTemplate? MineTemplate { get; set; }
        public IDataTemplate? ParameterTemplate { get; set; }
        public IDataTemplate? DefaultTemplate { get; set; }

        public Control? Build(object? param)
        {
            if (param == null) return null;

            var template = param switch
            {
                Vehicle => VehicleTemplate,
                Building => BuildingTemplate,
                Weapon => WeaponTemplate,
                Equipment => EquipmentTemplate,
                Artifact => ArtifactTemplate,
                Missile => MissileTemplate,
                Mine => MineTemplate,
                Parameter => ParameterTemplate,
                _ => DefaultTemplate
            };

            return template?.Build(param);
        }

        public bool Match(object? data) => data is Entity;
    }
}
