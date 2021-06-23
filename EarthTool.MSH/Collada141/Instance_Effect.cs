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
    /// <para>The instance_effect element declares the instantiation of a COLLADA effect resource.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("The instance_effect element declares the instantiation of a COLLADA effect resour" +
        "ce.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("instance_effect", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("instance_effect", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Instance_Effect
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Instance_EffectTechnique_Hint> _technique_Hint;
        
        /// <summary>
        /// <para>Add a hint for a platform of which technique to use in this effect.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Add a hint for a platform of which technique to use in this effect.")]
        [System.Xml.Serialization.XmlElementAttribute("technique_hint")]
        public System.Collections.ObjectModel.Collection<Instance_EffectTechnique_Hint> Technique_Hint
        {
            get
            {
                return this._technique_Hint;
            }
            private set
            {
                this._technique_Hint = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Technique_Hint collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Technique_HintSpecified
        {
            get
            {
                return (this.Technique_Hint.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Instance_Effect" /> class.</para>
        /// </summary>
        public Instance_Effect()
        {
            this._technique_Hint = new System.Collections.ObjectModel.Collection<Instance_EffectTechnique_Hint>();
            this._setparam = new System.Collections.ObjectModel.Collection<Instance_EffectSetparam>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Instance_EffectSetparam> _setparam;
        
        /// <summary>
        /// <para>Assigns a new value to a previously defined parameter</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Assigns a new value to a previously defined parameter")]
        [System.Xml.Serialization.XmlElementAttribute("setparam")]
        public System.Collections.ObjectModel.Collection<Instance_EffectSetparam> Setparam
        {
            get
            {
                return this._setparam;
            }
            private set
            {
                this._setparam = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Setparam collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SetparamSpecified
        {
            get
            {
                return (this.Setparam.Count != 0);
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
        /// <para>The url attribute refers to resource.  This may refer to a local resource using a relative URL 
        ///					fragment identifier that begins with the “#” character. The url attribute may refer to an external 
        ///					resource using an absolute or relative URL.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The url attribute refers to resource. This may refer to a local resource using a " +
            "relative URL fragment identifier that begins with the “#” character. The url att" +
            "ribute may refer to an external resource using an absolute or relative URL.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("url")]
        public string Url { get; set; }
        
        /// <summary>
        /// <para>The sid attribute is a text string value containing the sub-identifier of this element. This 
        ///					value must be unique within the scope of the parent element. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The sid attribute is a text string value containing the sub-identifier of this el" +
            "ement. This value must be unique within the scope of the parent element. Optiona" +
            "l attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("sid")]
        public string Sid { get; set; }
        
        /// <summary>
        /// <para>The name attribute is the text string name of this element. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The name attribute is the text string name of this element. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get; set; }
    }
}
