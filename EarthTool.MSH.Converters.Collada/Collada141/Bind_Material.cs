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
    /// <para>Bind a specific material to a piece of geometry, binding varying and uniform parameters at the 
    ///			same time.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("Bind a specific material to a piece of geometry, binding varying and uniform para" +
        "meters at the same time.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("bind_material", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("bind_material", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Bind_Material
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Param> _param;
        
        /// <summary>
        /// <para>The bind_material element may contain any number of param elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The bind_material element may contain any number of param elements.")]
        [System.Xml.Serialization.XmlElementAttribute("param")]
        public System.Collections.ObjectModel.Collection<Param> Param
        {
            get
            {
                return this._param;
            }
            private set
            {
                this._param = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Param collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ParamSpecified
        {
            get
            {
                return (this.Param.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Bind_Material" /> class.</para>
        /// </summary>
        public Bind_Material()
        {
            this._param = new System.Collections.ObjectModel.Collection<Param>();
            this._technique_Common = new System.Collections.ObjectModel.Collection<Instance_Material>();
            this._technique = new System.Collections.ObjectModel.Collection<Technique>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Instance_Material> _technique_Common;
        
        /// <summary>
        /// <para>The technique_common element specifies the bind_material information for the common 
        ///						profile which all COLLADA implementations need to support.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The technique_common element specifies the bind_material information for the comm" +
            "on profile which all COLLADA implementations need to support.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlArrayAttribute("technique_common")]
        [System.Xml.Serialization.XmlArrayItemAttribute("instance_material", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
        public System.Collections.ObjectModel.Collection<Instance_Material> Technique_Common
        {
            get
            {
                return this._technique_Common;
            }
            private set
            {
                this._technique_Common = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Technique> _technique;
        
        /// <summary>
        /// <para>This element may contain any number of non-common profile techniques.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("This element may contain any number of non-common profile techniques.")]
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
        /// <para xml:lang="en">Gets a value indicating whether the Technique collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TechniqueSpecified
        {
            get
            {
                return (this.Technique.Count != 0);
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
    }
}
