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
    [System.Xml.Serialization.XmlTypeAttribute("Rigid_BodyTechnique_CommonMass_Frame", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Rigid_BodyTechnique_CommonMass_Frame
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<TargetableFloat3> _translate;
        
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
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Rigid_BodyTechnique_CommonMass_Frame" /> class.</para>
        /// </summary>
        public Rigid_BodyTechnique_CommonMass_Frame()
        {
            this._translate = new System.Collections.ObjectModel.Collection<TargetableFloat3>();
            this._rotate = new System.Collections.ObjectModel.Collection<Rotate>();
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Rotate> _rotate;
        
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
    }
}
