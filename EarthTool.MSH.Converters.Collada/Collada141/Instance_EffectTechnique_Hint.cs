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
    [System.Xml.Serialization.XmlTypeAttribute("Instance_EffectTechnique_Hint", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Instance_EffectTechnique_Hint
    {
        
        /// <summary>
        /// <para>A platform defines a string that specifies which platform this is hint is aimed for.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("A platform defines a string that specifies which platform this is hint is aimed f" +
            "or.")]
        [System.Xml.Serialization.XmlAttributeAttribute("platform")]
        public string Platform { get; set; }
        
        /// <summary>
        /// <para>A profile defines a string that specifies which API profile this is hint is aimed for.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("A profile defines a string that specifies which API profile this is hint is aimed" +
            " for.")]
        [System.Xml.Serialization.XmlAttributeAttribute("profile")]
        public string Profile { get; set; }
        
        /// <summary>
        /// <para>A reference to the technique to use for the specified platform.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("A reference to the technique to use for the specified platform.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public string Ref { get; set; }
    }
}