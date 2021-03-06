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
    /// <para>The skin element contains vertex and primitive information sufficient to describe blend-weight skinning.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("The skin element contains vertex and primitive information sufficient to describe" +
        " blend-weight skinning.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("skin", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("skin", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Skin
    {
        
        /// <summary>
        /// <para>This provides extra information about the position and orientation of the base mesh before binding. 
        ///						If bind_shape_matrix is not specified then an identity matrix may be used as the bind_shape_matrix.
        ///						The bind_shape_matrix element may occur zero or one times.</para>
        /// <para xml:lang="en">Minimum length: 16.</para>
        /// <para xml:lang="en">Maximum length: 16.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute(@"This provides extra information about the position and orientation of the base mesh before binding. If bind_shape_matrix is not specified then an identity matrix may be used as the bind_shape_matrix. The bind_shape_matrix element may occur zero or one times.")]
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(16)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(16)]
        [System.Xml.Serialization.XmlElementAttribute("bind_shape_matrix")]
        public string Bind_Shape_Matrix { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Source> _source;
        
        /// <summary>
        /// <para>The skin element must contain at least three source elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The skin element must contain at least three source elements.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("source")]
        public System.Collections.ObjectModel.Collection<Source> Source
        {
            get
            {
                return this._source;
            }
            private set
            {
                this._source = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Skin" /> class.</para>
        /// </summary>
        public Skin()
        {
            this._source = new System.Collections.ObjectModel.Collection<Source>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        /// <summary>
        /// <para>The source attribute contains a URI reference to the base mesh, (a static mesh or a morphed mesh).
        ///					This also provides the bind-shape of the skinned mesh.  Required attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The source attribute contains a URI reference to the base mesh, (a static mesh or" +
            " a morphed mesh). This also provides the bind-shape of the skinned mesh. Require" +
            "d attribute.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("source")]
        public string Source_2 { get; set; }
        
        /// <summary>
        /// <para>The joints element associates joint, or skeleton, nodes with attribute data.  
        ///						In COLLADA, this is specified by the inverse bind matrix of each joint (influence) in the skeleton.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The joints element associates joint, or skeleton, nodes with attribute data. In C" +
            "OLLADA, this is specified by the inverse bind matrix of each joint (influence) i" +
            "n the skeleton.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("joints")]
        public SkinJoints Joints { get; set; }
        
        /// <summary>
        /// <para>The vertex_weights element associates a set of joint-weight pairs with each vertex in the base mesh.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The vertex_weights element associates a set of joint-weight pairs with each verte" +
            "x in the base mesh.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("vertex_weights")]
        public SkinVertex_Weights Vertex_Weights { get; set; }
        
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
