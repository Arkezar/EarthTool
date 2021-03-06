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
    /// <para>The asset element defines asset management information regarding its parent element.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("The asset element defines asset management information regarding its parent eleme" +
        "nt.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("asset", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("asset", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public partial class Asset
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<AssetContributor> _contributor;
        
        /// <summary>
        /// <para>The contributor element defines authoring information for asset management</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The contributor element defines authoring information for asset management")]
        [System.Xml.Serialization.XmlElementAttribute("contributor")]
        public System.Collections.ObjectModel.Collection<AssetContributor> Contributor
        {
            get
            {
                return this._contributor;
            }
            private set
            {
                this._contributor = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Contributor collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ContributorSpecified
        {
            get
            {
                return (this.Contributor.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Asset" /> class.</para>
        /// </summary>
        public Asset()
        {
            this._contributor = new System.Collections.ObjectModel.Collection<AssetContributor>();
        }
        
        /// <summary>
        /// <para>The created element contains the date and time that the parent element was created and is 
        ///						represented in an ISO 8601 format.  The created element may appear zero or one time.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The created element contains the date and time that the parent element was create" +
            "d and is represented in an ISO 8601 format. The created element may appear zero " +
            "or one time.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("created", DataType="dateTime")]
        public System.DateTime Created { get; set; }
        
        /// <summary>
        /// <para>The keywords element contains a list of words used as search criteria for the parent element. 
        ///						The keywords element may appear zero or more times.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The keywords element contains a list of words used as search criteria for the par" +
            "ent element. The keywords element may appear zero or more times.")]
        [System.Xml.Serialization.XmlElementAttribute("keywords")]
        public string Keywords { get; set; }
        
        /// <summary>
        /// <para>The modified element contains the date and time that the parent element was last modified and 
        ///						represented in an ISO 8601 format. The modified element may appear zero or one time.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The modified element contains the date and time that the parent element was last " +
            "modified and represented in an ISO 8601 format. The modified element may appear " +
            "zero or one time.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlElementAttribute("modified", DataType="dateTime")]
        public System.DateTime Modified { get; set; }
        
        /// <summary>
        /// <para>The revision element contains the revision information for the parent element. The revision 
        ///						element may appear zero or one time.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The revision element contains the revision information for the parent element. Th" +
            "e revision element may appear zero or one time.")]
        [System.Xml.Serialization.XmlElementAttribute("revision")]
        public string Revision { get; set; }
        
        /// <summary>
        /// <para>The subject element contains a description of the topical subject of the parent element. The 
        ///						subject element may appear zero or one time.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The subject element contains a description of the topical subject of the parent e" +
            "lement. The subject element may appear zero or one time.")]
        [System.Xml.Serialization.XmlElementAttribute("subject")]
        public string Subject { get; set; }
        
        /// <summary>
        /// <para>The title element contains the title information for the parent element. The title element may 
        ///						appear zero or one time.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The title element contains the title information for the parent element. The titl" +
            "e element may appear zero or one time.")]
        [System.Xml.Serialization.XmlElementAttribute("title")]
        public string Title { get; set; }
        
        /// <summary>
        /// <para>The unit element contains descriptive information about unit of measure. It has attributes for 
        ///						the name of the unit and the measurement with respect to the meter. The unit element may appear 
        ///						zero or one time.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The unit element contains descriptive information about unit of measure. It has a" +
            "ttributes for the name of the unit and the measurement with respect to the meter" +
            ". The unit element may appear zero or one time.")]
        [System.Xml.Serialization.XmlElementAttribute("unit")]
        public AssetUnit Unit { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private UpAxisType _up_Axis = Collada141.UpAxisType.Y_UP;
        
        /// <summary>
        /// <para>The up_axis element contains descriptive information about coordinate system of the geometric 
        ///						data. All coordinates are right-handed by definition. This element specifies which axis is 
        ///						considered up. The default is the Y-axis. The up_axis element may appear zero or one time.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(Collada141.UpAxisType.Y_UP)]
        [System.ComponentModel.DescriptionAttribute(@"The up_axis element contains descriptive information about coordinate system of the geometric data. All coordinates are right-handed by definition. This element specifies which axis is considered up. The default is the Y-axis. The up_axis element may appear zero or one time.")]
        [System.Xml.Serialization.XmlElementAttribute("up_axis")]
        public UpAxisType Up_Axis
        {
            get
            {
                return this._up_Axis;
            }
            set
            {
                this._up_Axis = value;
            }
        }
    }
}
