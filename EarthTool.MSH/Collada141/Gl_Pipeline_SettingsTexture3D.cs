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
    [System.Xml.Serialization.XmlTypeAttribute("Gl_Pipeline_SettingsTexture3D", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Gl_Pipeline_SettingsTexture3D
    {
        
        [System.Xml.Serialization.XmlElementAttribute("value")]
        public Gl_Sampler3D Value { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("param")]
        public string Param { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum inclusive value: 0.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("index")]
        public string Index { get; set; }
    }
}
