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
    
    
    /// <summary>
    /// <para>A surface type for the GLSL profile. This surface inherits from the fx_surface_common type and adds the
    ///			ability to programmatically generate textures.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("A surface type for the GLSL profile. This surface inherits from the fx_surface_co" +
        "mmon type and adds the ability to programmatically generate textures.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("glsl_surface_type", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Glsl_Surface_Type : Fx_Surface_Common
    {
        
        /// <summary>
        /// <para>A procedural surface generator.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("A procedural surface generator.")]
        [System.Xml.Serialization.XmlElementAttribute("generator")]
        public Glsl_Surface_TypeGenerator Generator { get; set; }
    }
}
