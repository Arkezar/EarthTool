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
    [System.Xml.Serialization.XmlTypeAttribute("Rigid_ConstraintTechnique_CommonSpring", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Rigid_ConstraintTechnique_CommonSpring
    {
        
        /// <summary>
        /// <para>The angular spring properties.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The angular spring properties.")]
        [System.Xml.Serialization.XmlElementAttribute("angular")]
        public Rigid_ConstraintTechnique_CommonSpringAngular Angular { get; set; }
        
        /// <summary>
        /// <para>The linear spring properties.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The linear spring properties.")]
        [System.Xml.Serialization.XmlElementAttribute("linear")]
        public Rigid_ConstraintTechnique_CommonSpringLinear Linear { get; set; }
    }
}
