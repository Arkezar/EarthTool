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
    /// <para>A three-dimensional texture sampler.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("A three-dimensional texture sampler.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("fx_sampler3D_common", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Cg_Sampler3D))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Gl_Sampler3D))]
    public partial class Fx_Sampler3D_Common
    {
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("source")]
        public string Source { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private Fx_Sampler_Wrap_Common _wrap_S = Collada141.Fx_Sampler_Wrap_Common.WRAP;
        
        [System.ComponentModel.DefaultValueAttribute(Collada141.Fx_Sampler_Wrap_Common.WRAP)]
        [System.Xml.Serialization.XmlElementAttribute("wrap_s")]
        public Fx_Sampler_Wrap_Common Wrap_S
        {
            get
            {
                return this._wrap_S;
            }
            set
            {
                this._wrap_S = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private Fx_Sampler_Wrap_Common _wrap_T = Collada141.Fx_Sampler_Wrap_Common.WRAP;
        
        [System.ComponentModel.DefaultValueAttribute(Collada141.Fx_Sampler_Wrap_Common.WRAP)]
        [System.Xml.Serialization.XmlElementAttribute("wrap_t")]
        public Fx_Sampler_Wrap_Common Wrap_T
        {
            get
            {
                return this._wrap_T;
            }
            set
            {
                this._wrap_T = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private Fx_Sampler_Wrap_Common _wrap_P = Collada141.Fx_Sampler_Wrap_Common.WRAP;
        
        [System.ComponentModel.DefaultValueAttribute(Collada141.Fx_Sampler_Wrap_Common.WRAP)]
        [System.Xml.Serialization.XmlElementAttribute("wrap_p")]
        public Fx_Sampler_Wrap_Common Wrap_P
        {
            get
            {
                return this._wrap_P;
            }
            set
            {
                this._wrap_P = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private Fx_Sampler_Filter_Common _minfilter = Collada141.Fx_Sampler_Filter_Common.NONE;
        
        [System.ComponentModel.DefaultValueAttribute(Collada141.Fx_Sampler_Filter_Common.NONE)]
        [System.Xml.Serialization.XmlElementAttribute("minfilter")]
        public Fx_Sampler_Filter_Common Minfilter
        {
            get
            {
                return this._minfilter;
            }
            set
            {
                this._minfilter = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private Fx_Sampler_Filter_Common _magfilter = Collada141.Fx_Sampler_Filter_Common.NONE;
        
        [System.ComponentModel.DefaultValueAttribute(Collada141.Fx_Sampler_Filter_Common.NONE)]
        [System.Xml.Serialization.XmlElementAttribute("magfilter")]
        public Fx_Sampler_Filter_Common Magfilter
        {
            get
            {
                return this._magfilter;
            }
            set
            {
                this._magfilter = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private Fx_Sampler_Filter_Common _mipfilter = Collada141.Fx_Sampler_Filter_Common.NONE;
        
        [System.ComponentModel.DefaultValueAttribute(Collada141.Fx_Sampler_Filter_Common.NONE)]
        [System.Xml.Serialization.XmlElementAttribute("mipfilter")]
        public Fx_Sampler_Filter_Common Mipfilter
        {
            get
            {
                return this._mipfilter;
            }
            set
            {
                this._mipfilter = value;
            }
        }
        
        [System.Xml.Serialization.XmlElementAttribute("border_color")]
        public string Border_Color { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private byte _mipmap_Maxlevel = 255;
        
        [System.ComponentModel.DefaultValueAttribute(255)]
        [System.Xml.Serialization.XmlElementAttribute("mipmap_maxlevel")]
        public byte Mipmap_Maxlevel
        {
            get
            {
                return this._mipmap_Maxlevel;
            }
            set
            {
                this._mipmap_Maxlevel = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private float _mipmap_Bias = 0F;
        
        [System.ComponentModel.DefaultValueAttribute(0F)]
        [System.Xml.Serialization.XmlElementAttribute("mipmap_bias")]
        public float Mipmap_Bias
        {
            get
            {
                return this._mipmap_Bias;
            }
            set
            {
                this._mipmap_Bias = value;
            }
        }
        
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
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Fx_Sampler3D_Common" /> class.</para>
        /// </summary>
        public Fx_Sampler3D_Common()
        {
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
    }
}
