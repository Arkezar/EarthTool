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
    [System.Xml.Serialization.XmlTypeAttribute("common_color_or_texture_type", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Common_Transparent_Type))]
    public partial class Common_Color_Or_Texture_Type
    {
        
        [System.Xml.Serialization.XmlElementAttribute("color")]
        public Common_Color_Or_Texture_TypeColor Color { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("param")]
        public Common_Color_Or_Texture_TypeParam Param { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture")]
        public Common_Color_Or_Texture_TypeTexture Texture { get; set; }
    }
}
