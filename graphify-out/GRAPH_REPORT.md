# Graph Report - EarthTool  (2026-08-08)

## Corpus Check
- 302 files · ~175,012 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2961 nodes · 7152 edges · 192 communities (139 shown, 53 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 295 edges (avg confidence: 0.81)
- Token cost: host-agent extraction · usage unavailable

## Community Hubs (Navigation)
- Dynamic GLTF Import
- GLTF Authoring Metadata
- Cross-Module Core Contracts
- Dynamic Effect Semantics
- PAR Entity Models
- TEX Preview Resolution
- WD CLI Archive Operations
- Canonical Base Header Encoding
- MSH Decoding Pipeline
- PAR Entity Type Enums
- GLTF Export Diagnostics
- Canonical Static GLTF Import
- WD GUI Archive Workflows
- GLTF Import Contracts
- GLTF Transform Light Interchange
- GUI Value Converters
- MSH Preview Resolution
- WD Archive Documentation
- GLTF CLI Execution
- Safe MSH Operations
- PAR GUI File Workflow
- PAR Equipment Entities
- MSH Module Integration
- PAR Tree Navigation Models
- GLTF Document Validation
- PAR JSON Conversion CLI
- Dynamic Effect Preview Generation
- GLTF Mesh Import Operations
- GLTF Document Projection
- PAR GUI Entity Services
- Earth File Metadata
- Static Mesh Asset Model
- PAR Entity Validation
- TEX GUI Workflow
- WD Archive Extraction Services
- PAR Vehicle Entities
- Canonical MSH Serialization
- PAR File Persistence
- PAR Entity Detail Editing
- MSH Authoring Validation
- PAR Transporter Entities
- MSH GLTF CLI Commands
- Static Render Sequence Encoding
- MSH CLI Host Composition
- GLTF Import Plan Serialization
- PAR Property Editor Factory
- GLTF Animation Parsing
- GLTF Plan JSON Parsing
- GLTF CLI Report Serialization
- PAR Interactable Entities
- GLTF Package Binary Writing
- TEX CLI Conversion
- Static Animation Projection
- GLTF Attachment Transforms
- MSH Operation Profiles
- PAR Editable State Models
- PAR Undo Redo History
- PAR Scalar Property Editors
- Avalonia Application Bootstrapping
- WD Archive Data Sources
- PAR Entity Group Inspection
- PAR Equipable Position Entities
- MSH Binary Dump Tooling
- Avalonia View Location
- Dynamic MSH Binary Layout
- Static Mesh Builders
- Dynamic GLTF Package Generation
- PAR Destructible Effect Entities
- PAR Research Reference Editor
- Shared GUI About Models
- Dynamic GLTF Binary Writing
- PAR Passive Artifact Entities
- WD CLI Extraction
- GUI Main Window Interactions
- GLTF CLI Report Filesystem
- Static Mesh Geometry Validation
- Dynamic Object Animation Tracks
- PAR GUI Project Dependencies
- TEX GUI Project Dependencies
- WD GUI Project Dependencies
- PAR Binary Reading
- WD Archive Decompression
- GLTF Transactional Export
- TEX File Image Model
- WD Archive Factory Mapping
- Common GUI Service Registration
- TEX GUI View Location
- Common Dialog Contracts
- Avalonia Dialog Implementation
- PAR Binary Extensions
- WD Archive Information Model
- PAR Entity JSON Serialization
- MSH Format Documentation
- CLI Command Type Registration
- Common GUI Project Dependencies
- GLTF Accessor Decoding
- PAR Flags Property Editor
- WD Tree Expansion State
- Core GLTF Project Dependencies
- PAR Enum Property Editor
- WD Archive Domain Model
- Message Box Button Composition
- Common CLI Command Workflow
- CLI Project Dependencies
- WD Memory Mapped Data
- Contribution Testing Process
- Static MSH Vertex Layout
- Solution Project Structure
- GUI Notification Service
- WD Archive Creation Factory
- WD Archive Compression Pipeline
- Common MSH Base Header
- MSH Project Dependencies
- Static MSH Size Calculations
- MSH Transactional File System
- TEX Project Dependencies
- Architecture and COLLADA Migration
- WD Text File Flags
- Git Versioning Policy
- Canonical MSH Regeneration
- MSH Public API Documentation
- GLTF Facade Safety
- Blender GLTF Round Trip
- Static MSH Triangle Layout
- WD Archive Format Commands
- GUI Notification Events
- WD Extraction Contracts
- PAR Project Dependencies
- WD Project Dependencies
- WD GUI Bootstrapping
- WD Compression Service
- Source-Free GLTF Creation
- Static Assembly Input Model
- EarthTool Contribution Workflow
- Common GUI About View
- MSH Qualification Performance
- Dynamic Effect Alpha Range
- Dynamic Animation Frame Timing
- Static Record Continuation Framing
- EarthTool Coding Standards
- Dynamic Child Translation Range
- Dynamic Effect Rectangle Range
- Dynamic Model Scale Range
- Dynamic Mesh Name Field
- Dynamic Sprite Column Layout
- Dynamic Sprite Row Layout
- Dynamic Light Overlap Storage
- Dynamic Terrain Light Color
- Dynamic Texture Path Field
- Static Animation Scale Vectors
- Static Animation Transform Matrices
- Static Animation Translation Vectors
- Static Texture Path Field
- PAR Capability Hierarchy
- Static Source Builder
- Static Mesh Hierarchy
- CLI Runtime Requirements
- Dynamic Additive Effect Flag
- Dynamic Attachment Table
- Dynamic Effect Color
- Dynamic Effect Type Field
- Dynamic Effect Depth Offset
- Dynamic Mesh Horizontal Extents
- Dynamic Light Type Field
- Dynamic Reserved Zero Field
- Dynamic Ribbon Half Width
- Static Box Presence Mask
- Static Coverage Bitmap Layout
- Static Occupancy Descriptors
- Static Animation Frame Counts
- Static Animation Lengths
- Static Animation Type Field
- Static Attachment Table
- Static Barrel Angle Field
- Static Box Flag Array
- Static Box Height Array
- Static Mesh Horizontal Extents
- Static MSH Magic Field
- Static MSH Binary Layout
- Static Object Flags Field
- Static Object Pivot Field
- Static Raw GUID Field
- Static Remaining Light Storage
- Static Spotlight Cannon Overlap
- Static Archive Framing Marker
- Static Mesh Kind Field
- Static Hierarchy Unwind Count
- Static Format Version Field
- WD GUI Manual Tests

## God Nodes (most connected - your core abstractions)
1. `GltfInterchange` - 112 edges
2. `GlbDocument` - 96 edges
3. `EarthTool.PAR.Enums` - 83 edges
4. `OperationDiagnostic` - 78 edges
5. `OperationResult` - 60 edges
6. `DynamicGltfDocument` - 58 edges
7. `CanonicalAuthoringMetadata` - 56 edges
8. `EarthTool.PAR.Models` - 56 edges
9. `EarthTool.Common.Interfaces` - 53 edges
10. `MainWindowViewModel` - 51 edges

## Surprising Connections (you probably didn't know these)
- `WD Archive Tools` --semantically_similar_to--> `WD Archive Management`  [INFERRED] [semantically similar]
  README.md → EarthTool.WD.GUI/README.md
- `EarthTool Common` --semantically_similar_to--> `EarthTool Common`  [INFERRED] [semantically similar]
  README.md → EarthTool.WD.GUI/README.md
- `GltfCommandExecutor` --references--> `GltfInterchange`  [EXTRACTED]
  EarthTool.CLI/Commands/MSH/GltfCommandExecutor.cs → EarthTool.GLTF/GltfInterchange.cs
- `GltfCommandExecutor` --references--> `GltfCliReportSerializer`  [EXTRACTED]
  EarthTool.CLI/Commands/MSH/GltfCommandExecutor.cs → EarthTool.GLTF/GltfPlanAndReport.cs
- `GltfCommandExecutor` --references--> `GltfImportPlanSerializer`  [EXTRACTED]
  EarthTool.CLI/Commands/MSH/GltfCommandExecutor.cs → EarthTool.GLTF/GltfPlanAndReport.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **EarthTool WD GUI Architectural Patterns** — earthtool_wd_gui_readme_mvvm, earthtool_wd_gui_readme_dependency_injection, earthtool_wd_gui_readme_command_pattern, earthtool_wd_gui_readme_observer_pattern [EXTRACTED 1.00]
- **GitVersion Branch Version Policies** — gitversion_main_branch_policy, gitversion_development_branch_policy, gitversion_feature_branch_policy, gitversion_release_branch_policy, gitversion_hotfix_branch_policy [EXTRACTED 1.00]
- **EarthTool Tool Suite Modules** — readme_wd_archive_tools, readme_tex_texture_tools, readme_msh_model_tools, readme_gltf_interchange, readme_par_parameter_tools, readme_earthtool_cli, readme_earthtool_wd_gui, readme_earthtool_par_gui [EXTRACTED 1.00]
- **MSH Serialized Representation System** — docs_msh_format_common_base_header, docs_msh_format_independent_footprint_representations, docs_msh_format_static_render_object_sequence, docs_msh_format_hierarchy_unwind_encoding, docs_msh_format_dynamic_effect_extension [EXTRACTED 1.00]
- **Canonical glTF Creation Flow** — docs_api_gltf_gltf_interchange_facade, docs_api_gltf_canonical_creation_contract, docs_api_gltf_earthtool_authoring_envelope, docs_api_gltf_resource_binding_authority, docs_api_msh_immutable_mesh_asset_model [EXTRACTED 1.00]
- **Blender Artist Interchange Workflow** — docs_mesh_artist_quickstart_blender_artist_workflow, docs_mesh_artist_quickstart_required_blender_settings, docs_mesh_artist_quickstart_canonical_attachment_identifiers, docs_api_gltf_earthtool_authoring_envelope, docs_api_gltf_canonical_creation_contract [EXTRACTED 1.00]
- **MESH Base Header Fields** — docs_msh_dynamic_bytefield_mesh_magic, docs_msh_dynamic_bytefield_version, docs_msh_dynamic_bytefield_mesh_kind, docs_msh_dynamic_bytefield_box_presence_mask, docs_msh_dynamic_bytefield_animation_lengths, docs_msh_dynamic_bytefield_animation_frames [INFERRED 0.85]
- **Dynamic Effect Parameter Block** — docs_msh_dynamic_bytefield_effect_type, docs_msh_dynamic_bytefield_light_type, docs_msh_dynamic_bytefield_first_source_frame, docs_msh_dynamic_bytefield_frame_count, docs_msh_dynamic_bytefield_sprite_columns, docs_msh_dynamic_bytefield_sprite_rows, docs_msh_dynamic_bytefield_frame_period, docs_msh_dynamic_bytefield_reciprocal_columns, docs_msh_dynamic_bytefield_reciprocal_rows, docs_msh_dynamic_bytefield_start_effect_rectangle, docs_msh_dynamic_bytefield_end_effect_rectangle, docs_msh_dynamic_bytefield_effect_z_offset, docs_msh_dynamic_bytefield_signed_ribbon_half_width, docs_msh_dynamic_bytefield_reserved_zero, docs_msh_dynamic_bytefield_additive_flag, docs_msh_dynamic_bytefield_scaled_terrain_light_rgb, docs_msh_dynamic_bytefield_color_rgb, docs_msh_dynamic_bytefield_visible_terrain_light_gain, docs_msh_dynamic_bytefield_alpha_mode, docs_msh_dynamic_bytefield_end_alpha, docs_msh_dynamic_bytefield_start_alpha, docs_msh_dynamic_bytefield_end_model_scale, docs_msh_dynamic_bytefield_start_model_scale, docs_msh_dynamic_bytefield_child_start_translation, docs_msh_dynamic_bytefield_child_end_translation [EXTRACTED 1.00]
- **Variable-length Dynamic Mesh Tail** — docs_msh_dynamic_bytefield_mesh_name_length_a, docs_msh_dynamic_bytefield_mesh_name, docs_msh_dynamic_bytefield_texture_path_length_b, docs_msh_dynamic_bytefield_texture_path, docs_msh_dynamic_bytefield_dynamic_child_count_c, docs_msh_dynamic_bytefield_dynamicobject_array [EXTRACTED 1.00]
- **Static MSH Header** — docs_msh_static_bytefield_msh_static_binary_layout, docs_msh_static_bytefield_static_framing, docs_msh_static_bytefield_raw_windows_guid, docs_msh_static_bytefield_mesh_magic, docs_msh_static_bytefield_version_1, docs_msh_static_bytefield_static_mesh_kind, docs_msh_static_bytefield_4x4_box_presence_mask, docs_msh_static_bytefield_animation_lengths, docs_msh_static_bytefield_animation_frames [EXTRACTED 1.00]
- **Fixed Spatial and Attachment Storage** — docs_msh_static_bytefield_msh_static_binary_layout, docs_msh_static_bytefield_spot_light_cannon_overlap_area, docs_msh_static_bytefield_remaining_light_storage_overlap, docs_msh_static_bytefield_box_heights, docs_msh_static_bytefield_box_flags, docs_msh_static_bytefield_4x4_occupancy_descriptors, docs_msh_static_bytefield_4x4_coverage_bitmaps, docs_msh_static_bytefield_attachments_1_49, docs_msh_static_bytefield_extents, docs_msh_static_bytefield_trailing_hierarchy_unwind_count [EXTRACTED 1.00]
- **Variable StaticObject Payload** — docs_msh_static_bytefield_msh_static_binary_layout, docs_msh_static_bytefield_render_vertex_count, docs_msh_static_bytefield_vertex_block_count, docs_msh_static_bytefield_vertex_block_array, docs_msh_static_bytefield_object_flags, docs_msh_static_bytefield_texture_path_length, docs_msh_static_bytefield_texture_path, docs_msh_static_bytefield_triangle_count, docs_msh_static_bytefield_triangle_array, docs_msh_static_bytefield_scale_count, docs_msh_static_bytefield_baked_tcbscale_vectors, docs_msh_static_bytefield_translation_count, docs_msh_static_bytefield_baked_translation_vectors, docs_msh_static_bytefield_matrix_count, docs_msh_static_bytefield_baked_transform_matrices, docs_msh_static_bytefield_animation_type, docs_msh_static_bytefield_object_pivot, docs_msh_static_bytefield_barrel_angle, docs_msh_static_bytefield_next_record_heap_pointer [EXTRACTED 1.00]

## Communities (192 total, 53 thin omitted)

### Community 0 - "Dynamic GLTF Import"
Cohesion: 0.08
Nodes (32): CanonicalDynamicGraph, CanonicalDynamicNode, CanonicalDynamicPreview, CancellationToken, GltfNewModelImportOptions, GltfOperationProfile, Guid, ICollection (+24 more)

### Community 1 - "GLTF Authoring Metadata"
Cohesion: 0.08
Nodes (29): Carrier, bool, GltfOperationProfile, GltfStaticObjectRoles, HashSet, IEnumerable, int, IReadOnlyDictionary (+21 more)

### Community 2 - "Cross-Module Core Contracts"
Cohesion: 0.04
Nodes (36): EarthTool.WD.GUI.ViewModels, EarthTool.CLI.Commands.PAR, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.TEX, EarthTool.PAR, EarthTool.PAR.Services, EarthTool.Common (+28 more)

### Community 3 - "Dynamic Effect Semantics"
Cohesion: 0.07
Nodes (24): DiagnosticSeverity, Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, ReadOnlySpan (+16 more)

### Community 4 - "PAR Entity Models"
Cohesion: 0.05
Nodes (34): EarthTool.PAR.Extensions, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Factories, IEnumerable, DestructibleEntity, Encoding, IEnumerable, TypelessEntity (+26 more)

### Community 5 - "TEX Preview Resolution"
Cohesion: 0.07
Nodes (25): Func, IEnumerable, IReadOnlyList, SafeResourceLookup, SafeResourceMatch, BinaryReader, byte, Exception (+17 more)

### Community 6 - "WD CLI Archive Operations"
Cohesion: 0.06
Nodes (33): Command, EarthTool.CLI.Commands.WD, CancellationToken, CommandContext, AddCommand, CancellationToken, CommandContext, CreateCommand (+25 more)

### Community 7 - "Canonical Base Header Encoding"
Cohesion: 0.10
Nodes (22): CornerPassageMaps, Exception, MetadataResourceLimitException, RequiredTextureResourceBindingException, ResourceLimitException, StaticLightMetadataException, CanonicalStaticFootprint, byte (+14 more)

### Community 8 - "MSH Decoding Pipeline"
Cohesion: 0.13
Nodes (18): DecodedStaticRecord, MeshAssetOrigin, int, DynamicMeshDecoder, CancellationToken, IReadOnlyDictionary, IReadOnlyList, List (+10 more)

### Community 9 - "PAR Entity Type Enums"
Cohesion: 0.06
Nodes (23): EarthTool.PAR.Enums, BarrelBetaType, BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, DamageFlags, ExplosionFlags (+15 more)

### Community 10 - "GLTF Export Diagnostics"
Cohesion: 0.11
Nodes (13): IReadOnlyDictionary, OperationDiagnostic, CancellationToken, GltfExportOptions, IReadOnlyDictionary, DynamicTexPreviewLoadResult, TexPreviewLoadResult, Action (+5 more)

### Community 11 - "Canonical Static GLTF Import"
Cohesion: 0.09
Nodes (16): Guid, IDictionary, IReadOnlyList, ISet, List, Path, NewModelAnimationSet, NewModelAnimationTrack (+8 more)

### Community 12 - "WD GUI Archive Workflows"
Cohesion: 0.12
Nodes (10): ITextFlagService, bool, ILogger, object, ObservableCollection, ReactiveCommand, string, Task (+2 more)

### Community 13 - "GLTF Import Contracts"
Cohesion: 0.09
Nodes (17): IReadOnlyDictionary, IReadOnlyList, string, GltfDiagnosticCodes, GltfExportOptions, GltfLightHandle, GltfMaterialHandle, GltfMeshResourceLimits (+9 more)

### Community 14 - "GLTF Transform Light Interchange"
Cohesion: 0.10
Nodes (18): GltfNewModelStaticLightOptions, Func, ICollection, IEnumerable, IReadOnlyCollection, IReadOnlyDictionary, Matrix4x4, Quaternion (+10 more)

### Community 15 - "GUI Value Converters"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 16 - "MSH Preview Resolution"
Cohesion: 0.11
Nodes (18): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+10 more)

### Community 17 - "WD Archive Documentation"
Cohesion: 0.06
Nodes (39): Centralized Asynchronous Error Handling, Avalonia UI, Reactive Command Pattern, Dependency Injection, DialogService, Earth 2150, EarthTool Common, EarthTool WD Backend (+31 more)

### Community 18 - "GLTF CLI Execution"
Cohesion: 0.14
Nodes (13): CancellationToken, IEnumerable, IReadOnlyList, Task, TextWriter, GltfCommandExecutor, OperationStatus, GltfCliReportOperation (+5 more)

### Community 19 - "Safe MSH Operations"
Cohesion: 0.14
Nodes (13): IReadOnlyList, OperationResult, MeshAsset, ITransactionalFileSystem, CancellationToken, Exception, IEnumerable, ILogger (+5 more)

### Community 20 - "PAR GUI File Workflow"
Cohesion: 0.11
Nodes (8): bool, ILogger, ObservableCollection, ReactiveCommand, string, Task, Unit, MainWindowViewModel

### Community 21 - "PAR Equipment Entities"
Cohesion: 0.07
Nodes (19): ConnectorType, LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable (+11 more)

### Community 22 - "MSH Module Integration"
Cohesion: 0.13
Nodes (16): EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF.Internal, EarthTool.MSH.Assets, EarthTool.MSH.Expert, EarthTool.Common.Operations (+8 more)

### Community 23 - "PAR Tree Navigation Models"
Cohesion: 0.09
Nodes (24): EntityClassType, Faction, ResearchType, ObservableCollection, EntityGroupNodeViewModel, ObservableCollection, EntityGroupsRootNodeViewModel, ObservableCollection (+16 more)

### Community 24 - "GLTF Document Validation"
Cohesion: 0.15
Nodes (5): GltfOperationProfile, JsonElement, Matrix4x4, GltfImportIntent, JsonDocument

### Community 25 - "PAR JSON Conversion CLI"
Cohesion: 0.11
Nodes (17): CommonCommand, CommonSettings, IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand (+9 more)

### Community 26 - "Dynamic Effect Preview Generation"
Cohesion: 0.15
Nodes (11): DynamicEffectPreview, float, int, uint, Vector2, Vector3, DynamicAnimationTrack, DynamicEffectPreview (+3 more)

### Community 27 - "GLTF Mesh Import Operations"
Cohesion: 0.20
Nodes (10): GltfOperationProfile, CancellationToken, Stream, Task, GltfNewModelImportOptions, Guid, IReadOnlyDictionary, CanonicalStaticGltfCreationOptions (+2 more)

### Community 28 - "GLTF Document Projection"
Cohesion: 0.13
Nodes (9): IEnumerable, Span, uint, Utf8JsonWriter, GlbDocument, PreviewLayout, EmitterActive, MarkerRecordCount (+1 more)

### Community 29 - "PAR GUI Entity Services"
Cohesion: 0.14
Nodes (7): EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, EarthTool.PAR.Models, ValidationError, ValidationSeverity

### Community 30 - "Earth File Metadata"
Cohesion: 0.11
Nodes (15): FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo (+7 more)

### Community 31 - "Static Mesh Asset Model"
Cohesion: 0.12
Nodes (16): ProjectedPartition, byte, IReadOnlyList, Matrix4x4, Vector2, Vector3, RenderVertex, StaticAnimationClass (+8 more)

### Community 32 - "PAR Entity Validation"
Cohesion: 0.14
Nodes (13): EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List, ValidationResult, IEnumerable, ILogger (+5 more)

### Community 33 - "TEX GUI Workflow"
Cohesion: 0.12
Nodes (13): Bitmap, INotificationService, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap (+5 more)

### Community 34 - "WD Archive Extraction Services"
Cohesion: 0.12
Nodes (11): DateTime, Encoding, IReadOnlyCollection, IArchive, ReadOnlyMemory, IArchiveItem, PathValidator, Encoding (+3 more)

### Community 35 - "PAR Vehicle Entities"
Cohesion: 0.09
Nodes (16): VehicleObjectType, Encoding, IEnumerable, Builder, Encoding, IEnumerable, Harvester, Encoding (+8 more)

### Community 36 - "Canonical MSH Serialization"
Cohesion: 0.14
Nodes (9): Guid, MeshArchiveFraming, Encoding, Guid, int, IReadOnlyList, ReadOnlySpan, Vector3 (+1 more)

### Community 37 - "PAR File Persistence"
Cohesion: 0.13
Nodes (11): Writer, Task, IParFileService, ILogger, Task, ParFileService, Encoding, IEnumerable (+3 more)

### Community 38 - "PAR Entity Detail Editing"
Cohesion: 0.14
Nodes (11): Action, bool, IEnumerable, ILogger, ObservableCollection, ReactiveCommand, string, Type (+3 more)

### Community 39 - "MSH Authoring Validation"
Cohesion: 0.18
Nodes (6): HashSet, List, AuthoringValidation, MshBuildResult, Matrix4x4, CanonicalStaticMeshAssembler

### Community 40 - "PAR Transporter Entities"
Cohesion: 0.09
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 41 - "MSH GLTF CLI Commands"
Cohesion: 0.13
Nodes (14): AsyncCommand, EarthTool.GLTF, EarthTool.CLI.Commands.MSH, EarthTool.MSH, CancellationToken, CommandContext, Task, ExportGltfCommand (+6 more)

### Community 42 - "Static Render Sequence Encoding"
Cohesion: 0.22
Nodes (9): CanonicalStaticRecord, StaticAnimationReplacement, ICollection, int, IReadOnlyDictionary, IReadOnlyList, Matrix4x4, Vector3 (+1 more)

### Community 43 - "MSH CLI Host Composition"
Cohesion: 0.09
Nodes (17): Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter, CliExitCode (+9 more)

### Community 44 - "GLTF Import Plan Serialization"
Cohesion: 0.16
Nodes (9): CancellationToken, IReadOnlyDictionary, Stream, Task, GltfImportPlan, ImportPlanException, UnsupportedGltfDomainException, MetadataElementLimitException (+1 more)

### Community 45 - "PAR Property Editor Factory"
Cohesion: 0.14
Nodes (16): Action, IEnumerable, IPropertyEditorFactory, Action, HashSet, IEnumerable, ILogger, Type (+8 more)

### Community 46 - "GLTF Animation Parsing"
Cohesion: 0.15
Nodes (13): AnimationObjectLayout, int, IReadOnlyList, string, AnimationLayout, ParsedAnimationBuilder, ParsedGltfAnimation, ParsedGltfAnimationChannel (+5 more)

### Community 47 - "GLTF Plan JSON Parsing"
Cohesion: 0.35
Nodes (4): JsonElement, GltfImportPlanSerializer, ImportPlanException, JsonValueKind

### Community 48 - "GLTF CLI Report Serialization"
Cohesion: 0.17
Nodes (11): Guid, IEnumerable, int, IReadOnlyList, string, Utf8JsonWriter, GltfCliReport, GltfCliReportFormat (+3 more)

### Community 49 - "PAR Interactable Entities"
Cohesion: 0.10
Nodes (12): ShadowType, Encoding, IEnumerable, InteractableEntity, Encoding, IEnumerable, TypedEntity, Encoding (+4 more)

### Community 50 - "GLTF Package Binary Writing"
Cohesion: 0.15
Nodes (10): AnimationLayout, BinaryWriter, IDictionary, IReadOnlyDictionary, GltfPackage, PartitionLayout, MemoryStream, PartitionLayout (+2 more)

### Community 51 - "TEX CLI Conversion"
Cohesion: 0.17
Nodes (12): IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, IReader, IEnumerable, TexHeader (+4 more)

### Community 52 - "Static Animation Projection"
Cohesion: 0.25
Nodes (10): IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionSet, ProjectedAnimationClip, ProjectedAnimationFrame, ProjectedAnimationObject (+2 more)

### Community 53 - "GLTF Attachment Transforms"
Cohesion: 0.17
Nodes (10): float, Quaternion, Translation, Vector3, AttachmentHeadingProjection, ProjectedAttachment, ProjectedCannon, ProjectedStaticLight (+2 more)

### Community 54 - "MSH Operation Profiles"
Cohesion: 0.19
Nodes (9): IEnumerable, MshExpert, CancellationToken, Stream, Task, IMshReader, IMshValidator, IMshWriter (+1 more)

### Community 55 - "PAR Editable State Models"
Cohesion: 0.12
Nodes (12): bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, bool, FlagValueViewModel (+4 more)

### Community 56 - "PAR Undo Redo History"
Cohesion: 0.12
Nodes (10): Action, DateTime, UndoAction, IEnumerable, Action, IEnumerable, ILogger, int (+2 more)

### Community 57 - "PAR Scalar Property Editors"
Cohesion: 0.11
Nodes (13): Action, IUndoRedoService, IEnumerable, string, IntCollectionPropertyEditorViewModel, int, string, IntPropertyEditorViewModel (+5 more)

### Community 58 - "Avalonia Application Bootstrapping"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 59 - "WD Archive Data Sources"
Cohesion: 0.11
Nodes (12): Type, CommandTypeResolver, ReadOnlyMemory, IArchiveDataSource, bool, ReadOnlyMemory, ArchiveItem, ReadOnlyMemory (+4 more)

### Community 60 - "PAR Entity Group Inspection"
Cohesion: 0.13
Nodes (10): CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings, Encoding, IBinarySerializable, Encoding (+2 more)

### Community 61 - "PAR Equipable Position Entities"
Cohesion: 0.12
Nodes (10): MaxShieldUpgradeType, PositionType, Encoding, IEnumerable, EquipableEntity, Encoding, Platoon, Encoding (+2 more)

### Community 62 - "MSH Binary Dump Tooling"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 63 - "Avalonia View Location"
Cohesion: 0.12
Nodes (9): EarthTool.PAR.GUI, AppBuilder, STAThread, Program, Control, ViewLocator, Control, ViewLocator (+1 more)

### Community 64 - "Dynamic MSH Binary Layout"
Cohesion: 0.12
Nodes (17): Animation Frames, Animation Lengths, Archive Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors (+9 more)

### Community 65 - "Static Mesh Builders"
Cohesion: 0.18
Nodes (7): Guid, CanonicalHorizontalExtents, CanonicalStaticObjectRole, CanonicalStaticSourceObject, DynamicMeshBuilder, StaticMeshBuilder, CanonicalStaticRecord

### Community 66 - "Dynamic GLTF Package Generation"
Cohesion: 0.26
Nodes (5): GltfOperationProfile, IReadOnlyDictionary, ISet, JsonElement, TexPreview

### Community 67 - "PAR Destructible Effect Entities"
Cohesion: 0.12
Nodes (9): WasteSize, Encoding, Encoding, Encoding, IEnumerable, FlyingWaste, Encoding, Encoding (+1 more)

### Community 68 - "PAR Research Reference Editor"
Cohesion: 0.20
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 69 - "Shared GUI About Models"
Cohesion: 0.17
Nodes (9): EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, ReactiveCommand, Unit, AboutViewModel, ViewModelBase, ParAboutViewModel, TexAboutViewModel (+1 more)

### Community 70 - "Dynamic GLTF Binary Writing"
Cohesion: 0.17
Nodes (6): DynamicAnimationLayout, DynamicImageLayout, DynamicMeshLayout, BinaryWriter, Stream, Utf8JsonWriter

### Community 71 - "PAR Passive Artifact Entities"
Cohesion: 0.13
Nodes (9): ArtifactType, PassiveMask, Encoding, IEnumerable, PassiveEntity, Encoding, IEnumerable, Artifact (+1 more)

### Community 72 - "WD CLI Extraction"
Cohesion: 0.17
Nodes (11): CommandSettings, CommonSettings, CancellationToken, CommandContext, List, ExtractCommand, ExtractSettings, WdMultiSettings (+3 more)

### Community 73 - "GUI Main Window Interactions"
Cohesion: 0.15
Nodes (9): EarthTool.TEX.GUI.Views, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs, RoutedEventArgs (+1 more)

### Community 74 - "GLTF CLI Report Filesystem"
Cohesion: 0.17
Nodes (4): Stream, CliReportFileSystem, ICliReportFileSystem, Exception

### Community 75 - "Static Mesh Geometry Validation"
Cohesion: 0.20
Nodes (6): IEnumerable, IReadOnlyList, Vector2, Vector3, CanonicalStaticVertex, CanonicalTriangle

### Community 76 - "Dynamic Object Animation Tracks"
Cohesion: 0.20
Nodes (8): DynamicAnimationTrack, DynamicObjectScope, ICollection, IEnumerable, IReadOnlyList, DynamicAnimationLayout, DynamicObjectScope, DynamicObject

### Community 77 - "PAR GUI Project Dependencies"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 78 - "TEX GUI Project Dependencies"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 79 - "WD GUI Project Dependencies"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 80 - "PAR Binary Reading"
Cohesion: 0.26
Nodes (6): Reader, FileType, BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 81 - "WD Archive Decompression"
Cohesion: 0.19
Nodes (7): ReadOnlySpan, Stream, IDecompressor, ILogger, ReadOnlySpan, Stream, DecompressorService

### Community 82 - "GLTF Transactional Export"
Cohesion: 0.22
Nodes (3): Stream, ITransactionalFileSystem, TransactionalFileSystem

### Community 83 - "TEX File Image Model"
Cohesion: 0.24
Nodes (8): BinaryReader, IEnumerable, TexFile, TexHeader, BinaryReader, IEnumerable, SKBitmap, TexImage

### Community 84 - "WD Archive Factory Mapping"
Cohesion: 0.26
Nodes (7): BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory

### Community 85 - "Common GUI Service Registration"
Cohesion: 0.26
Nodes (5): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, IServiceCollection, ServiceCollectionExtensions

### Community 86 - "TEX GUI View Location"
Cohesion: 0.18
Nodes (6): EarthTool.TEX.GUI, AppBuilder, STAThread, Program, Control, ViewLocator

### Community 87 - "Common Dialog Contracts"
Cohesion: 0.24
Nodes (3): IEnumerable, Task, IDialogService

### Community 88 - "Avalonia Dialog Implementation"
Cohesion: 0.36
Nodes (4): IEnumerable, ILogger, Task, DialogService

### Community 89 - "PAR Binary Extensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 90 - "WD Archive Information Model"
Cohesion: 0.20
Nodes (6): DateTime, int, long, string, ArchiveInfoViewModel, ViewModelBase

### Community 91 - "PAR Entity JSON Serialization"
Cohesion: 0.20
Nodes (8): EarthTool.PAR.Models.Serialization, JsonSerializerOptions, Type, Utf8JsonWriter, EntityConverter, TypeReader, JsonConverter, Utf8JsonReader

### Community 92 - "MSH Format Documentation"
Cohesion: 0.22
Nodes (11): MSH Creation GUID Lifecycle, Explicit MSH Framing and Extension Model, Bounded MSH Operations, SharpGLTF Toolkit Topology Incompatibility, Common MSH Base Header, Dynamic Effect Extension, Static Hierarchy Unwind Encoding, Independent Serialized Footprint Representations (+3 more)

### Community 93 - "CLI Command Type Registration"
Cohesion: 0.22
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 94 - "Common GUI Project Dependencies"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 96 - "PAR Flags Property Editor"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 97 - "WD Tree Expansion State"
Cohesion: 0.24
Nodes (5): HashSet, bool, Guid, ObservableCollection, TreeItemViewModel

### Community 98 - "Core GLTF Project Dependencies"
Cohesion: 0.20
Nodes (10): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.GLTF, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions (+2 more)

### Community 99 - "PAR Enum Property Editor"
Cohesion: 0.29
Nodes (5): object, ObservableCollection, Type, EnumPropertyEditorViewModel, EnumValueViewModel

### Community 100 - "WD Archive Domain Model"
Cohesion: 0.22
Nodes (6): bool, DateTime, IReadOnlyCollection, MemoryMappedFile, Archive, SortedSet

### Community 101 - "Message Box Button Composition"
Cohesion: 0.25
Nodes (5): Button, MessageBoxResult, MessageBoxType, Window, Panel

### Community 102 - "Common CLI Command Workflow"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 103 - "CLI Project Dependencies"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 104 - "WD Memory Mapped Data"
Cohesion: 0.25
Nodes (6): int, MemoryMappedFile, ReadOnlyMemory, MappedArchiveDataSource, Lazy, MemoryMappedViewAccessor

### Community 105 - "Contribution Testing Process"
Cohesion: 0.25
Nodes (8): Arrange Act Assert Test Pattern, Automated Pull Request Checks, AwesomeAssertions, Conventional Commits, Documentation Maintenance, Pull Request Process, Testing Requirements, xUnit

### Community 106 - "Static MSH Vertex Layout"
Cohesion: 0.25
Nodes (8): Render Vertex Count V (u32), Reserved W = 0, VertexBlock[B] (B x 0xA0 bytes), Vertex Block Count B = ceil(V/4) (u32), Vertex Normal, Vertex Sharing Links, Vertex UV, Vertex XYZ

### Community 107 - "Solution Project Structure"
Cohesion: 0.25
Nodes (6): EarthTool.CLI.Tests, EarthTool.Consumer.Tests, EarthTool.MSH.Tests, EarthTool.PAR.Tests, EarthTool.TEX.Tests, EarthTool.WD.Tests

### Community 108 - "GUI Notification Service"
Cohesion: 0.39
Nodes (3): Exception, ILogger, NotificationService

### Community 109 - "WD Archive Creation Factory"
Cohesion: 0.25
Nodes (5): DateTime, Guid, IArchiveFactory, DateTime, Guid

### Community 110 - "WD Archive Compression Pipeline"
Cohesion: 0.36
Nodes (3): Stream, ICompressor, Encoding

### Community 111 - "Common MSH Base Header"
Cohesion: 0.32
Nodes (5): byte, int, IReadOnlyList, ReadOnlySpan, CommonMeshBaseHeader

### Community 112 - "MSH Project Dependencies"
Cohesion: 0.25
Nodes (8): EarthTool.MSH, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, System.Text.Json, Microsoft.NET.Sdk, System.ComponentModel

### Community 113 - "Static MSH Size Calculations"
Cohesion: 0.25
Nodes (6): IEnumerable, TriangleCount, VertexCount, IEnumerable, TriangleCount, VertexCount

### Community 115 - "TEX Project Dependencies"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 116 - "Architecture and COLLADA Migration"
Cohesion: 0.33
Nodes (7): earthtoolAuthoring Envelope, EarthTool Format Module Architecture, Canonical Attachment Identifiers, COLLADA Removal Breaking Change, COLLADA-to-glTF Migration Workflow, EarthTool Dependency Direction, EarthTool Project Module Structure

### Community 118 - "Git Versioning Policy"
Cohesion: 0.29
Nodes (7): Continuous Deployment Versioning, Development Branch Version Policy, Feature Branch Version Policy, Hotfix Branch Version Policy, Main Branch Version Policy, Release Branch Version Policy, Semantic Versioning Configuration

### Community 119 - "Canonical MSH Regeneration"
Cohesion: 0.33
Nodes (6): Immutable MSH Conversion Substrate, Metadata Conflict Fallback, Canonical Whole-Asset Regeneration, Deterministic Canonical MSH Output, Typed Authoring Metadata Scope, Source Restoration Contract Removal

### Community 120 - "MSH Public API Documentation"
Cohesion: 0.40
Nodes (6): Dynamic Effect Preview Contract, Canonical and Expert MSH Authoring, Immutable MeshAsset Model, EarthTool Installation Methods, EarthTool Supported Workflows, EarthTool Documentation Index

### Community 121 - "GLTF Facade Safety"
Cohesion: 0.40
Nodes (6): GltfInterchange Facade, Transactional glTF Package Writing, Transactional CLI Boundary, SharpGLTF Package API Isolation, SharpGLTF.Core 1.0.6 Selection, EarthTool Operation Safety Model

### Community 122 - "Blender GLTF Round Trip"
Cohesion: 0.33
Nodes (6): EarthTool Blender-Safe Metadata Carrier, Blender 4.5.12 Mixed-Arity Attribute Import Defect, Native glTF Artist Data, Stock Blender Semantic Round-Trip Contract, Topology Fingerprint Validation, Required Blender glTF Settings

### Community 123 - "Static MSH Triangle Layout"
Cohesion: 0.33
Nodes (6): Base Flag Bit 0, Triangle[T], Triangle Count T (u32), Triangle Flags (u16), Three u16 Triangle Indices, Upward Flag Bit 1

### Community 124 - "WD Archive Format Commands"
Cohesion: 0.33
Nodes (6): WD Archive Quick Start Workflow, WD Archive Command Set, WD Command Safety and Compression Policy, WD Archive Layout, WD Compressed Central Directory, Whole-Archive Memory Loading

### Community 125 - "GUI Notification Events"
Cohesion: 0.33
Nodes (4): NotificationType, Exception, NotificationEventArgs, EventArgs

### Community 126 - "WD Extraction Contracts"
Cohesion: 0.33
Nodes (3): Task, IExtractor, IWDExtractor

### Community 127 - "PAR Project Dependencies"
Cohesion: 0.33
Nodes (6): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk

### Community 128 - "WD Project Dependencies"
Cohesion: 0.33
Nodes (6): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk

### Community 129 - "WD GUI Bootstrapping"
Cohesion: 0.40
Nodes (3): AppBuilder, STAThread, Program

### Community 130 - "WD Compression Service"
Cohesion: 0.47
Nodes (3): ILogger, Stream, CompressorService

### Community 131 - "Source-Free GLTF Creation"
Cohesion: 0.50
Nodes (5): Source-Free Canonical glTF Creation Contract, Explicit TEX and MSH Resource Binding Authority, Blender Mesh Artist Workflow, OperationResult Mesh Creation API, MSH Export and Import Quick Start

### Community 132 - "Static Assembly Input Model"
Cohesion: 0.50
Nodes (4): Guid, IReadOnlyDictionary, Vector3, CanonicalStaticMeshAssemblyInput

### Community 133 - "EarthTool Contribution Workflow"
Cohesion: 0.50
Nodes (4): Bug Reporting Process, Development Setup, EarthTool Contribution Workflow, Enhancement Proposal

### Community 134 - "Common GUI About View"
Cohesion: 0.50
Nodes (3): EarthTool.Common.GUI.Views, AboutView, UserControl

### Community 135 - "MSH Qualification Performance"
Cohesion: 0.50
Nodes (4): EarthTool Interchange Boundary, Bounded Qualification Worker Queue, Deterministic Corpus Evidence Reduction, 12.74x Qualification Speedup

### Community 136 - "Dynamic Effect Alpha Range"
Cohesion: 1.00
Nodes (3): Alpha Mode, End Alpha, Start Alpha

### Community 137 - "Dynamic Animation Frame Timing"
Cohesion: 0.67
Nodes (3): First Source Frame, Frame Count, Frame Period

### Community 138 - "Static Record Continuation Framing"
Cohesion: 0.67
Nodes (3): End of File, Next-record Heap Pointer Marker (Boolean-only u32, Unaligned), Next Complete StaticObject

## Knowledge Gaps
- **228 isolated node(s):** `EarthTool.CLI.Commands.TEX`, `net8.0`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Logging.Console` (+223 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **53 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.TEX` connect `Cross-Module Core Contracts` to `GLTF Export Diagnostics`, `TEX File Image Model`?**
  _High betweenness centrality (0.284) - this node is a cross-community bridge._
- **Why does `EarthTool.Common.Interfaces` connect `Cross-Module Core Contracts` to `WD Archive Extraction Services`, `WD Compression Service`, `PAR Entity Models`, `Shared GUI About Models`, `WD CLI Archive Operations`, `WD Archive Creation Factory`, `WD Archive Compression Pipeline`, `WD Archive Decompression`, `Common GUI Service Registration`, `PAR JSON Conversion CLI`, `WD Extraction Contracts`, `PAR Entity Group Inspection`, `PAR GUI Entity Services`, `Earth File Metadata`?**
  _High betweenness centrality (0.250) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `MSH Module Integration` to `Dynamic GLTF Import`, `GLTF Authoring Metadata`, `Static Mesh Builders`, `Dynamic Effect Semantics`, `Canonical Base Header Encoding`, `MSH GLTF CLI Commands`, `GLTF Export Diagnostics`, `GLTF Import Contracts`, `GLTF Animation Parsing`, `GLTF CLI Report Serialization`, `Static Animation Projection`, `Static Mesh Asset Model`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **What connects `EarthTool.CLI.Commands.TEX`, `net8.0`, `Microsoft.Extensions.Configuration` to the rest of the system?**
  _228 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Dynamic GLTF Import` be split into smaller, more focused modules?**
  _Cohesion score 0.08310540711129813 - nodes in this community are weakly interconnected._
- **Should `GLTF Authoring Metadata` be split into smaller, more focused modules?**
  _Cohesion score 0.07591553060678963 - nodes in this community are weakly interconnected._
- **Should `Cross-Module Core Contracts` be split into smaller, more focused modules?**
  _Cohesion score 0.03939808481532148 - nodes in this community are weakly interconnected._