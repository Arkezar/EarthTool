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
    [System.Xml.Serialization.XmlTypeAttribute("Rigid_ConstraintTechnique_CommonLimitsLinear", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Rigid_ConstraintTechnique_CommonLimitsLinear
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private TargetableFloat3 _min = new Collada141.TargetableFloat3 { Value = "0.0 0.0 0.0" };
        
        /// <summary>
        /// <para>The minimum values for the limit.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The minimum values for the limit.")]
        [System.Xml.Serialization.XmlElementAttribute("min")]
        public TargetableFloat3 Min
        {
            get
            {
                return this._min;
            }
            set
            {
                this._min = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private TargetableFloat3 _max = new Collada141.TargetableFloat3 { Value = "0.0 0.0 0.0" };
        
        /// <summary>
        /// <para>The maximum values for the limit.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The maximum values for the limit.")]
        [System.Xml.Serialization.XmlElementAttribute("max")]
        public TargetableFloat3 Max
        {
            get
            {
                return this._max;
            }
            set
            {
                this._max = value;
            }
        }
    }
}
