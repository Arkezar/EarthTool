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
    /// <para>The fx_surface_common type is used to declare a resource that can be used both as the source for texture samples and as the target of a rendering pass.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("The fx_surface_common type is used to declare a resource that can be used both as" +
        " the source for texture samples and as the target of a rendering pass.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("fx_surface_common", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Cg_Surface_Type))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Glsl_Surface_Type))]
    public partial class Fx_Surface_Common : IFx_Surface_Init_Common
    {
        
        /// <summary>
        /// <para>This surface is intended to be initialized later externally by a "setparam" element.  If it is used before being initialized there is profile and platform specific behavior.  Most elements on the surface element containing this will be ignored including mip_levels, mipmap_generate, size, viewport_ratio, and format.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute(@"This surface is intended to be initialized later externally by a ""setparam"" element. If it is used before being initialized there is profile and platform specific behavior. Most elements on the surface element containing this will be ignored including mip_levels, mipmap_generate, size, viewport_ratio, and format.")]
        [System.Xml.Serialization.XmlElementAttribute("init_as_null")]
        public object Init_As_Null { get; set; }
        
        /// <summary>
        /// <para>Init as a target for depth, stencil, or color.  It does not need image data. Surface should not have mipmap_generate when using this.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Init as a target for depth, stencil, or color. It does not need image data. Surfa" +
            "ce should not have mipmap_generate when using this.")]
        [System.Xml.Serialization.XmlElementAttribute("init_as_target")]
        public object Init_As_Target { get; set; }
        
        /// <summary>
        /// <para>Init a CUBE from a compound image such as DDS</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Init a CUBE from a compound image such as DDS")]
        [System.Xml.Serialization.XmlElementAttribute("init_cube")]
        public Fx_Surface_Init_Cube_Common Init_Cube { get; set; }
        
        /// <summary>
        /// <para>Init a 3D from a compound image such as DDS</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Init a 3D from a compound image such as DDS")]
        [System.Xml.Serialization.XmlElementAttribute("init_volume")]
        public Fx_Surface_Init_Volume_Common Init_Volume { get; set; }
        
        /// <summary>
        /// <para>Init a 1D,2D,RECT,DEPTH from a compound image such as DDS</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Init a 1D,2D,RECT,DEPTH from a compound image such as DDS")]
        [System.Xml.Serialization.XmlElementAttribute("init_planar")]
        public Fx_Surface_Init_Planar_Common Init_Planar { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Surface_Init_From_Common> _init_From;
        
        /// <summary>
        /// <para>Initialize the surface one sub-surface at a time by specifying combinations of mip, face, and slice which make sense for a particular surface type.  Each sub-surface is initialized by a common 2D image, not a complex compound image such as DDS. If not all subsurfaces are initialized, it is invalid and will result in profile and platform specific behavior unless mipmap_generate is responsible for initializing the remainder of the sub-surfaces</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute(@"Initialize the surface one sub-surface at a time by specifying combinations of mip, face, and slice which make sense for a particular surface type. Each sub-surface is initialized by a common 2D image, not a complex compound image such as DDS. If not all subsurfaces are initialized, it is invalid and will result in profile and platform specific behavior unless mipmap_generate is responsible for initializing the remainder of the sub-surfaces")]
        [System.Xml.Serialization.XmlElementAttribute("init_from")]
        public System.Collections.ObjectModel.Collection<Fx_Surface_Init_From_Common> Init_From
        {
            get
            {
                return this._init_From;
            }
            private set
            {
                this._init_From = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Init_From collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Init_FromSpecified
        {
            get
            {
                return (this.Init_From.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Fx_Surface_Common" /> class.</para>
        /// </summary>
        public Fx_Surface_Common()
        {
            this._init_From = new System.Collections.ObjectModel.Collection<Fx_Surface_Init_From_Common>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        /// <summary>
        /// <para>Contains a string representing the profile and platform specific texel format that the author would like this surface to use.  If this element is not specified then the application will use a common format R8G8B8A8 with linear color gradient, not  sRGB.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Contains a string representing the profile and platform specific texel format tha" +
            "t the author would like this surface to use. If this element is not specified th" +
            "en the application will use a common format R8G8B8A8 with linear color gradient," +
            " not sRGB.")]
        [System.Xml.Serialization.XmlElementAttribute("format")]
        public string Format { get; set; }
        
        /// <summary>
        /// <para>If the exact format cannot be resolved via the "format" element then the format_hint will describe the important features of the format so that the application may select a compatable or close format</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("If the exact format cannot be resolved via the \"format\" element then the format_h" +
            "int will describe the important features of the format so that the application m" +
            "ay select a compatable or close format")]
        [System.Xml.Serialization.XmlElementAttribute("format_hint")]
        public Fx_Surface_Format_Hint_Common Format_Hint { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private string _size = "0 0 0";
        
        /// <summary>
        /// <para>The surface should be sized to these exact dimensions</para>
        /// <para xml:lang="en">Minimum length: 3.</para>
        /// <para xml:lang="en">Maximum length: 3.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute("0 0 0")]
        [System.ComponentModel.DescriptionAttribute("The surface should be sized to these exact dimensions")]
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(3)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(3)]
        [System.Xml.Serialization.XmlElementAttribute("size")]
        public string Size
        {
            get
            {
                return this._size;
            }
            set
            {
                this._size = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private string _viewport_Ratio = "1 1";
        
        /// <summary>
        /// <para>The surface should be sized to a dimension based on this ratio of the viewport's dimensions in pixels</para>
        /// <para xml:lang="en">Minimum length: 2.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute("1 1")]
        [System.ComponentModel.DescriptionAttribute("The surface should be sized to a dimension based on this ratio of the viewport\'s " +
            "dimensions in pixels")]
        [System.ComponentModel.DataAnnotations.MinLengthAttribute(2)]
        [System.ComponentModel.DataAnnotations.MaxLengthAttribute(2)]
        [System.Xml.Serialization.XmlElementAttribute("viewport_ratio")]
        public string Viewport_Ratio
        {
            get
            {
                return this._viewport_Ratio;
            }
            set
            {
                this._viewport_Ratio = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private uint _mip_Levels = 0u;
        
        /// <summary>
        /// <para>the surface should contain the following number of MIP levels.  If this element is not present it is assumed that all miplevels exist until a dimension becomes 1 texel.  To create a surface that has only one level of mip maps (mip=0) set this to 1.  If the value is 0 the result is the same as if mip_levels was unspecified, all possible mip_levels will exist.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0u)]
        [System.ComponentModel.DescriptionAttribute(@"the surface should contain the following number of MIP levels. If this element is not present it is assumed that all miplevels exist until a dimension becomes 1 texel. To create a surface that has only one level of mip maps (mip=0) set this to 1. If the value is 0 the result is the same as if mip_levels was unspecified, all possible mip_levels will exist.")]
        [System.Xml.Serialization.XmlElementAttribute("mip_levels")]
        public uint Mip_Levels
        {
            get
            {
                return this._mip_Levels;
            }
            set
            {
                this._mip_Levels = value;
            }
        }
        
        /// <summary>
        /// <para>By default it is assumed that mipmaps are supplied by the author so, if not all subsurfaces are initialized, it is invalid and will result in profile and platform specific behavior unless mipmap_generate is responsible for initializing the remainder of the sub-surfaces</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute(@"By default it is assumed that mipmaps are supplied by the author so, if not all subsurfaces are initialized, it is invalid and will result in profile and platform specific behavior unless mipmap_generate is responsible for initializing the remainder of the sub-surfaces")]
        [System.Xml.Serialization.XmlElementAttribute("mipmap_generate")]
        public bool Mipmap_Generate { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the Mipmap_Generate property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Mipmap_GenerateSpecified { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Extra> _extra;
        
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
        /// <para>Specifying the type of a surface is mandatory though the type may be "UNTYPED".  When a surface is typed as UNTYPED, it is said to be temporarily untyped and instead will be typed later by the context it is used in such as which samplers reference it in that are used in a particular technique or pass.   If there is a type mismatch between what is set into it later and what the runtime decides the type should be the result in profile and platform specific behavior.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute(@"Specifying the type of a surface is mandatory though the type may be ""UNTYPED"". When a surface is typed as UNTYPED, it is said to be temporarily untyped and instead will be typed later by the context it is used in such as which samplers reference it in that are used in a particular technique or pass. If there is a type mismatch between what is set into it later and what the runtime decides the type should be the result in profile and platform specific behavior.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public Fx_Surface_Type_Enum Type { get; set; }
    }
}