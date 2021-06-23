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
    /// <para>Matrix transformations embody mathematical changes to points within a coordinate systems or the 
    ///			coordinate system itself. The matrix element contains a 4-by-4 matrix of floating-point values.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("Matrix transformations embody mathematical changes to points within a coordinate " +
        "systems or the coordinate system itself. The matrix element contains a 4-by-4 ma" +
        "trix of floating-point values.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("matrix", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("matrix", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Matrix
    {
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets the text value.</para>
        /// <para xml:lang="en">Minimum length: 16.</para>
        /// <para xml:lang="en">Maximum length: 16.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(16)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(16)]
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }
        
        /// <summary>
        /// <para>The sid attribute is a text string value containing the sub-identifier of this element. 
        ///							This value must be unique within the scope of the parent element. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The sid attribute is a text string value containing the sub-identifier of this el" +
            "ement. This value must be unique within the scope of the parent element. Optiona" +
            "l attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("sid")]
        public string Sid { get; set; }
    }
}
