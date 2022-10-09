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
    /// <para>A tapered cylinder primitive that is centered on and aligned with the local Y axis.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("A tapered cylinder primitive that is centered on and aligned with the local Y axi" +
        "s.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("tapered_cylinder", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("tapered_cylinder", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Tapered_Cylinder
    {
        
        /// <summary>
        /// <para>A float value that represents the length of the cylinder along the Y axis.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("A float value that represents the length of the cylinder along the Y axis.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("height")]
        public double Height { get; set; }
        
        /// <summary>
        /// <para>Two float values that represent the radii of the tapered cylinder at the positive (height/2) 
        ///						Y value. Both ends of the tapered cylinder may be elliptical.</para>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Two float values that represent the radii of the tapered cylinder at the positive" +
            " (height/2) Y value. Both ends of the tapered cylinder may be elliptical.")]
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("radius1")]
        public string Radius1 { get; set; }
        
        /// <summary>
        /// <para>Two float values that represent the radii of the tapered cylinder at the negative (height/2) 
        ///						Y value.Both ends of the tapered cylinder may be elliptical.</para>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Two float values that represent the radii of the tapered cylinder at the negative" +
            " (height/2) Y value.Both ends of the tapered cylinder may be elliptical.")]
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("radius2")]
        public string Radius2 { get; set; }
        
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
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Tapered_Cylinder" /> class.</para>
        /// </summary>
        public Tapered_Cylinder()
        {
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
    }
}