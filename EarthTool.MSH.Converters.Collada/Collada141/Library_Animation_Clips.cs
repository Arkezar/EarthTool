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
    /// <para>The library_animation_clips element declares a module of animation_clip elements.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("The library_animation_clips element declares a module of animation_clip elements." +
        "")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("library_animation_clips", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("library_animation_clips", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Library_Animation_Clips
    {
        
        /// <summary>
        /// <para>The library_animation_clips element may contain an asset element.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The library_animation_clips element may contain an asset element.")]
        [System.Xml.Serialization.XmlElementAttribute("asset")]
        public Asset Asset { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Animation_Clip> _animation_Clip;
        
        /// <summary>
        /// <para>There must be at least one animation_clip element.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("There must be at least one animation_clip element.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("animation_clip")]
        public System.Collections.ObjectModel.Collection<Animation_Clip> Animation_Clip
        {
            get
            {
                return this._animation_Clip;
            }
            private set
            {
                this._animation_Clip = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Library_Animation_Clips" /> class.</para>
        /// </summary>
        public Library_Animation_Clips()
        {
            this._animation_Clip = new System.Collections.ObjectModel.Collection<Animation_Clip>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
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
        /// <para>The id attribute is a text string containing the unique identifier of this element. 
        ///					This value must be unique within the instance document. Optional attribute.</para>
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
    }
}
