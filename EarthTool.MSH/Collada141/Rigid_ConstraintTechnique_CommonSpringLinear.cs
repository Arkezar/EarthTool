//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// This code was generated by XmlSchemaClassGenerator version 2.0.560.0 using the following command:
// xscgen -o Collada141 --nc=true --sf .\collada_schema_1_4_1_ms.xsd
namespace Collada141
{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("Rigid_ConstraintTechnique_CommonSpringLinear", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Rigid_ConstraintTechnique_CommonSpringLinear
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private TargetableFloat _stiffness = new Collada141.TargetableFloat { Value = 1D };
        
        /// <summary>
        /// <para>The stiffness (also called spring coefficient) has units of force/distance.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The stiffness (also called spring coefficient) has units of force/distance.")]
        [System.Xml.Serialization.XmlElementAttribute("stiffness")]
        public TargetableFloat Stiffness
        {
            get
            {
                return this._stiffness;
            }
            set
            {
                this._stiffness = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private TargetableFloat _damping = new Collada141.TargetableFloat { Value = 0D };
        
        /// <summary>
        /// <para>The spring damping coefficient.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The spring damping coefficient.")]
        [System.Xml.Serialization.XmlElementAttribute("damping")]
        public TargetableFloat Damping
        {
            get
            {
                return this._damping;
            }
            set
            {
                this._damping = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private TargetableFloat _target_Value = new Collada141.TargetableFloat { Value = 0D };
        
        /// <summary>
        /// <para>The spring's target or resting distance.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The spring\'s target or resting distance.")]
        [System.Xml.Serialization.XmlElementAttribute("target_value")]
        public TargetableFloat Target_Value
        {
            get
            {
                return this._target_Value;
            }
            set
            {
                this._target_Value = value;
            }
        }
    }
}
