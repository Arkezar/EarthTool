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
    /// <para>The image element declares the storage for the graphical representation of an object. 
    ///			The image element best describes raster image data, but can conceivably handle other 
    ///			forms of imagery. The image elements allows for specifying an external image file with 
    ///			the init_from element or embed image data with the data element.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute(@"The image element declares the storage for the graphical representation of an object. The image element best describes raster image data, but can conceivably handle other forms of imagery. The image elements allows for specifying an external image file with the init_from element or embed image data with the data element.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("image", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("image", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Image
    {
        
        /// <summary>
        /// <para>The image element may contain an asset element.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The image element may contain an asset element.")]
        [System.Xml.Serialization.XmlElementAttribute("asset")]
        public Asset Asset { get; set; }
        
        /// <summary>
        /// <para>The data child element contains a sequence of hexadecimal encoded  binary octets representing 
        ///							the embedded image data.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The data child element contains a sequence of hexadecimal encoded binary octets r" +
            "epresenting the embedded image data.")]
        [System.Xml.Serialization.XmlElementAttribute("data")]
        public string Data { get; set; }
        
        /// <summary>
        /// <para>The init_from element allows you to specify an external image file to use for the image element.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The init_from element allows you to specify an external image file to use for the" +
            " image element.")]
        [System.Xml.Serialization.XmlElementAttribute("init_from")]
        public string Init_From { get; set; }
        
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
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Image" /> class.</para>
        /// </summary>
        public Image()
        {
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
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
        /// <para>The format attribute is a text string value that indicates the image format. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The format attribute is a text string value that indicates the image format. Opti" +
            "onal attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("format")]
        public string Format { get; set; }
        
        /// <summary>
        /// <para>The height attribute is an integer value that indicates the height of the image in pixel 
        ///					units. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The height attribute is an integer value that indicates the height of the image i" +
            "n pixel units. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("height")]
        public ulong Height { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Height property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool HeightSpecified { get; set; }
        
        /// <summary>
        /// <para>The width attribute is an integer value that indicates the width of the image in pixel units. 
        ///					Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The width attribute is an integer value that indicates the width of the image in " +
            "pixel units. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("width")]
        public ulong Width { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Width property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool WidthSpecified { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private ulong _depth = 1ul;
        
        /// <summary>
        /// <para>The depth attribute is an integer value that indicates the depth of the image in pixel units. 
        ///					A 2-D image has a depth of 1, which is also the default value. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1ul)]
        [System.ComponentModel.DescriptionAttribute("The depth attribute is an integer value that indicates the depth of the image in " +
            "pixel units. A 2-D image has a depth of 1, which is also the default value. Opti" +
            "onal attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("depth")]
        public ulong Depth
        {
            get
            {
                return this._depth;
            }
            set
            {
                this._depth = value;
            }
        }
    }
}
