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
    [System.Xml.Serialization.XmlTypeAttribute("fx_colortarget_common", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Fx_Colortarget_Common
    {
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets the text value.</para>
        /// </summary>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private string _index = "0";
        
        [System.ComponentModel.DefaultValueAttribute("0")]
        [System.Xml.Serialization.XmlAttributeAttribute("index")]
        public string Index
        {
            get
            {
                return this._index;
            }
            set
            {
                this._index = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private Fx_Surface_Face_Enum _face = Collada141.Fx_Surface_Face_Enum.POSITIVE_X;
        
        [System.ComponentModel.DefaultValueAttribute(Collada141.Fx_Surface_Face_Enum.POSITIVE_X)]
        [System.Xml.Serialization.XmlAttributeAttribute("face")]
        public Fx_Surface_Face_Enum Face
        {
            get
            {
                return this._face;
            }
            set
            {
                this._face = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private string _mip = "0";
        
        [System.ComponentModel.DefaultValueAttribute("0")]
        [System.Xml.Serialization.XmlAttributeAttribute("mip")]
        public string Mip
        {
            get
            {
                return this._mip;
            }
            set
            {
                this._mip = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private string _slice = "0";
        
        [System.ComponentModel.DefaultValueAttribute("0")]
        [System.Xml.Serialization.XmlAttributeAttribute("slice")]
        public string Slice
        {
            get
            {
                return this._slice;
            }
            set
            {
                this._slice = value;
            }
        }
    }
}
