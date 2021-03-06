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
    [System.Xml.Serialization.XmlTypeAttribute("glsl_setparam", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Glsl_Setparam : IGlsl_Param_Type
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Annotate_Common> _annotate;
        
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
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Glsl_Setparam" /> class.</para>
        /// </summary>
        public Glsl_Setparam()
        {
            this._annotate = new System.Collections.ObjectModel.Collection<Fx_Annotate_Common>();
        }
        
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
        
        [System.Xml.Serialization.XmlElementAttribute("float")]
        public float Float { get; set; }
        
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
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        [System.Xml.Serialization.XmlElementAttribute("float2x2")]
        public string Float2X2 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 9.</para>
        /// <para xml:lang="en">Maximum length: 9.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(9)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(9)]
        [System.Xml.Serialization.XmlElementAttribute("float3x3")]
        public string Float3X3 { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 16.</para>
        /// <para xml:lang="en">Maximum length: 16.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(16)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(16)]
        [System.Xml.Serialization.XmlElementAttribute("float4x4")]
        public string Float4X4 { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("int")]
        public int Int { get; set; }
        
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
        
        [System.Xml.Serialization.XmlElementAttribute("surface")]
        public Glsl_Surface_Type Surface { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sampler1D")]
        public Gl_Sampler1D Sampler1D { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sampler2D")]
        public Gl_Sampler2D Sampler2D { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sampler3D")]
        public Gl_Sampler3D Sampler3D { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("samplerCUBE")]
        public Gl_SamplerCUBE SamplerCUBE { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("samplerRECT")]
        public Gl_SamplerRECT SamplerRECT { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("samplerDEPTH")]
        public Gl_SamplerDEPTH SamplerDEPTH { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("enum")]
        public Gl_Enumeration Enum { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Enum property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool EnumSpecified { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("array")]
        public Glsl_Setarray_Type Array { get; set; }
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public string Ref { get; set; }
        
        [System.Xml.Serialization.XmlAttributeAttribute("program")]
        public string Program { get; set; }
    }
}
