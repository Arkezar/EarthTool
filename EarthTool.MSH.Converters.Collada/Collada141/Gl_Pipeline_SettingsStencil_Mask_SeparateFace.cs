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
    [System.Xml.Serialization.XmlTypeAttribute("Gl_Pipeline_SettingsStencil_Mask_SeparateFace", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Gl_Pipeline_SettingsStencil_Mask_SeparateFace
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private Gl_Face_Type _value = Collada141.Gl_Face_Type.FRONT_AND_BACK;
        
        [System.ComponentModel.DefaultValueAttribute(Collada141.Gl_Face_Type.FRONT_AND_BACK)]
        [System.Xml.Serialization.XmlAttributeAttribute("value")]
        public Gl_Face_Type Value
        {
            get
            {
                return this._value;
            }
            set
            {
                this._value = value;
            }
        }
        
        [System.Xml.Serialization.XmlAttributeAttribute("param")]
        public string Param { get; set; }
    }
}
