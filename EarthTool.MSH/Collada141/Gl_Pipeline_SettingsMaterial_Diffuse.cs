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
    [System.Xml.Serialization.XmlTypeAttribute("Gl_Pipeline_SettingsMaterial_Diffuse", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Gl_Pipeline_SettingsMaterial_Diffuse
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<double> _value;
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        [System.Xml.Serialization.XmlAttributeAttribute("value")]
        public System.Collections.ObjectModel.Collection<double> Value
        {
            get
            {
                return this._value;
            }
            private set
            {
                this._value = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Value collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ValueSpecified
        {
            get
            {
                return (this.Value.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Gl_Pipeline_SettingsMaterial_Diffuse" /> class.</para>
        /// </summary>
        public Gl_Pipeline_SettingsMaterial_Diffuse()
        {
            this._value = new System.Collections.ObjectModel.Collection<double>();
        }
        
        [System.Xml.Serialization.XmlAttributeAttribute("param")]
        public string Param { get; set; }
    }
}
