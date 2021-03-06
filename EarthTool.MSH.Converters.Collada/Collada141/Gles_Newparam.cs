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
    /// <para>Create a new, named param object in the GLES Runtime, assign it a type, an initial value, and additional attributes at declaration time.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("Create a new, named param object in the GLES Runtime, assign it a type, an initia" +
        "l value, and additional attributes at declaration time.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("gles_newparam", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Gles_Newparam : IGles_Basic_Type_Common
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Annotate_Common> _annotate;
        
        /// <summary>
        /// <para>The annotate element allows you to specify an annotation for this new param.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The annotate element allows you to specify an annotation for this new param.")]
        [System.Xml.Serialization.XmlElementAttribute("annotate")]
        public System.Collections.ObjectModel.Collection<Fx_Annotate_Common> Annotate
        {
            get
            {
                return this._annotate;
            }
            private set
            {
                this._annotate = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Annotate collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool AnnotateSpecified
        {
            get
            {
                return (this.Annotate.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Gles_Newparam" /> class.</para>
        /// </summary>
        public Gles_Newparam()
        {
            this._annotate = new System.Collections.ObjectModel.Collection<Fx_Annotate_Common>();
        }
        
        /// <summary>
        /// <para>The semantic element allows you to specify a semantic for this new param.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The semantic element allows you to specify a semantic for this new param.")]
        [System.Xml.Serialization.XmlElementAttribute("semantic")]
        public string Semantic { get; set; }
        
        /// <summary>
        /// <para>The modifier element allows you to specify a modifier for this new param.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The modifier element allows you to specify a modifier for this new param.")]
        [System.Xml.Serialization.XmlElementAttribute("modifier")]
        public Fx_Modifier_Enum_Common Modifier { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Modifier property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ModifierSpecified { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("bool")]
        public bool Bool { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Bool property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool BoolSpecified { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        [System.Xml.Serialization.XmlElementAttribute("bool2")]
        public string Bool2 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        [System.Xml.Serialization.XmlElementAttribute("bool3")]
        public string Bool3 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        [System.Xml.Serialization.XmlElementAttribute("bool4")]
        public string Bool4 { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("int")]
        public long Int { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Int property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool IntSpecified { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        [System.Xml.Serialization.XmlElementAttribute("int2")]
        public string Int2 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        [System.Xml.Serialization.XmlElementAttribute("int3")]
        public string Int3 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        [System.Xml.Serialization.XmlElementAttribute("int4")]
        public string Int4 { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("float")]
        public double Float { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Float property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool FloatSpecified { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        [System.Xml.Serialization.XmlElementAttribute("float2")]
        public string Float2 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        [System.Xml.Serialization.XmlElementAttribute("float3")]
        public string Float3 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        [System.Xml.Serialization.XmlElementAttribute("float4")]
        public string Float4 { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("float1x1")]
        public double Float1X1 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Float1X1 property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Float1X1Specified { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        [System.Xml.Serialization.XmlElementAttribute("float1x2")]
        public string Float1X2 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        [System.Xml.Serialization.XmlElementAttribute("float1x3")]
        public string Float1X3 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        [System.Xml.Serialization.XmlElementAttribute("float1x4")]
        public string Float1X4 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        [System.Xml.Serialization.XmlElementAttribute("float2x1")]
        public string Float2X1 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        [System.Xml.Serialization.XmlElementAttribute("float2x2")]
        public string Float2X2 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 6.</para>
        /// <para xml:lang="en">Maximum length: 6.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(6)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(6)]
        [System.Xml.Serialization.XmlElementAttribute("float2x3")]
        public string Float2X3 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 8.</para>
        /// <para xml:lang="en">Maximum length: 8.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(8)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(8)]
        [System.Xml.Serialization.XmlElementAttribute("float2x4")]
        public string Float2X4 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        [System.Xml.Serialization.XmlElementAttribute("float3x1")]
        public string Float3X1 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 6.</para>
        /// <para xml:lang="en">Maximum length: 6.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(6)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(6)]
        [System.Xml.Serialization.XmlElementAttribute("float3x2")]
        public string Float3X2 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 9.</para>
        /// <para xml:lang="en">Maximum length: 9.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(9)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(9)]
        [System.Xml.Serialization.XmlElementAttribute("float3x3")]
        public string Float3X3 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 12.</para>
        /// <para xml:lang="en">Maximum length: 12.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(12)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(12)]
        [System.Xml.Serialization.XmlElementAttribute("float3x4")]
        public string Float3X4 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        [System.Xml.Serialization.XmlElementAttribute("float4x1")]
        public string Float4X1 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 8.</para>
        /// <para xml:lang="en">Maximum length: 8.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(8)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(8)]
        [System.Xml.Serialization.XmlElementAttribute("float4x2")]
        public string Float4X2 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 12.</para>
        /// <para xml:lang="en">Maximum length: 12.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(12)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(12)]
        [System.Xml.Serialization.XmlElementAttribute("float4x3")]
        public string Float4X3 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 16.</para>
        /// <para xml:lang="en">Maximum length: 16.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(16)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(16)]
        [System.Xml.Serialization.XmlElementAttribute("float4x4")]
        public string Float4X4 { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("surface")]
        public Fx_Surface_Common Surface { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture_pipeline")]
        public Gles_Texture_Pipeline Texture_Pipeline { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sampler_state")]
        public Gles_Sampler_State Sampler_State { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture_unit")]
        public Gles_Texture_Unit Texture_Unit { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("enum")]
        public Gles_Enumeration Enum { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Enum property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool EnumSpecified { get; set; }
        
        /// <summary>
        /// <para>The sid attribute is a text string value containing the sub-identifier of this element. 
        ///				This value must be unique within the scope of the parent element.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The sid attribute is a text string value containing the sub-identifier of this el" +
            "ement. This value must be unique within the scope of the parent element.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("sid")]
        public string Sid { get; set; }
    }
}
