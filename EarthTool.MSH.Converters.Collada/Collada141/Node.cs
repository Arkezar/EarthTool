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
    /// <para>Nodes embody the hierarchical relationship of elements in the scene.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("Nodes embody the hierarchical relationship of elements in the scene.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("node", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("node", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Node
    {
        
        /// <summary>
        /// <para>The node element may contain an asset element.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may contain an asset element.")]
        [System.Xml.Serialization.XmlElementAttribute("asset")]
        public Asset Asset { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Lookat> _lookat;
        
        /// <summary>
        /// <para>The node element may contain any number of lookat elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may contain any number of lookat elements.")]
        [System.Xml.Serialization.XmlElementAttribute("lookat")]
        public System.Collections.ObjectModel.Collection<Lookat> Lookat
        {
            get
            {
                return this._lookat;
            }
            private set
            {
                this._lookat = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Lookat collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool LookatSpecified
        {
            get
            {
                return (this.Lookat.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Node" /> class.</para>
        /// </summary>
        public Node()
        {
            this._lookat = new System.Collections.ObjectModel.Collection<Lookat>();
            this._matrix = new System.Collections.ObjectModel.Collection<Matrix>();
            this._rotate = new System.Collections.ObjectModel.Collection<Rotate>();
            this._scale = new System.Collections.ObjectModel.Collection<TargetableFloat3>();
            this._skew = new System.Collections.ObjectModel.Collection<Skew>();
            this._translate = new System.Collections.ObjectModel.Collection<TargetableFloat3>();
            this._instance_Camera = new System.Collections.ObjectModel.Collection<InstanceWithExtra>();
            this._instance_Controller = new System.Collections.ObjectModel.Collection<Instance_Controller>();
            this._instance_Geometry = new System.Collections.ObjectModel.Collection<Instance_Geometry>();
            this._instance_Light = new System.Collections.ObjectModel.Collection<InstanceWithExtra>();
            this._instance_Node = new System.Collections.ObjectModel.Collection<InstanceWithExtra>();
            this._nodeProperty = new System.Collections.ObjectModel.Collection<Node>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
            this.Layer = new System.Collections.ObjectModel.Collection<string>();
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Matrix> _matrix;
        
        /// <summary>
        /// <para>The node element may contain any number of matrix elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may contain any number of matrix elements.")]
        [System.Xml.Serialization.XmlElementAttribute("matrix")]
        public System.Collections.ObjectModel.Collection<Matrix> Matrix
        {
            get
            {
                return this._matrix;
            }
            private set
            {
                this._matrix = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Matrix collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool MatrixSpecified
        {
            get
            {
                return (this.Matrix.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Rotate> _rotate;
        
        /// <summary>
        /// <para>The node element may contain any number of rotate elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may contain any number of rotate elements.")]
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
        private System.Collections.ObjectModel.Collection<TargetableFloat3> _scale;
        
        /// <summary>
        /// <para>The node element may contain any number of scale elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may contain any number of scale elements.")]
        [System.Xml.Serialization.XmlElementAttribute("scale")]
        public System.Collections.ObjectModel.Collection<TargetableFloat3> Scale
        {
            get
            {
                return this._scale;
            }
            private set
            {
                this._scale = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Scale collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ScaleSpecified
        {
            get
            {
                return (this.Scale.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Skew> _skew;
        
        /// <summary>
        /// <para>The node element may contain any number of skew elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may contain any number of skew elements.")]
        [System.Xml.Serialization.XmlElementAttribute("skew")]
        public System.Collections.ObjectModel.Collection<Skew> Skew
        {
            get
            {
                return this._skew;
            }
            private set
            {
                this._skew = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Skew collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SkewSpecified
        {
            get
            {
                return (this.Skew.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<TargetableFloat3> _translate;
        
        /// <summary>
        /// <para>The node element may contain any number of translate elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may contain any number of translate elements.")]
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
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<InstanceWithExtra> _instance_Camera;
        
        /// <summary>
        /// <para>The node element may instance any number of camera objects.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may instance any number of camera objects.")]
        [System.Xml.Serialization.XmlElementAttribute("instance_camera")]
        public System.Collections.ObjectModel.Collection<InstanceWithExtra> Instance_Camera
        {
            get
            {
                return this._instance_Camera;
            }
            private set
            {
                this._instance_Camera = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Instance_Camera collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Instance_CameraSpecified
        {
            get
            {
                return (this.Instance_Camera.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Instance_Controller> _instance_Controller;
        
        /// <summary>
        /// <para>The node element may instance any number of controller objects.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may instance any number of controller objects.")]
        [System.Xml.Serialization.XmlElementAttribute("instance_controller")]
        public System.Collections.ObjectModel.Collection<Instance_Controller> Instance_Controller
        {
            get
            {
                return this._instance_Controller;
            }
            private set
            {
                this._instance_Controller = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Instance_Controller collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Instance_ControllerSpecified
        {
            get
            {
                return (this.Instance_Controller.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Instance_Geometry> _instance_Geometry;
        
        /// <summary>
        /// <para>The node element may instance any number of geometry objects.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may instance any number of geometry objects.")]
        [System.Xml.Serialization.XmlElementAttribute("instance_geometry")]
        public System.Collections.ObjectModel.Collection<Instance_Geometry> Instance_Geometry
        {
            get
            {
                return this._instance_Geometry;
            }
            private set
            {
                this._instance_Geometry = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Instance_Geometry collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Instance_GeometrySpecified
        {
            get
            {
                return (this.Instance_Geometry.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<InstanceWithExtra> _instance_Light;
        
        /// <summary>
        /// <para>The node element may instance any number of light objects.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may instance any number of light objects.")]
        [System.Xml.Serialization.XmlElementAttribute("instance_light")]
        public System.Collections.ObjectModel.Collection<InstanceWithExtra> Instance_Light
        {
            get
            {
                return this._instance_Light;
            }
            private set
            {
                this._instance_Light = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Instance_Light collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Instance_LightSpecified
        {
            get
            {
                return (this.Instance_Light.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<InstanceWithExtra> _instance_Node;
        
        /// <summary>
        /// <para>The node element may instance any number of node elements or hierarchies objects.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may instance any number of node elements or hierarchies objects." +
            "")]
        [System.Xml.Serialization.XmlElementAttribute("instance_node")]
        public System.Collections.ObjectModel.Collection<InstanceWithExtra> Instance_Node
        {
            get
            {
                return this._instance_Node;
            }
            private set
            {
                this._instance_Node = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Instance_Node collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Instance_NodeSpecified
        {
            get
            {
                return (this.Instance_Node.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Node> _nodeProperty;
        
        /// <summary>
        /// <para>The node element may be hierarchical and be the parent of any number of other node elements.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The node element may be hierarchical and be the parent of any number of other nod" +
            "e elements.")]
        [System.Xml.Serialization.XmlElementAttribute("node")]
        public System.Collections.ObjectModel.Collection<Node> NodeProperty
        {
            get
            {
                return this._nodeProperty;
            }
            private set
            {
                this._nodeProperty = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the NodeProperty collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool NodePropertySpecified
        {
            get
            {
                return (this.NodeProperty.Count != 0);
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
        
        /// <summary>
        /// <para>The sid attribute is a text string value containing the sub-identifier of this element. 
        ///					This value must be unique within the scope of the parent element. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The sid attribute is a text string value containing the sub-identifier of this el" +
            "ement. This value must be unique within the scope of the parent element. Optiona" +
            "l attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("sid")]
        public string Sid { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private NodeType _type = Collada141.NodeType.NODE;
        
        /// <summary>
        /// <para>The type attribute indicates the type of the node element. The default value is “NODE”. 
        ///					Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(Collada141.NodeType.NODE)]
        [System.ComponentModel.DescriptionAttribute("The type attribute indicates the type of the node element. The default value is “" +
            "NODE”. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public NodeType Type
        {
            get
            {
                return this._type;
            }
            set
            {
                this._type = value;
            }
        }
        
        /// <summary>
        /// <para>The layer attribute indicates the names of the layers to which this node belongs.  For example, 
        ///					a value of “foreground glowing” indicates that this node belongs to both the ‘foreground’ layer 
        ///					and the ‘glowing’ layer.  The default value is empty, indicating that the node doesn’t belong to 
        ///					any layer.  Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute(@"The layer attribute indicates the names of the layers to which this node belongs. For example, a value of “foreground glowing” indicates that this node belongs to both the ‘foreground’ layer and the ‘glowing’ layer. The default value is empty, indicating that the node doesn’t belong to any layer. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("layer")]
        public System.Collections.ObjectModel.Collection<string> Layer { get; private set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Layer collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool LayerSpecified
        {
            get
            {
                return (this.Layer.Count != 0);
            }
        }
    }
}