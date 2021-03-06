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
    /// <para>The per-texel layout of the format.  The length of the string indicate how many channels there are and the letter respresents the name of the channel.  There are typically 0 to 4 channels.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("The per-texel layout of the format. The length of the string indicate how many ch" +
        "annels there are and the letter respresents the name of the channel. There are t" +
        "ypically 0 to 4 channels.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.0.560.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("fx_surface_format_hint_channels_enum", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public enum Fx_Surface_Format_Hint_Channels_Enum
    {
        
        /// <summary>
        /// <para>RGB color  map</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("RGB color map")]
        RGB,
        
        /// <summary>
        /// <para>RGB color + Alpha map often used for color + transparency or other things packed into channel A like specular power</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("RGB color + Alpha map often used for color + transparency or other things packed " +
            "into channel A like specular power")]
        RGBA,
        
        /// <summary>
        /// <para>Luminance map often used for light mapping</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Luminance map often used for light mapping")]
        L,
        
        /// <summary>
        /// <para>Luminance+Alpha map often used for light mapping</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Luminance+Alpha map often used for light mapping")]
        LA,
        
        /// <summary>
        /// <para>Depth map often used for displacement, parellax, relief, or shadow mapping</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Depth map often used for displacement, parellax, relief, or shadow mapping")]
        D,
        
        /// <summary>
        /// <para>Typically used for normal maps or 3component displacement maps.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Typically used for normal maps or 3component displacement maps.")]
        XYZ,
        
        /// <summary>
        /// <para>Typically used for normal maps where W is the depth for relief or parrallax mapping</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Typically used for normal maps where W is the depth for relief or parrallax mappi" +
            "ng")]
        XYZW,
    }
}
