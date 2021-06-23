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
    [System.Xml.Serialization.XmlTypeAttribute("common_newparam_type", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Common_Newparam_Type
    {
        
        [System.Xml.Serialization.XmlElementAttribute("semantic")]
        public string Semantic { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("float")]
        public double Float { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Float property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool FloatSpecified { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        [System.Xml.Serialization.XmlElementAttribute("float2")]
        public string Float2 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        [System.Xml.Serialization.XmlElementAttribute("float3")]
        public string Float3 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        [System.Xml.Serialization.XmlElementAttribute("float4")]
        public string Float4 { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("surface")]
        public Fx_Surface_Common Surface { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sampler2D")]
        public Fx_Sampler2D_Common Sampler2D { get; set; }
        
        /// <summary>
        /// <para>The sid attribute is a text string value containing the sub-identifier of this element. 
        ///				This value must be unique within the scope of the parent element. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The sid attribute is a text string value containing the sub-identifier of this el" +
            "ement. This value must be unique within the scope of the parent element. Optiona" +
            "l attribute.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("sid")]
        public string Sid { get; set; }
    }
}
