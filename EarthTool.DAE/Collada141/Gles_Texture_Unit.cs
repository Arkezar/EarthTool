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
    [System.Xml.Serialization.XmlTypeAttribute("gles_texture_unit", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Gles_Texture_Unit
    {
        
        [System.Xml.Serialization.XmlElementAttribute("surface")]
        public string Surface { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sampler_state")]
        public string Sampler_State { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texcoord")]
        public Gles_Texture_UnitTexcoord Texcoord { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Extra> _extra;
        
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
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Gles_Texture_Unit" /> class.</para>
        /// </summary>
        public Gles_Texture_Unit()
        {
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        /// <summary>
        /// <para>The sid attribute is a text string value containing the sub-identifier of this element. 
        ///				This value must be unique within the scope of the parent element. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The sid attribute is a text string value containing the sub-identifier of this el" +
            "ement. This value must be unique within the scope of the parent element. Optiona" +
            "l attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("sid")]
        public string Sid { get; set; }
    }
}