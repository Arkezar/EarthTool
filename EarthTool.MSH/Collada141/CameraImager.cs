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
    [System.Xml.Serialization.XmlTypeAttribute("CameraImager", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class CameraImager
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Technique> _technique;
        
        /// <summary>
        /// <para>This element may contain any number of non-common profile techniques.
        ///									There is no common technique for imager.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("This element may contain any number of non-common profile techniques. There is no" +
            " common technique for imager.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("technique")]
        public System.Collections.ObjectModel.Collection<Technique> Technique
        {
            get
            {
                return this._technique;
            }
            private set
            {
                this._technique = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="CameraImager" /> class.</para>
        /// </summary>
        public CameraImager()
        {
            this._technique = new System.Collections.ObjectModel.Collection<Technique>();
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
    }
}
