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
    /// <para>If the exact format cannot be resolve via other methods then the format_hint will describe the important features of the format so that the application may select a compatable or close format</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("If the exact format cannot be resolve via other methods then the format_hint will" +
        " describe the important features of the format so that the application may selec" +
        "t a compatable or close format")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("fx_surface_format_hint_common", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Fx_Surface_Format_Hint_Common
    {
        
        /// <summary>
        /// <para>The per-texel layout of the format.  The length of the string indicate how many channels there are and the letter respresents the name of the channel.  There are typically 0 to 4 channels.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The per-texel layout of the format. The length of the string indicate how many ch" +
            "annels there are and the letter respresents the name of the channel. There are t" +
            "ypically 0 to 4 channels.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("channels")]
        public Fx_Surface_Format_Hint_Channels_Enum Channels { get; set; }
        
        /// <summary>
        /// <para>Each channel represents a range of values. Some example ranges are signed or unsigned integers, or between between a clamped range such as 0.0f to 1.0f, or high dynamic range via floating point</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Each channel represents a range of values. Some example ranges are signed or unsi" +
            "gned integers, or between between a clamped range such as 0.0f to 1.0f, or high " +
            "dynamic range via floating point")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("range")]
        public Fx_Surface_Format_Hint_Range_Enum Range { get; set; }
        
        /// <summary>
        /// <para>Each channel of the texel has a precision.  Typically these are all linked together.  An exact format lay lower the precision of an individual channel but applying a higher precision by linking the channels together may still convey the same information.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Each channel of the texel has a precision. Typically these are all linked togethe" +
            "r. An exact format lay lower the precision of an individual channel but applying" +
            " a higher precision by linking the channels together may still convey the same i" +
            "nformation.")]
        [System.Xml.Serialization.XmlElementAttribute("precision")]
        public Fx_Surface_Format_Hint_Precision_Enum Precision { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Precision property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PrecisionSpecified { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Surface_Format_Hint_Option_Enum> _option;
        
        /// <summary>
        /// <para>Additional hints about data relationships and other things to help the application pick the best format.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Additional hints about data relationships and other things to help the applicatio" +
            "n pick the best format.")]
        [System.Xml.Serialization.XmlElementAttribute("option")]
        public System.Collections.ObjectModel.Collection<Fx_Surface_Format_Hint_Option_Enum> Option
        {
            get
            {
                return this._option;
            }
            private set
            {
                this._option = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Option collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool OptionSpecified
        {
            get
            {
                return (this.Option.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Fx_Surface_Format_Hint_Common" /> class.</para>
        /// </summary>
        public Fx_Surface_Format_Hint_Common()
        {
            this._option = new System.Collections.ObjectModel.Collection<Fx_Surface_Format_Hint_Option_Enum>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
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
    }
}