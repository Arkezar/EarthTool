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
    [System.Xml.Serialization.XmlTypeAttribute("fx_opaque_enum", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
    public enum Fx_Opaque_Enum
    {
        
        /// <summary>
        /// <para>When a transparent opaque attribute is set to A_ONE, it means the transparency information will be taken from the alpha channel of the color, texture, or parameter supplying the value. The value of 1.0 is opaque in this mode.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("When a transparent opaque attribute is set to A_ONE, it means the transparency in" +
            "formation will be taken from the alpha channel of the color, texture, or paramet" +
            "er supplying the value. The value of 1.0 is opaque in this mode.")]
        A_ONE,
        
        /// <summary>
        /// <para>When a transparent opaque attribute is set to RGB_ZERO, it means the transparency information will be taken from the red, green, and blue channels of the color, texture, or parameter supplying the value. Each channel is modulated independently. The value of 0.0 is opaque in this mode.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute(@"When a transparent opaque attribute is set to RGB_ZERO, it means the transparency information will be taken from the red, green, and blue channels of the color, texture, or parameter supplying the value. Each channel is modulated independently. The value of 0.0 is opaque in this mode.")]
        RGB_ZERO,
    }
}
