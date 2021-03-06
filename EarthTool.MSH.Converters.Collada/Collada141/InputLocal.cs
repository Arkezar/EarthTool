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
    /// <para>The InputLocal type is used to represent inputs that can only reference resources declared in the same document.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("The InputLocal type is used to represent inputs that can only reference resources" +
        " declared in the same document.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("InputLocal", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class InputLocal
    {
        
        /// <summary>
        /// <para>The semantic attribute is the user-defined meaning of the input connection. Required attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The semantic attribute is the user-defined meaning of the input connection. Requi" +
            "red attribute.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("semantic")]
        public string Semantic { get; set; }
        
        /// <summary>
        /// <para>The source attribute indicates the location of the data source. Required attribute.</para>
        /// <para>This type is used for URI reference which can only reference a resource declared within it's same document.</para>
        /// <para xml:lang="en">Pattern: (#(.*)).</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The source attribute indicates the location of the data source. Required attribut" +
            "e.")]
        [System.ComponentModel.DataAnnotations.RegularExpressionAttribute("(#(.*))")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("source")]
        public string Source { get; set; }
    }
}
