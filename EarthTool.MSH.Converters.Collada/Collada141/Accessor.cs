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
    /// <para>The accessor element declares an access pattern to one of the array elements: float_array, 
    ///			int_array, Name_array, bool_array, and IDREF_array. The accessor element describes access 
    ///			to arrays that are organized in either an interleaved or non-interleaved manner, depending 
    ///			on the offset and stride attributes.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute(@"The accessor element declares an access pattern to one of the array elements: float_array, int_array, Name_array, bool_array, and IDREF_array. The accessor element describes access to arrays that are organized in either an interleaved or non-interleaved manner, depending on the offset and stride attributes.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("accessor", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("accessor", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Accessor
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Param> _param;
        
        /// <summary>
        /// <para>The accessor element may have any number of param elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The accessor element may have any number of param elements.")]
        [System.Xml.Serialization.XmlElementAttribute("param")]
        public System.Collections.ObjectModel.Collection<Param> Param
        {
            get
            {
                return this._param;
            }
            private set
            {
                this._param = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Param collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ParamSpecified
        {
            get
            {
                return (this.Param.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Accessor" /> class.</para>
        /// </summary>
        public Accessor()
        {
            this._param = new System.Collections.ObjectModel.Collection<Param>();
        }
        
        /// <summary>
        /// <para>The count attribute indicates the number of times the array is accessed. Required attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The count attribute indicates the number of times the array is accessed. Required" +
            " attribute.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("count")]
        public ulong Count { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private ulong _offset = 0ul;
        
        /// <summary>
        /// <para>The offset attribute indicates the index of the first value to be read from the array. 
        ///					The default value is 0. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0ul)]
        [System.ComponentModel.DescriptionAttribute("The offset attribute indicates the index of the first value to be read from the a" +
            "rray. The default value is 0. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("offset")]
        public ulong Offset
        {
            get
            {
                return this._offset;
            }
            set
            {
                this._offset = value;
            }
        }
        
        /// <summary>
        /// <para>The source attribute indicates the location of the array to access using a URL expression. Required attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The source attribute indicates the location of the array to access using a URL ex" +
            "pression. Required attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("source")]
        public string Source { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private ulong _stride = 1ul;
        
        /// <summary>
        /// <para>The stride attribute indicates number of values to be considered a unit during each access to 
        ///					the array. The default value is 1, indicating that a single value is accessed. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1ul)]
        [System.ComponentModel.DescriptionAttribute("The stride attribute indicates number of values to be considered a unit during ea" +
            "ch access to the array. The default value is 1, indicating that a single value i" +
            "s accessed. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("stride")]
        public ulong Stride
        {
            get
            {
                return this._stride;
            }
            set
            {
                this._stride = value;
            }
        }
    }
}