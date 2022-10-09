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
    /// <para>This element allows for building complex combinations of rigid-bodies and constraints that 
    ///			may be instantiated multiple times.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("This element allows for building complex combinations of rigid-bodies and constra" +
        "ints that may be instantiated multiple times.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("physics_model", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("physics_model", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Physics_Model
    {
        
        /// <summary>
        /// <para>The physics_model element may contain an asset element.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The physics_model element may contain an asset element.")]
        [System.Xml.Serialization.XmlElementAttribute("asset")]
        public Asset Asset { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Rigid_Body> _rigid_Body;
        
        /// <summary>
        /// <para>The physics_model may define any number of rigid_body elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The physics_model may define any number of rigid_body elements.")]
        [System.Xml.Serialization.XmlElementAttribute("rigid_body")]
        public System.Collections.ObjectModel.Collection<Rigid_Body> Rigid_Body
        {
            get
            {
                return this._rigid_Body;
            }
            private set
            {
                this._rigid_Body = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Rigid_Body collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Rigid_BodySpecified
        {
            get
            {
                return (this.Rigid_Body.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Physics_Model" /> class.</para>
        /// </summary>
        public Physics_Model()
        {
            this._rigid_Body = new System.Collections.ObjectModel.Collection<Rigid_Body>();
            this._rigid_Constraint = new System.Collections.ObjectModel.Collection<Rigid_Constraint>();
            this._instance_Physics_Model = new System.Collections.ObjectModel.Collection<Instance_Physics_Model>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Rigid_Constraint> _rigid_Constraint;
        
        /// <summary>
        /// <para>The physics_model may define any number of rigid_constraint elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The physics_model may define any number of rigid_constraint elements.")]
        [System.Xml.Serialization.XmlElementAttribute("rigid_constraint")]
        public System.Collections.ObjectModel.Collection<Rigid_Constraint> Rigid_Constraint
        {
            get
            {
                return this._rigid_Constraint;
            }
            private set
            {
                this._rigid_Constraint = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Rigid_Constraint collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Rigid_ConstraintSpecified
        {
            get
            {
                return (this.Rigid_Constraint.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Instance_Physics_Model> _instance_Physics_Model;
        
        /// <summary>
        /// <para>The physics_model may instance any number of other physics_model elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The physics_model may instance any number of other physics_model elements.")]
        [System.Xml.Serialization.XmlElementAttribute("instance_physics_model")]
        public System.Collections.ObjectModel.Collection<Instance_Physics_Model> Instance_Physics_Model
        {
            get
            {
                return this._instance_Physics_Model;
            }
            private set
            {
                this._instance_Physics_Model = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Instance_Physics_Model collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Instance_Physics_ModelSpecified
        {
            get
            {
                return (this.Instance_Physics_Model.Count != 0);
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