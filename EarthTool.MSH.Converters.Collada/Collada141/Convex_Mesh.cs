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
    /// <para>The definition of the convex_mesh element is identical to the mesh element with the exception that 
    ///			instead of a complete description (source, vertices, polygons etc.), it may simply point to another 
    ///			geometry to derive its shape. The latter case means that the convex hull of that geometry should 
    ///			be computed and is indicated by the optional “convex_hull_of” attribute.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute(@"The definition of the convex_mesh element is identical to the mesh element with the exception that instead of a complete description (source, vertices, polygons etc.), it may simply point to another geometry to derive its shape. The latter case means that the convex hull of that geometry should be computed and is indicated by the optional “convex_hull_of” attribute.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("convex_mesh", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("convex_mesh", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Convex_Mesh
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Source> _source;
        
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
        /// <para xml:lang="en">Gets a value indicating whether the Source collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SourceSpecified
        {
            get
            {
                return (this.Source.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Convex_Mesh" /> class.</para>
        /// </summary>
        public Convex_Mesh()
        {
            this._source = new System.Collections.ObjectModel.Collection<Source>();
            this._lines = new System.Collections.ObjectModel.Collection<Lines>();
            this._linestrips = new System.Collections.ObjectModel.Collection<Linestrips>();
            this._polygons = new System.Collections.ObjectModel.Collection<Polygons>();
            this._polylist = new System.Collections.ObjectModel.Collection<Polylist>();
            this._triangles = new System.Collections.ObjectModel.Collection<Triangles>();
            this._trifans = new System.Collections.ObjectModel.Collection<Trifans>();
            this._tristrips = new System.Collections.ObjectModel.Collection<Tristrips>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        [System.Xml.Serialization.XmlElementAttribute("vertices")]
        public Vertices Vertices { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Lines> _lines;
        
        [System.Xml.Serialization.XmlElementAttribute("lines")]
        public System.Collections.ObjectModel.Collection<Lines> Lines
        {
            get
            {
                return this._lines;
            }
            private set
            {
                this._lines = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Lines collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool LinesSpecified
        {
            get
            {
                return (this.Lines.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Linestrips> _linestrips;
        
        [System.Xml.Serialization.XmlElementAttribute("linestrips")]
        public System.Collections.ObjectModel.Collection<Linestrips> Linestrips
        {
            get
            {
                return this._linestrips;
            }
            private set
            {
                this._linestrips = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Linestrips collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool LinestripsSpecified
        {
            get
            {
                return (this.Linestrips.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Polygons> _polygons;
        
        [System.Xml.Serialization.XmlElementAttribute("polygons")]
        public System.Collections.ObjectModel.Collection<Polygons> Polygons
        {
            get
            {
                return this._polygons;
            }
            private set
            {
                this._polygons = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Polygons collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PolygonsSpecified
        {
            get
            {
                return (this.Polygons.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Polylist> _polylist;
        
        [System.Xml.Serialization.XmlElementAttribute("polylist")]
        public System.Collections.ObjectModel.Collection<Polylist> Polylist
        {
            get
            {
                return this._polylist;
            }
            private set
            {
                this._polylist = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Polylist collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PolylistSpecified
        {
            get
            {
                return (this.Polylist.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Triangles> _triangles;
        
        [System.Xml.Serialization.XmlElementAttribute("triangles")]
        public System.Collections.ObjectModel.Collection<Triangles> Triangles
        {
            get
            {
                return this._triangles;
            }
            private set
            {
                this._triangles = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Triangles collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TrianglesSpecified
        {
            get
            {
                return (this.Triangles.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Trifans> _trifans;
        
        [System.Xml.Serialization.XmlElementAttribute("trifans")]
        public System.Collections.ObjectModel.Collection<Trifans> Trifans
        {
            get
            {
                return this._trifans;
            }
            private set
            {
                this._trifans = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Trifans collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TrifansSpecified
        {
            get
            {
                return (this.Trifans.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Tristrips> _tristrips;
        
        [System.Xml.Serialization.XmlElementAttribute("tristrips")]
        public System.Collections.ObjectModel.Collection<Tristrips> Tristrips
        {
            get
            {
                return this._tristrips;
            }
            private set
            {
                this._tristrips = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Tristrips collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TristripsSpecified
        {
            get
            {
                return (this.Tristrips.Count != 0);
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
        /// <para>The convex_hull_of attribute is a URI string of geometry to compute the convex hull of. 
        ///					Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The convex_hull_of attribute is a URI string of geometry to compute the convex hu" +
            "ll of. Optional attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("convex_hull_of")]
        public string Convex_Hull_Of { get; set; }
    }
}
