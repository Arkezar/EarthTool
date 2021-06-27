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
    [System.Xml.Serialization.XmlTypeAttribute("Instance_Rigid_BodyTechnique_CommonShape", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Instance_Rigid_BodyTechnique_CommonShape
    {
        
        [System.Xml.Serialization.XmlElementAttribute("hollow")]
        public Instance_Rigid_BodyTechnique_CommonShapeHollow Hollow { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("mass")]
        public TargetableFloat Mass { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("density")]
        public TargetableFloat Density { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("instance_physics_material")]
        public InstanceWithExtra Instance_Physics_Material { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("physics_material")]
        public Physics_Material Physics_Material { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("instance_geometry")]
        public Instance_Geometry Instance_Geometry { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("plane")]
        public Plane Plane { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("box")]
        public Box Box { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sphere")]
        public Sphere Sphere { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("cylinder")]
        public Cylinder Cylinder { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("tapered_cylinder")]
        public Tapered_Cylinder Tapered_Cylinder { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("capsule")]
        public Capsule Capsule { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("tapered_capsule")]
        public Tapered_Capsule Tapered_Capsule { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<TargetableFloat3> _translate;
        
        [System.Xml.Serialization.XmlElementAttribute("translate")]
        public System.Collections.ObjectModel.Collection<TargetableFloat3> Translate
        {
            get
            {
                return this._translate;
            }
            private set
            {
                this._translate = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Translate collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TranslateSpecified
        {
            get
            {
                return (this.Translate.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Instance_Rigid_BodyTechnique_CommonShape" /> class.</para>
        /// </summary>
        public Instance_Rigid_BodyTechnique_CommonShape()
        {
            this._translate = new System.Collections.ObjectModel.Collection<TargetableFloat3>();
            this._rotate = new System.Collections.ObjectModel.Collection<Rotate>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Rotate> _rotate;
        
        [System.Xml.Serialization.XmlElementAttribute("rotate")]
        public System.Collections.ObjectModel.Collection<Rotate> Rotate
        {
            get
            {
                return this._rotate;
            }
            private set
            {
                this._rotate = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Rotate collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool RotateSpecified
        {
            get
            {
                return (this.Rotate.Count != 0);
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