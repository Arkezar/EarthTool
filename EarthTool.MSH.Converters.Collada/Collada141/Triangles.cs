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
    /// <para>The triangles element provides the information needed to bind vertex attributes together and 
    ///			then organize those vertices into individual triangles.	Each triangle described by the mesh has 
    ///			three vertices. The first triangle is formed from the first, second, and third vertices. The 
    ///			second triangle is formed from the fourth, fifth, and sixth vertices, and so on.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute(@"The triangles element provides the information needed to bind vertex attributes together and then organize those vertices into individual triangles. Each triangle described by the mesh has three vertices. The first triangle is formed from the first, second, and third vertices. The second triangle is formed from the fourth, fifth, and sixth vertices, and so on.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("triangles", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("triangles", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Triangles
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<InputLocalOffset> _input;
        
        /// <summary>
        /// <para>The input element may occur any number of times. This input is a local input with the 
        ///						offset and set attributes.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The input element may occur any number of times. This input is a local input with" +
            " the offset and set attributes.")]
        [System.Xml.Serialization.XmlElementAttribute("input")]
        public System.Collections.ObjectModel.Collection<InputLocalOffset> Input
        {
            get
            {
                return this._input;
            }
            private set
            {
                this._input = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Input collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool InputSpecified
        {
            get
            {
                return (this.Input.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Triangles" /> class.</para>
        /// </summary>
        public Triangles()
        {
            this._input = new System.Collections.ObjectModel.Collection<InputLocalOffset>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        /// <summary>
        /// <para>The triangles element may have any number of p elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The triangles element may have any number of p elements.")]
        [System.Xml.Serialization.XmlElementAttribute("p")]
        public string P { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Extra> _extra;
        
        /// <summary>
        /// <para>The extra element may appear any number of times.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The extra element may appear any number of times.")]
        [System.Xml.Serialization.XmlElementAttribute("extra")]
        public System.Collections.ObjectModel.Collection<Extra> Extra
        {
            get
            {
                return this._extra;
            }
            private set
            {
                this._extra = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Extra collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ExtraSpecified
        {
            get
            {
                return (this.Extra.Count != 0);
            }
        }
        
        /// <summary>
        /// <para>The name attribute is the text string name of this element. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The name attribute is the text string name of this element. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// <para>The count attribute indicates the number of triangle primitives. Required attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The count attribute indicates the number of triangle primitives. Required attribu" +
            "te.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("count")]
        public ulong Count { get; set; }
        
        /// <summary>
        /// <para>The material attribute declares a symbol for a material. This symbol is bound to a material at 
        ///					the time of instantiation. Optional attribute. If the material attribute is not specified then 
        ///					the lighting and shading results are application defined.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The material attribute declares a symbol for a material. This symbol is bound to " +
            "a material at the time of instantiation. Optional attribute. If the material att" +
            "ribute is not specified then the lighting and shading results are application de" +
            "fined.")]
        [System.Xml.Serialization.XmlAttributeAttribute("material")]
        public string Material { get; set; }
    }
}
