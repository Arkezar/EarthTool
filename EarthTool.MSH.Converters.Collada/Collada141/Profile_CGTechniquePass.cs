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
    [System.Xml.Serialization.XmlTypeAttribute("Profile_CGTechniquePass", Namespace="http://www.collada.org/2005/11/COLLADASchema", AnonymousType=true)]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Profile_CGTechniquePass : IGl_Pipeline_Settings
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
        /// <para xml:lang="en">Initializes a new instance of the <see cref="Profile_CGTechniquePass" /> class.</para>
        /// </summary>
        public Profile_CGTechniquePass()
        {
            this._annotate = new System.Collections.ObjectModel.Collection<Fx_Annotate_Common>();
            this._color_Target = new System.Collections.ObjectModel.Collection<Fx_Colortarget_Common>();
            this._depth_Target = new System.Collections.ObjectModel.Collection<Fx_Depthtarget_Common>();
            this._stencil_Target = new System.Collections.ObjectModel.Collection<Fx_Stenciltarget_Common>();
            this._color_Clear = new System.Collections.ObjectModel.Collection<Fx_Clearcolor_Common>();
            this._depth_Clear = new System.Collections.ObjectModel.Collection<Fx_Cleardepth_Common>();
            this._stencil_Clear = new System.Collections.ObjectModel.Collection<Fx_Clearstencil_Common>();
            this._shader = new System.Collections.ObjectModel.Collection<Profile_CGTechniquePassShader>();
            this._extra = new System.Collections.ObjectModel.Collection<Extra>();
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Colortarget_Common> _color_Target;
        
        [System.Xml.Serialization.XmlElementAttribute("color_target")]
        public System.Collections.ObjectModel.Collection<Fx_Colortarget_Common> Color_Target
        {
            get
            {
                return this._color_Target;
            }
            private set
            {
                this._color_Target = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Color_Target collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Color_TargetSpecified
        {
            get
            {
                return (this.Color_Target.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Depthtarget_Common> _depth_Target;
        
        [System.Xml.Serialization.XmlElementAttribute("depth_target")]
        public System.Collections.ObjectModel.Collection<Fx_Depthtarget_Common> Depth_Target
        {
            get
            {
                return this._depth_Target;
            }
            private set
            {
                this._depth_Target = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Depth_Target collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Depth_TargetSpecified
        {
            get
            {
                return (this.Depth_Target.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Stenciltarget_Common> _stencil_Target;
        
        [System.Xml.Serialization.XmlElementAttribute("stencil_target")]
        public System.Collections.ObjectModel.Collection<Fx_Stenciltarget_Common> Stencil_Target
        {
            get
            {
                return this._stencil_Target;
            }
            private set
            {
                this._stencil_Target = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Stencil_Target collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Stencil_TargetSpecified
        {
            get
            {
                return (this.Stencil_Target.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Clearcolor_Common> _color_Clear;
        
        [System.Xml.Serialization.XmlElementAttribute("color_clear")]
        public System.Collections.ObjectModel.Collection<Fx_Clearcolor_Common> Color_Clear
        {
            get
            {
                return this._color_Clear;
            }
            private set
            {
                this._color_Clear = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Color_Clear collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Color_ClearSpecified
        {
            get
            {
                return (this.Color_Clear.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Cleardepth_Common> _depth_Clear;
        
        [System.Xml.Serialization.XmlElementAttribute("depth_clear")]
        public System.Collections.ObjectModel.Collection<Fx_Cleardepth_Common> Depth_Clear
        {
            get
            {
                return this._depth_Clear;
            }
            private set
            {
                this._depth_Clear = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Depth_Clear collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Depth_ClearSpecified
        {
            get
            {
                return (this.Depth_Clear.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Fx_Clearstencil_Common> _stencil_Clear;
        
        [System.Xml.Serialization.XmlElementAttribute("stencil_clear")]
        public System.Collections.ObjectModel.Collection<Fx_Clearstencil_Common> Stencil_Clear
        {
            get
            {
                return this._stencil_Clear;
            }
            private set
            {
                this._stencil_Clear = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Stencil_Clear collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool Stencil_ClearSpecified
        {
            get
            {
                return (this.Stencil_Clear.Count != 0);
            }
        }
        
        [System.Xml.Serialization.XmlElementAttribute("draw")]
        public string Draw { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("alpha_func")]
        public Gl_Pipeline_SettingsAlpha_Func Alpha_Func { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("blend_func")]
        public Gl_Pipeline_SettingsBlend_Func Blend_Func { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("blend_func_separate")]
        public Gl_Pipeline_SettingsBlend_Func_Separate Blend_Func_Separate { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("blend_equation")]
        public Gl_Pipeline_SettingsBlend_Equation Blend_Equation { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("blend_equation_separate")]
        public Gl_Pipeline_SettingsBlend_Equation_Separate Blend_Equation_Separate { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("color_material")]
        public Gl_Pipeline_SettingsColor_Material Color_Material { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("cull_face")]
        public Gl_Pipeline_SettingsCull_Face Cull_Face { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("depth_func")]
        public Gl_Pipeline_SettingsDepth_Func Depth_Func { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("fog_mode")]
        public Gl_Pipeline_SettingsFog_Mode Fog_Mode { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("fog_coord_src")]
        public Gl_Pipeline_SettingsFog_Coord_Src Fog_Coord_Src { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("front_face")]
        public Gl_Pipeline_SettingsFront_Face Front_Face { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_model_color_control")]
        public Gl_Pipeline_SettingsLight_Model_Color_Control Light_Model_Color_Control { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("logic_op")]
        public Gl_Pipeline_SettingsLogic_Op Logic_Op { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("polygon_mode")]
        public Gl_Pipeline_SettingsPolygon_Mode Polygon_Mode { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("shade_model")]
        public Gl_Pipeline_SettingsShade_Model Shade_Model { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("stencil_func")]
        public Gl_Pipeline_SettingsStencil_Func Stencil_Func { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("stencil_op")]
        public Gl_Pipeline_SettingsStencil_Op Stencil_Op { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("stencil_func_separate")]
        public Gl_Pipeline_SettingsStencil_Func_Separate Stencil_Func_Separate { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("stencil_op_separate")]
        public Gl_Pipeline_SettingsStencil_Op_Separate Stencil_Op_Separate { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("stencil_mask_separate")]
        public Gl_Pipeline_SettingsStencil_Mask_Separate Stencil_Mask_Separate { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_enable")]
        public Gl_Pipeline_SettingsLight_Enable Light_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_ambient")]
        public Gl_Pipeline_SettingsLight_Ambient Light_Ambient { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_diffuse")]
        public Gl_Pipeline_SettingsLight_Diffuse Light_Diffuse { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_specular")]
        public Gl_Pipeline_SettingsLight_Specular Light_Specular { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_position")]
        public Gl_Pipeline_SettingsLight_Position Light_Position { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_constant_attenuation")]
        public Gl_Pipeline_SettingsLight_Constant_Attenuation Light_Constant_Attenuation { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_linear_attenuation")]
        public Gl_Pipeline_SettingsLight_Linear_Attenuation Light_Linear_Attenuation { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_quadratic_attenuation")]
        public Gl_Pipeline_SettingsLight_Quadratic_Attenuation Light_Quadratic_Attenuation { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_spot_cutoff")]
        public Gl_Pipeline_SettingsLight_Spot_Cutoff Light_Spot_Cutoff { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_spot_direction")]
        public Gl_Pipeline_SettingsLight_Spot_Direction Light_Spot_Direction { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_spot_exponent")]
        public Gl_Pipeline_SettingsLight_Spot_Exponent Light_Spot_Exponent { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture1D")]
        public Gl_Pipeline_SettingsTexture1D Texture1D { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture2D")]
        public Gl_Pipeline_SettingsTexture2D Texture2D { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture3D")]
        public Gl_Pipeline_SettingsTexture3D Texture3D { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("textureCUBE")]
        public Gl_Pipeline_SettingsTextureCUBE TextureCUBE { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("textureRECT")]
        public Gl_Pipeline_SettingsTextureRECT TextureRECT { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("textureDEPTH")]
        public Gl_Pipeline_SettingsTextureDEPTH TextureDEPTH { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture1D_enable")]
        public Gl_Pipeline_SettingsTexture1D_Enable Texture1D_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture2D_enable")]
        public Gl_Pipeline_SettingsTexture2D_Enable Texture2D_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture3D_enable")]
        public Gl_Pipeline_SettingsTexture3D_Enable Texture3D_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("textureCUBE_enable")]
        public Gl_Pipeline_SettingsTextureCUBE_Enable TextureCUBE_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("textureRECT_enable")]
        public Gl_Pipeline_SettingsTextureRECT_Enable TextureRECT_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("textureDEPTH_enable")]
        public Gl_Pipeline_SettingsTextureDEPTH_Enable TextureDEPTH_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture_env_color")]
        public Gl_Pipeline_SettingsTexture_Env_Color Texture_Env_Color { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("texture_env_mode")]
        public Gl_Pipeline_SettingsTexture_Env_Mode Texture_Env_Mode { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("clip_plane")]
        public Gl_Pipeline_SettingsClip_Plane Clip_Plane { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("clip_plane_enable")]
        public Gl_Pipeline_SettingsClip_Plane_Enable Clip_Plane_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("blend_color")]
        public Gl_Pipeline_SettingsBlend_Color Blend_Color { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("clear_color")]
        public Gl_Pipeline_SettingsClear_Color Clear_Color { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("clear_stencil")]
        public Gl_Pipeline_SettingsClear_Stencil Clear_Stencil { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("clear_depth")]
        public Gl_Pipeline_SettingsClear_Depth Clear_Depth { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("color_mask")]
        public Gl_Pipeline_SettingsColor_Mask Color_Mask { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("depth_bounds")]
        public Gl_Pipeline_SettingsDepth_Bounds Depth_Bounds { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("depth_mask")]
        public Gl_Pipeline_SettingsDepth_Mask Depth_Mask { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("depth_range")]
        public Gl_Pipeline_SettingsDepth_Range Depth_Range { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("fog_density")]
        public Gl_Pipeline_SettingsFog_Density Fog_Density { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("fog_start")]
        public Gl_Pipeline_SettingsFog_Start Fog_Start { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("fog_end")]
        public Gl_Pipeline_SettingsFog_End Fog_End { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("fog_color")]
        public Gl_Pipeline_SettingsFog_Color Fog_Color { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_model_ambient")]
        public Gl_Pipeline_SettingsLight_Model_Ambient Light_Model_Ambient { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("lighting_enable")]
        public Gl_Pipeline_SettingsLighting_Enable Lighting_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("line_stipple")]
        public Gl_Pipeline_SettingsLine_Stipple Line_Stipple { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("line_width")]
        public Gl_Pipeline_SettingsLine_Width Line_Width { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("material_ambient")]
        public Gl_Pipeline_SettingsMaterial_Ambient Material_Ambient { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("material_diffuse")]
        public Gl_Pipeline_SettingsMaterial_Diffuse Material_Diffuse { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("material_emission")]
        public Gl_Pipeline_SettingsMaterial_Emission Material_Emission { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("material_shininess")]
        public Gl_Pipeline_SettingsMaterial_Shininess Material_Shininess { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("material_specular")]
        public Gl_Pipeline_SettingsMaterial_Specular Material_Specular { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("model_view_matrix")]
        public Gl_Pipeline_SettingsModel_View_Matrix Model_View_Matrix { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("point_distance_attenuation")]
        public Gl_Pipeline_SettingsPoint_Distance_Attenuation Point_Distance_Attenuation { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("point_fade_threshold_size")]
        public Gl_Pipeline_SettingsPoint_Fade_Threshold_Size Point_Fade_Threshold_Size { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("point_size")]
        public Gl_Pipeline_SettingsPoint_Size Point_Size { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("point_size_min")]
        public Gl_Pipeline_SettingsPoint_Size_Min Point_Size_Min { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("point_size_max")]
        public Gl_Pipeline_SettingsPoint_Size_Max Point_Size_Max { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("polygon_offset")]
        public Gl_Pipeline_SettingsPolygon_Offset Polygon_Offset { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("projection_matrix")]
        public Gl_Pipeline_SettingsProjection_Matrix Projection_Matrix { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("scissor")]
        public Gl_Pipeline_SettingsScissor Scissor { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("stencil_mask")]
        public Gl_Pipeline_SettingsStencil_Mask Stencil_Mask { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("alpha_test_enable")]
        public Gl_Pipeline_SettingsAlpha_Test_Enable Alpha_Test_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("auto_normal_enable")]
        public Gl_Pipeline_SettingsAuto_Normal_Enable Auto_Normal_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("blend_enable")]
        public Gl_Pipeline_SettingsBlend_Enable Blend_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("color_logic_op_enable")]
        public Gl_Pipeline_SettingsColor_Logic_Op_Enable Color_Logic_Op_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("color_material_enable")]
        public Gl_Pipeline_SettingsColor_Material_Enable Color_Material_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("cull_face_enable")]
        public Gl_Pipeline_SettingsCull_Face_Enable Cull_Face_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("depth_bounds_enable")]
        public Gl_Pipeline_SettingsDepth_Bounds_Enable Depth_Bounds_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("depth_clamp_enable")]
        public Gl_Pipeline_SettingsDepth_Clamp_Enable Depth_Clamp_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("depth_test_enable")]
        public Gl_Pipeline_SettingsDepth_Test_Enable Depth_Test_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("dither_enable")]
        public Gl_Pipeline_SettingsDither_Enable Dither_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("fog_enable")]
        public Gl_Pipeline_SettingsFog_Enable Fog_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_model_local_viewer_enable")]
        public Gl_Pipeline_SettingsLight_Model_Local_Viewer_Enable Light_Model_Local_Viewer_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("light_model_two_side_enable")]
        public Gl_Pipeline_SettingsLight_Model_Two_Side_Enable Light_Model_Two_Side_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("line_smooth_enable")]
        public Gl_Pipeline_SettingsLine_Smooth_Enable Line_Smooth_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("line_stipple_enable")]
        public Gl_Pipeline_SettingsLine_Stipple_Enable Line_Stipple_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("logic_op_enable")]
        public Gl_Pipeline_SettingsLogic_Op_Enable Logic_Op_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("multisample_enable")]
        public Gl_Pipeline_SettingsMultisample_Enable Multisample_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("normalize_enable")]
        public Gl_Pipeline_SettingsNormalize_Enable Normalize_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("point_smooth_enable")]
        public Gl_Pipeline_SettingsPoint_Smooth_Enable Point_Smooth_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("polygon_offset_fill_enable")]
        public Gl_Pipeline_SettingsPolygon_Offset_Fill_Enable Polygon_Offset_Fill_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("polygon_offset_line_enable")]
        public Gl_Pipeline_SettingsPolygon_Offset_Line_Enable Polygon_Offset_Line_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("polygon_offset_point_enable")]
        public Gl_Pipeline_SettingsPolygon_Offset_Point_Enable Polygon_Offset_Point_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("polygon_smooth_enable")]
        public Gl_Pipeline_SettingsPolygon_Smooth_Enable Polygon_Smooth_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("polygon_stipple_enable")]
        public Gl_Pipeline_SettingsPolygon_Stipple_Enable Polygon_Stipple_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("rescale_normal_enable")]
        public Gl_Pipeline_SettingsRescale_Normal_Enable Rescale_Normal_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sample_alpha_to_coverage_enable")]
        public Gl_Pipeline_SettingsSample_Alpha_To_Coverage_Enable Sample_Alpha_To_Coverage_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sample_alpha_to_one_enable")]
        public Gl_Pipeline_SettingsSample_Alpha_To_One_Enable Sample_Alpha_To_One_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("sample_coverage_enable")]
        public Gl_Pipeline_SettingsSample_Coverage_Enable Sample_Coverage_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("scissor_test_enable")]
        public Gl_Pipeline_SettingsScissor_Test_Enable Scissor_Test_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("stencil_test_enable")]
        public Gl_Pipeline_SettingsStencil_Test_Enable Stencil_Test_Enable { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("gl_hook_abstract")]
        public object Gl_Hook_Abstract { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<Profile_CGTechniquePassShader> _shader;
        
        /// <summary>
        /// <para>Declare and prepare a shader for execution in the rendering pipeline of a pass.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("Declare and prepare a shader for execution in the rendering pipeline of a pass.")]
        [System.Xml.Serialization.XmlElementAttribute("shader")]
        public System.Collections.ObjectModel.Collection<Profile_CGTechniquePassShader> Shader
        {
            get
            {
                return this._shader;
            }
            private set
            {
                this._shader = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Shader collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ShaderSpecified
        {
            get
            {
                return (this.Shader.Count != 0);
            }
        }
        
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
        /// <para>The sid attribute is a text string value containing the sub-identifier of this element. 
        ///											This value must be unique within the scope of the parent element. Optional attribute.</para>
        /// </summary>
        [System.ComponentModel.DescriptionAttribute("The sid attribute is a text string value containing the sub-identifier of this el" +
            "ement. This value must be unique within the scope of the parent element. Optiona" +
            "l attribute.")]
        [System.Xml.Serialization.XmlAttributeAttribute("sid")]
        public string Sid { get; set; }
    }
}
