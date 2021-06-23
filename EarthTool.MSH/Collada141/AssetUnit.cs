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
    [System.Xml.Serialization.XmlTypeAttribute("AssetUnit", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class AssetUnit
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private double _meter = 1D;
        
        /// <summary>
        /// <para>The meter attribute specifies the measurement with respect to the meter. The default 
        ///								value for the meter attribute is “1.0”.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1D)]
        [System.ComponentModel.DescriptionAttribute("The meter attribute specifies the measurement with respect to the meter. The defa" +
            "ult value for the meter attribute is “1.0”.")]
        [System.Xml.Serialization.XmlAttributeAttribute("meter")]
        public double Meter
        {
            get
            {
                return this._meter;
            }
            set
            {
                this._meter = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private string _name = "meter";
        
        /// <summary>
        /// <para>The name attribute specifies the name of the unit. The default value for the name 
        ///								attribute is “meter”.</para>
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute("meter")]
        [System.ComponentModel.DescriptionAttribute("The name attribute specifies the name of the unit. The default value for the name" +
            " attribute is “meter”.")]
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }
    }
}