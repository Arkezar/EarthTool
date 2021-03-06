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
    /// <para>The polygons element provides the information needed to bind vertex attributes together and 
    ///			then organize those vertices into individual polygons. The polygons described can contain 
    ///			arbitrary numbers of vertices. These polygons may be self intersecting and may also contain holes.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute(@"The polygons element provides the information needed to bind vertex attributes together and then organize those vertices into individual polygons. The polygons described can contain arbitrary numbers of vertices. These polygons may be self intersecting and may also contain holes.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("polygons", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("polygons", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Polygons
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
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Polygons" /> class.</para>
        /// </summary>
        public Polygons()
        {
            this._input = new System.Collections.ObjectModel.Collection<InputLocalOffset>();
            this._p = new System.Collections.ObjectModel.Collection<string>();
            this._ph = new System.Collections.ObjectModel.Collection<PolygonsPh>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<string> _p;
        
        /// <summary>
        /// <para>The p element may occur any number of times.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The p element may occur any number of times.")]
        [System.Xml.Serialization.XmlElementAttribute("p")]
        public System.Collections.ObjectModel.Collection<string> P
        {
            get
            {
                return this._p;
            }
            private set
            {
                this._p = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the P collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PSpecified
        {
            get
            {
                return (this.P.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<PolygonsPh> _ph;
        
        /// <summary>
        /// <para>The ph element descripes a polygon with holes.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The ph element descripes a polygon with holes.")]
        [System.Xml.Serialization.XmlElementAttribute("ph")]
        public System.Collections.ObjectModel.Collection<PolygonsPh> Ph
        {
            get
            {
                return this._ph;
            }
            private set
            {
                this._ph = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Ph collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PhSpecified
        {
            get
            {
                return (this.Ph.Count != 0);
            }
        }
        
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
        /// <para>The count attribute indicates the number of polygon primitives. Required attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The count attribute indicates the number of polygon primitives. Required attribut" +
            "e.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("count")]
        public ulong Count { get; set; }
        
        /// <summary>
        /// <para>The material attribute declares a symbol for a material. This symbol is bound to a material 
        ///					at the time of instantiation. If the material attribute is not specified then the lighting 
        ///					and shading results are application defined. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The material attribute declares a symbol for a material. This symbol is bound to " +
            "a material at the time of instantiation. If the material attribute is not specif" +
            "ied then the lighting and shading results are application defined. Optional attr" +
            "ibute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("material")]
        public string Material { get; set; }
    }
}
