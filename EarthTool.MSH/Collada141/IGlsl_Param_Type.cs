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
    /// <para>A group that specifies the allowable types for GLSL profile parameters.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("A group that specifies the allowable types for GLSL profile parameters.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    public partial interface IGlsl_Param_Type
    {
        
        bool Bool
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        string Bool2
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        string Bool3
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        string Bool4
        {
            get;
            set;
        }
        
        float Float
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        string Float2
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        string Float3
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        string Float4
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        string Float2X2
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 9.</para>
        /// <para xml:lang="en">Maximum length: 9.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(9)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(9)]
        string Float3X3
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 16.</para>
        /// <para xml:lang="en">Maximum length: 16.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(16)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(16)]
        string Float4X4
        {
            get;
            set;
        }
        
        int Int
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        string Int2
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        string Int3
        {
            get;
            set;
        }
        
        /// <summary>
        /// <para xml:lang="en">Minimum length: 4.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(4)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(4)]
        string Int4
        {
            get;
            set;
        }
        
        Glsl_Surface_Type Surface
        {
            get;
            set;
        }
        
        Gl_Sampler1D Sampler1D
        {
            get;
            set;
        }
        
        Gl_Sampler2D Sampler2D
        {
            get;
            set;
        }
        
        Gl_Sampler3D Sampler3D
        {
            get;
            set;
        }
        
        Gl_SamplerCUBE SamplerCUBE
        {
            get;
            set;
        }
        
        Gl_SamplerRECT SamplerRECT
        {
            get;
            set;
        }
        
        Gl_SamplerDEPTH SamplerDEPTH
        {
            get;
            set;
        }
        
        Gl_Enumeration Enum
        {
            get;
            set;
        }
    }
}