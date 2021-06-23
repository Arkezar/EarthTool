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
    /// <para>The float_array element declares the storage for a homogenous array of floating point values.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("The float_array element declares the storage for a homogenous array of floating p" +
        "oint values.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("float_array", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("float_array", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Float_Array
    {
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets the text value.</para>
        /// </summary>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }
        
        /// <summary>
        /// <para>The id attribute is a text string containing the unique identifier of this element. This value 
        ///							must be unique within the instance document. Optional attribute.</para>
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
        /// <para>The count attribute indicates the number of values in the array. Required attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The count attribute indicates the number of values in the array. Required attribu" +
            "te.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("count")]
        public ulong Count { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private short _digits = 6;
        
        /// <summary>
        /// <para>The digits attribute indicates the number of significant decimal digits of the float values that 
        ///							can be contained in the array. The default value is 6. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(6)]
        [System.ComponentModel.DescriptionAttribute("The digits attribute indicates the number of significant decimal digits of the fl" +
            "oat values that can be contained in the array. The default value is 6. Optional " +
            "attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("digits")]
        public short Digits
        {
            get
            {
                return this._digits;
            }
            set
            {
                this._digits = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private short _magnitude = 38;
        
        /// <summary>
        /// <para>The magnitude attribute indicates the largest exponent of the float values that can be contained 
        ///							in the array. The default value is 38. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(38)]
        [System.ComponentModel.DescriptionAttribute("The magnitude attribute indicates the largest exponent of the float values that c" +
            "an be contained in the array. The default value is 38. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("magnitude")]
        public short Magnitude
        {
            get
            {
                return this._magnitude;
            }
            set
            {
                this._magnitude = value;
            }
        }
    }
}
