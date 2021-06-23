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
    /// <para>The extra element declares additional information regarding its parent element.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("The extra element declares additional information regarding its parent element.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("extra", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("extra", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Extra
    {
        
        /// <summary>
        /// <para>The extra element may contain an asset element.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The extra element may contain an asset element.")]
        [System.Xml.Serialization.XmlElementAttribute("asset")]
        public Asset Asset { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Technique> _technique;
        
        /// <summary>
        /// <para>This element must contain at least one non-common profile technique.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("This element must contain at least one non-common profile technique.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("technique")]
        public System.Collections.ObjectModel.Collection<Technique> Technique
        {
            get
            {
                return this._technique;
            }
            private set
            {
                this._technique = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Extra" /> class.</para>
        /// </summary>
        public Extra()
        {
            this._technique = new System.Collections.ObjectModel.Collection<Technique>();
        }
        
        /// <summary>
        /// <para>The id attribute is a text string containing the unique identifier of this element. This value 
        ///					must be unique within the instance document. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The id attribute is a text string containing the unique identifier of this elemen" +
            "t. This value must be unique within the instance document. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("id")]
        public string Id { get; set; }
        
        /// <summary>
        /// <para>The name attribute is the text string name of this element. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The name attribute is the text string name of this element. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// <para>The type attribute indicates the type of the value data. This text string must be understood by 
        ///					the application. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The type attribute indicates the type of the value data. This text string must be" +
            " understood by the application. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public string Type { get; set; }
    }
}