# Graph Report - EarthTool  (2026-08-04)

## Corpus Check
- 364 files · ~293,746 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4678 nodes · 14080 edges · 178 communities (171 shown, 7 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 935 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `3ab3c231`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- MeshAsset
- Runner
- FramedMshBaseHeaderTests
- .Compress
- .CreateEffectPreview
- GltfWalkingSkeletonTests
- .ToByteArray
- .Load
- IValueConverter
- MainWindowViewModel
- OperationResult
- DynamicGltfInterchangeTests
- Vector3
- MshV1Decoder
- .OpenArchive
- DynamicEffectSemantics
- Dynamic MESH Binary Layout
- EarthTool.Common
- Equipment
- release-qualification.mjs
- MainWindowViewModel
- GltfImportPlanSerializer
- MainWindowViewModel
- .ExportGlbFileAsync
- Vehicle
- Common MSH Base Header
- MetadataGraphValidationTests
- .CreateMockHeader
- EarthTool.MSH.Assets
- .CreateJson
- EarthTool.CLI
- DynamicMeshAssetTests
- .Create
- .CreateCurrentStaticLightGuards
- AnimationClassBytes
- GltfContracts.cs
- StaticMeshAsset
- ArchiverServiceTests
- TreeItemViewModel
- IArchiveItem
- TexPreviewLoader
- .ExportGlbAsync
- Static Mesh Header
- StaticObject Record
- GltfInterchange
- StaticAnimationProjection
- EntityDetailsViewModel
- ParFile
- PublicApiApproval
- IEarthInfo
- .Create
- GlbDocument
- OfficialCorpusQualification
- IReadOnlyList
- MetadataEnvelope
- Entity
- EarthTool.MSH.Tests
- EditableEntity
- DialogService
- EarthTool.PAR
- DynamicGltfDocument
- EarthTool.PAR.Models.Abstracts
- .ToByteArray
- IUndoRedoService
- TexFile
- EarthTool.PAR.GUI
- MetadataConflictCollector
- DynamicEffectExtension
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- .ResolveAndLoad
- InteractableEntity
- Task
- .Decode
- AuthoringValidation
- IReadOnlyList
- EarthTool.sln
- EarthTool.Common.Interfaces
- ResearchReferenceCollectionEditorViewModel
- BinaryExtensions
- Fact
- .CreateJson
- Blender 4.5 glTF round-trip research
- EarthTool.PAR.Enums
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- UndoRedoService
- CommonCommand
- OneTriangleMshFixture
- EarthTool WD Archive Manager
- TreeNodeViewModelBase
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- MeshAssetAuthoring.cs
- PassiveEntity
- EarthTool.Common.GUI.Enums
- SourceObjectId
- EarthTool Suite
- WD Central Directory
- .WriteReconciledRecord
- EarthTool.CLI.Commands.WD
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- KhronosValidatorServer
- Reader
- .Resolve
- PropertyEditorViewModel
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- EnumPropertyEditorViewModel
- InMemoryArchiveDataSourceTests
- EntityConverter
- .GenerateSampleData
- EarthTool.TEX
- gltf.md
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- Modify An Existing Mesh
- StaticMeshAsset.cs
- Base Header
- IArchive
- UnitTest1.cs
- EarthTool Installation Guide
- .ResolveMetadataConflicts
- .GetMarkerAttachmentFlag
- ConvertCommand
- Dependabot Dependency Automation
- GlbDocument.cs
- Setup .NET Environment
- Mesh Attachments 1..49
- MeshAssetLineageId
- Code Quality Analysis Job
- Dynamic Color
- FlagsPropertyEditorViewModel
- MshOperationProfile
- Mesh Artist Quick Start And Cheat Sheet
- package.json
- CommandTypeRegistrar
- Migrate From COLLADA To glTF
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- App
- EarthTool.TEX.GUI/App.axaml.cs
- IExtractor
- MshCanonicalSerializer
- TexPreview
- validate-glb.mjs
- ViewLocator
- Decision consequences for later tickets
- Official MSH Qualification Performance
- .Match
- Tested build and fixture
- Extras and custom properties
- EarthTool.CLI.Tests
- .EditImportSamplesCubicSplineWithoutPreservingTangents
- ArchiverService
- Underscore-prefixed custom attributes
- ConvertCommand
- .ToByteArray
- IntCollectionPropertyEditorViewModel
- Building
- .Create

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 233 edges
2. `GltfInterchange` - 190 edges
3. `GlbDocument` - 152 edges
4. `DynamicGltfDocument` - 121 edges
5. `DynamicGltfInterchangeTests` - 96 edges
6. `OperationDiagnostic` - 93 edges
7. `EarthTool.PAR.Enums` - 90 edges
8. `OperationResult` - 79 edges
9. `StaticMeshAsset` - 78 edges
10. `MetadataGraphValidationTests` - 77 edges

## Surprising Connections (you probably didn't know these)
- `Memory-Mapped Archive Data Source` --semantically_similar_to--> `WD Reading Algorithm`  [INFERRED] [semantically similar]
  docs/architecture.md → docs/WD_FORMAT.md
- `CommitFailingReportFileSystem` --implements--> `ICliReportFileSystem`  [EXTRACTED]
  EarthTool.CLI.Tests/InternalMshCommandHostTests.cs → EarthTool.CLI/Commands/MSH/CliReportFileSystem.cs
- `FailingMshWriter` --implements--> `IMshWriter`  [EXTRACTED]
  EarthTool.CLI.Tests/InternalMshCommandHostTests.cs → EarthTool.MSH/Operations/MshOperations.cs
- `GltfCommandExecutor` --references--> `GltfInterchange`  [EXTRACTED]
  EarthTool.CLI/Commands/MSH/GltfCommandExecutor.cs → EarthTool.GLTF/GltfInterchange.cs
- `GltfCommandExecutor` --references--> `GltfCliReportSerializer`  [EXTRACTED]
  EarthTool.CLI/Commands/MSH/GltfCommandExecutor.cs → EarthTool.GLTF/GltfPlanAndReport.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Unified CI Multiplatform Application Builds** — _github_workflows_ci_unified_build_cli, _github_workflows_ci_unified_build_wd_gui, _github_workflows_ci_unified_build_par_gui, _github_workflows_ci_unified_build_tex_gui [EXTRACTED 1.00]
- **MSH Static Light Record Model** — context_static_light, context_active_static_light, context_light_attachment, context_light_parameters, context_spot_heading, context_spot_shape_values [EXTRACTED 1.00]
- **WD Test Coverage Layers** — earthtool_wd_tests_readme_model_tests, earthtool_wd_tests_readme_service_tests, earthtool_wd_tests_readme_integration_tests [EXTRACTED 1.00]
- **PAR Capability Hierarchy** — docs_par_structure_interactableentity, docs_par_structure_destructibleentity, docs_par_structure_equipableentity, docs_par_structure_passiveentity, docs_par_structure_vehicle, docs_par_structure_verticaltransporter [EXTRACTED 1.00]
- **WD Binary Archive Layout** — docs_wd_format_earthinfo_archive_header, docs_wd_format_file_data_section, docs_wd_format_central_directory, docs_wd_format_descriptor_length [EXTRACTED 1.00]
- **EarthTool Layered Modules** — docs_architecture_earthtool_cli, docs_architecture_earthtool_wd_gui, docs_architecture_earthtool_wd, docs_architecture_earthtool_msh, docs_architecture_earthtool_dae, docs_architecture_earthtool_par, docs_architecture_earthtool_tex, docs_architecture_earthtool_common [EXTRACTED 1.00]
- **Dynamic MESH Base Header Fields** — docs_msh_dynamic_bytefield_mesh_magic, docs_msh_dynamic_bytefield_version_1, docs_msh_dynamic_bytefield_dynamic_mesh_kind, docs_msh_dynamic_bytefield_box_presence_mask, docs_msh_dynamic_bytefield_animation_lengths, docs_msh_dynamic_bytefield_header_flags_reserved [INFERRED 0.95]
- **Dynamic MESH Spatial and Attachment Storage** — docs_msh_dynamic_bytefield_box_heights, docs_msh_dynamic_bytefield_box_flags, docs_msh_dynamic_bytefield_coverage_descriptors, docs_msh_dynamic_bytefield_coverage_bitmaps, docs_msh_dynamic_bytefield_attachments_1_49, docs_msh_dynamic_bytefield_mesh_extents [INFERRED 0.85]
- **Dynamic MESH Variable-Length Tail** — docs_msh_dynamic_bytefield_mesh_name_length, docs_msh_dynamic_bytefield_mesh_name, docs_msh_dynamic_bytefield_texture_path_length, docs_msh_dynamic_bytefield_texture_path, docs_msh_dynamic_bytefield_dynamic_child_count, docs_msh_dynamic_bytefield_dynamicobject_array [INFERRED 0.95]
- **Fixed Static Mesh Header Fields** — docs_msh_static_bytefield_static_framing_marker, docs_msh_static_bytefield_raw_windows_guid, docs_msh_static_bytefield_mesh_magic, docs_msh_static_bytefield_static_mesh_version, docs_msh_static_bytefield_static_mesh_kind, docs_msh_static_bytefield_box_presence_mask, docs_msh_static_bytefield_animation_length_encoding, docs_msh_static_bytefield_header_flags [EXTRACTED 1.00]
- **Spatial Coverage Metadata** — docs_msh_static_bytefield_box_presence_mask, docs_msh_static_bytefield_box_heights, docs_msh_static_bytefield_box_flags, docs_msh_static_bytefield_coverage_descriptors, docs_msh_static_bytefield_coverage_bitmaps [INFERRED 0.85]
- **StaticObject Variable Record Layout** — docs_msh_static_bytefield_staticobject_record, docs_msh_static_bytefield_object_flags, docs_msh_static_bytefield_texture_path, docs_msh_static_bytefield_triangle_array, docs_msh_static_bytefield_baked_tcbscale_vectors, docs_msh_static_bytefield_baked_translation_vectors, docs_msh_static_bytefield_baked_transform_matrices, docs_msh_static_bytefield_animation_type, docs_msh_static_bytefield_object_pivot, docs_msh_static_bytefield_barrel_angle, docs_msh_static_bytefield_next_record_marker [INFERRED 0.85]

## Communities (178 total, 7 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - "MeshAsset"
Cohesion: 0.10
Nodes (16): byte, MeshAsset, MeshAssetKind, Stream, ITransactionalFileSystem, TransactionalFileSystem, CancellationToken, Exception (+8 more)

### Community 2 - "Runner"
Cohesion: 0.13
Nodes (15): AssetResult, ChannelReader, ChannelWriter, DiagnosticKey, DynamicCoverage, Guid, IEnumerable, Task (+7 more)

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.11
Nodes (15): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+7 more)

### Community 5 - ".CreateEffectPreview"
Cohesion: 0.14
Nodes (12): DynamicEditedPreview, DynamicEffectPreview, ReadOnlySpan, Translation, Vector2, Vector3, DynamicAnimationTrack, DynamicEditedPreview (+4 more)

### Community 6 - "GltfWalkingSkeletonTests"
Cohesion: 0.08
Nodes (8): BlenderOutputEvidence, Guid, IEnumerable, Task, Trait, BlenderOutputEvidence, GltfWalkingSkeletonTests, Action

### Community 7 - ".ToByteArray"
Cohesion: 0.11
Nodes (10): EarthTool.PAR.Tests.Factories, Encoding, Encoding, Encoding, Encoding, Encoding, Encoding, Encoding (+2 more)

### Community 8 - ".Load"
Cohesion: 0.17
Nodes (10): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IReadOnlyDictionary, IReadOnlyList, Vector3, MshPreviewLoader (+2 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.12
Nodes (8): bool, ILogger, ObservableCollection, ReactiveCommand, string, Task, Unit, MainWindowViewModel

### Community 11 - "OperationResult"
Cohesion: 0.13
Nodes (19): IReadOnlyList, OperationResult, GltfDynamicEditImportResult, GltfEditImportOptions, GltfEditImportResult, GltfMeshEditImportResult, GltfMetadataConflictResolution, GltfMetadataLineageDisposition (+11 more)

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.06
Nodes (33): DynamicAlphaTiming, DynamicEffectType, DynamicMeshAsset, EffectRectangle, IEnumerable, Vector3, CanonicalDynamicAlpha, CanonicalDynamicEffectShape (+25 more)

### Community 13 - "Vector3"
Cohesion: 0.10
Nodes (12): Action, BinaryWriter, float, Matrix4x4, Quaternion, Translation, Vector3, AttachmentHeadingProjection (+4 more)

### Community 14 - "MshV1Decoder"
Cohesion: 0.10
Nodes (22): DecodedStaticRecord, MeshAssetOrigin, CancellationToken, Guid, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+14 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "DynamicEffectSemantics"
Cohesion: 0.17
Nodes (9): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, Fact, Guid (+1 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "EarthTool.Common"
Cohesion: 0.08
Nodes (16): EarthTool.TEX, EarthTool.PAR, EarthTool.Common, EarthTool.CLI, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, EarthTool.CLI.Commands, Type (+8 more)

### Community 19 - "Equipment"
Cohesion: 0.08
Nodes (17): LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable, Equipment (+9 more)

### Community 20 - "release-qualification.mjs"
Cohesion: 0.07
Nodes (62): corpusBinaryStages, corpusInterchangeStages, recognizedDynamicEffectTypes, assertPrivacySafe(), buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames() (+54 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.08
Nodes (15): IEnumerable, Task, IDialogService, INotificationService, Task, ITextFlagService, bool, ILogger (+7 more)

### Community 22 - "GltfImportPlanSerializer"
Cohesion: 0.06
Nodes (28): BufferPath, ConflictKey, Directory, CancellationToken, Guid, IEnumerable, IReadOnlyDictionary, JsonElement (+20 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (11): Bitmap, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap, string (+3 more)

### Community 24 - ".ExportGlbFileAsync"
Cohesion: 0.05
Nodes (13): Stream, ITransactionalFileSystem, TransactionalFileSystem, int, Stream, ManifestFailingFileSystem, CancellationTokenSource, Stream (+5 more)

### Community 25 - "Vehicle"
Cohesion: 0.08
Nodes (18): VehicleObjectType, Encoding, IEnumerable, Builder, Encoding, IEnumerable, Harvester, Encoding (+10 more)

### Community 26 - "Common MSH Base Header"
Cohesion: 0.10
Nodes (23): Model MSH Framing and Record Extensions Explicitly, Canonical Next Record Markers, MSH Footprint API, MSH Horizontal Extents API, IMeshBaseHeader, Legacy MSH Model Migration, MSH API, MSH Slots API (+15 more)

### Community 27 - "MetadataGraphValidationTests"
Cohesion: 0.13
Nodes (13): Baseline, Action, Bytes, Fact, Func, Guid, ICollection, InlineData (+5 more)

### Community 28 - ".CreateMockHeader"
Cohesion: 0.15
Nodes (11): IEarthInfoFactory, bool, DateTime, IReadOnlyCollection, MemoryMappedFile, Archive, Fact, ArchiveTests (+3 more)

### Community 29 - "EarthTool.MSH.Assets"
Cohesion: 0.07
Nodes (37): CliResult, EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.CLI.Commands.MSH, EarthTool.MSH (+29 more)

### Community 30 - ".CreateJson"
Cohesion: 0.16
Nodes (6): DynamicAnimationLayout, DynamicAnimationTrack, DynamicImageLayout, DynamicMeshLayout, BinaryWriter, Utf8JsonWriter

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Asset, byte, Bytes, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - ".Create"
Cohesion: 0.13
Nodes (10): JsonNode, int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture (+2 more)

### Community 35 - "AnimationClassBytes"
Cohesion: 0.14
Nodes (10): CanonicalStaticRecord, AnimationClassBytes, CanonicalHorizontalExtents, CanonicalStaticFootprint, CanonicalStaticRenderObject, CanonicalStaticSourceObject, StaticMeshBuilder, Guid (+2 more)

### Community 36 - "GltfContracts.cs"
Cohesion: 0.08
Nodes (26): Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfAnimationHandle, GltfArtistObjectLocalIds, GltfDiagnosticCodes, GltfExportOptions (+18 more)

### Community 37 - "StaticMeshAsset"
Cohesion: 0.15
Nodes (10): StaticMeshAsset, StaticRenderObjectId, StaticSourceObject, bool, Dictionary, IEnumerable, int, Matrix4x4 (+2 more)

### Community 38 - "ArchiverServiceTests"
Cohesion: 0.16
Nodes (8): CancellationToken, CommandContext, DateTime, Guid, IArchiver, Fact, string, ArchiverServiceTests

### Community 39 - "TreeItemViewModel"
Cohesion: 0.10
Nodes (12): DateTime, int, long, string, ArchiveInfoViewModel, ArchiveItemViewModel, HashSet, bool (+4 more)

### Community 40 - "IArchiveItem"
Cohesion: 0.08
Nodes (19): ReadOnlyMemory, IArchiveItem, HashSet, TextFlagService, ReadOnlyMemory, IArchiveDataSource, bool, ReadOnlyMemory (+11 more)

### Community 41 - "TexPreviewLoader"
Cohesion: 0.21
Nodes (8): byte, CancellationToken, Exception, GltfExportOptions, GltfOperationProfile, ICollection, PreviewResolutionKind, TexPreviewLoader

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - "GltfInterchange"
Cohesion: 0.06
Nodes (17): AnimationReplacement, IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, ICollection, IEnumerable, JsonNode, JsonObject (+9 more)

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.12
Nodes (17): AnimationObjectLayout, AnimationReplacement, NewModelAnimationTrack, BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion (+9 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.11
Nodes (15): Action, IEnumerable, IPropertyEditorFactory, Action, bool, IEnumerable, ILogger, ObservableCollection (+7 more)

### Community 48 - "ParFile"
Cohesion: 0.07
Nodes (23): Reader, Writer, FileType, Task, IParFileService, ILogger, Task, ParFileService (+15 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (20): FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo (+12 more)

### Community 51 - ".Create"
Cohesion: 0.07
Nodes (21): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, Fact, IEnumerable (+13 more)

### Community 52 - "GlbDocument"
Cohesion: 0.09
Nodes (9): GltfOperationProfile, IDictionary, JsonDocument, JsonElement, ReadOnlySpan, uint, GlbDocument, GltfImportIntent (+1 more)

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.08
Nodes (24): ContentFingerprint, BinaryWriter, Dictionary, IDictionary, int, IReadOnlyDictionary, IReadOnlyList, ISet (+16 more)

### Community 54 - "IReadOnlyList"
Cohesion: 0.13
Nodes (16): AnimationLayout, PartitionMatch, IReadOnlyList, MemoryStream, ByteArrayComparer, GeometryPartition, ParsedGltfPrimitive, PartitionLayout (+8 more)

### Community 55 - "MetadataEnvelope"
Cohesion: 0.08
Nodes (32): Discarded, IDictionary, IReadOnlyCollection, IReadOnlyDictionary, IReadOnlyList, ISet, List, Matrix4x4 (+24 more)

### Community 56 - "Entity"
Cohesion: 0.11
Nodes (16): EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List, ValidationError, ValidationResult, ValidationSeverity (+8 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "EditableEntity"
Cohesion: 0.11
Nodes (14): EntityClassType, bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, ObservableCollection (+6 more)

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - "DynamicGltfDocument"
Cohesion: 0.09
Nodes (20): DynamicSceneLayout, float, IEnumerable, int, ISet, JsonElement, string, uint (+12 more)

### Community 62 - "EarthTool.PAR.Models.Abstracts"
Cohesion: 0.03
Nodes (50): EarthTool.PAR.Extensions, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Factories, ExplosionFlags, MaxShieldUpgradeType, PositionType, ResourceVehicleType, StandType (+42 more)

### Community 63 - ".ToByteArray"
Cohesion: 0.12
Nodes (9): WasteSize, Encoding, Encoding, Encoding, IEnumerable, FlyingWaste, Encoding, Encoding (+1 more)

### Community 64 - "IUndoRedoService"
Cohesion: 0.11
Nodes (11): Action, IEnumerable, IUndoRedoService, int, string, IntPropertyEditorViewModel, bool, int (+3 more)

### Community 65 - "TexFile"
Cohesion: 0.18
Nodes (9): BinaryReader, BinaryReader, IEnumerable, TexFile, TexHeader, BinaryReader, IEnumerable, SKBitmap (+1 more)

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "MetadataConflictCollector"
Cohesion: 0.15
Nodes (18): bool, GltfOperationProfile, IEnumerable, int, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList, List (+10 more)

### Community 68 - "DynamicEffectExtension"
Cohesion: 0.27
Nodes (3): ReadOnlySpan, DynamicEffectExtension, DynamicLightType

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - ".ResolveAndLoad"
Cohesion: 0.23
Nodes (6): IEnumerable, int, long, Resolution, ResolutionBudget, Resolution

### Community 72 - "InteractableEntity"
Cohesion: 0.06
Nodes (20): BarrelBetaType, ConnectorType, ShadowType, TargetType, WeaponFireType, Encoding, IEnumerable, InteractableEntity (+12 more)

### Community 73 - "Task"
Cohesion: 0.07
Nodes (27): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+19 more)

### Community 74 - ".Decode"
Cohesion: 0.14
Nodes (8): IEnumerable, int, long, TexHeader, TexResolutionBudget, TexFlags, HasVariants, Preview

### Community 75 - "AuthoringValidation"
Cohesion: 0.15
Nodes (6): HashSet, IReadOnlyList, List, Vector2, Vector3, AuthoringValidation

### Community 76 - "IReadOnlyList"
Cohesion: 0.15
Nodes (14): DynamicObjectScope, CancellationToken, GltfOperationProfile, ICollection, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList, JsonDocument (+6 more)

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.05
Nodes (25): EarthTool.WD.GUI.ViewModels, EarthTool.CLI.Commands.PAR, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Services, EarthTool.Common.Models (+17 more)

### Community 79 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.20
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - "Fact"
Cohesion: 0.09
Nodes (10): AnimationLengths, Fact, JsonDocument, JsonElement, Vector3, IReadOnlyList, Matrix4x4, Vector3 (+2 more)

### Community 82 - ".CreateJson"
Cohesion: 0.15
Nodes (11): GltfArtistObjectLocalIds, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, NativeProjectionFingerprint, Utf8JsonWriter, GltfPackage, PartitionLayout (+3 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.20
Nodes (10): Animations, Blender 4.5 glTF round-trip research, Conclusion, Evidence model, Meshes, primitives, and topology, Nodes, hierarchy, scenes, and transforms, Primary sources, Punctual lights (+2 more)

### Community 84 - "EarthTool.PAR.Enums"
Cohesion: 0.06
Nodes (22): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.PAR.GUI.Services, EarthTool.PAR.Tests.Services, EarthTool.PAR.Enums, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models (+14 more)

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "UndoRedoService"
Cohesion: 0.14
Nodes (9): Action, DateTime, UndoAction, Action, IEnumerable, ILogger, int, UndoRedoService (+1 more)

### Community 88 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "TreeNodeViewModelBase"
Cohesion: 0.07
Nodes (26): Encoding, IBinarySerializable, Faction, ResearchType, ObservableCollection, EntityGroupNodeViewModel, ObservableCollection, EntityGroupsRootNodeViewModel (+18 more)

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "MeshAssetAuthoring.cs"
Cohesion: 0.21
Nodes (8): Guid, ICollection, DynamicMeshBuilder, MshEditResult, PreservationChange, PreservationDisposition, PreservationReport, StaticLightRecordKind

### Community 95 - "PassiveEntity"
Cohesion: 0.15
Nodes (8): ArtifactType, PassiveMask, Encoding, IEnumerable, PassiveEntity, Encoding, IEnumerable, Artifact

### Community 96 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.06
Nodes (24): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, EarthTool.Common.GUI.Views, NotificationType, Exception (+16 more)

### Community 97 - "SourceObjectId"
Cohesion: 0.12
Nodes (5): UnchangedEmitterOwnership, SourceObjectId, IDictionary, RewrittenStaticRecord, IEquatable

### Community 98 - "EarthTool Suite"
Cohesion: 0.20
Nodes (11): EarthTool.DAE, EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management (+3 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - ".WriteReconciledRecord"
Cohesion: 0.22
Nodes (3): DynamicRecordSlice, IDictionary, Stream

### Community 101 - "EarthTool.CLI.Commands.WD"
Cohesion: 0.06
Nodes (36): Command, CommandSettings, EarthTool.CLI.Commands.WD, CommonSettings, AddCommand, CancellationToken, CommandContext, CreateCommand (+28 more)

### Community 102 - "EarthTool Documentation"
Cohesion: 0.25
Nodes (9): Dependency Inversion, EarthTool Architecture, Interface Segregation, Layered Modular Architecture, Central Package Management, EarthTool Project Structure, EarthTool Module Dependency Rules, EarthTool Test Project Structure (+1 more)

### Community 103 - "EarthTool.Common"
Cohesion: 0.29
Nodes (8): EarthTool.CLI, EarthTool.Common, EarthTool.MSH, EarthTool.PAR, EarthTool.TEX, EarthTool.WD, EarthTool.WD.GUI, Memory-Mapped Archive Data Source

### Community 104 - "Entity"
Cohesion: 0.25
Nodes (9): PAR Parameter Editing, PAR Binary Serialization, EarthTool PAR Entity Hierarchy, Entity, IBinarySerializable, ParameterEntry, Polymorphic JSON Deserialization, Research (+1 more)

### Community 105 - "DestructibleEntity"
Cohesion: 0.28
Nodes (9): Capability Stacking Architecture, DestructibleEntity, EquipableEntity, Equipment, InteractableEntity, PassiveEntity, TypedEntity, Vehicle (+1 more)

### Community 106 - "KhronosValidatorServer"
Cohesion: 0.20
Nodes (8): List, KhronosValidatorServer, ValidatorResult, IAsyncDisposable, Process, ValidatorCode, ValidatorResult, ValueTask

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - ".Resolve"
Cohesion: 0.29
Nodes (5): Func, IEnumerable, IReadOnlyList, SafeResourceLookup, SafeResourceMatch

### Community 109 - "PropertyEditorViewModel"
Cohesion: 0.17
Nodes (13): Action, HashSet, IEnumerable, ILogger, Type, PropertyEditorFactory, bool, ReactiveCommand (+5 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.06
Nodes (33): AsyncCommand, Stream, CliReportFileSystem, ICliReportFileSystem, CancellationToken, Exception, Func, IEnumerable (+25 more)

### Community 111 - "glTF .NET foundation research"
Cohesion: 0.22
Nodes (8): Alternative: Khronos glTF2Loader, Decision, glTF .NET foundation research, Historical branch verification, Package and architecture recommendation, Required capabilities, Risks and required spikes, SharpGLTF assessment

### Community 112 - "Detect Changes Job"
Cohesion: 0.29
Nodes (8): Build CLI Job, Build PAR GUI Job, Build TEX GUI Job, Build WD GUI Job, Detect Changes Job, Branch Version Labels, Continuous Deployment Versioning, Semantic Versioning Configuration

### Community 113 - "Unified CI Pipeline"
Cohesion: 0.29
Nodes (8): Unified CI Pipeline, Development Preview Builds, Smart Change Detection, Unified Workflow Architecture, Release Build and Test Job, Create Release Job, Unified Release Pipeline, Self-Contained Multiplatform Artifacts

### Community 114 - "Conventional Commits"
Cohesion: 0.36
Nodes (8): Automatic Changelog Generation, Breaking Change Signaling, Conventional Commits, Release Type Detection, Generate Unified Changelog, EarthTool Agent Guidelines, Build and Test Commands, EarthTool Contributor Workflow

### Community 115 - "WD Archive Commands"
Cohesion: 0.25
Nodes (8): CLI Archive Workflow, GUI Archive Workflow, EarthTool Quick Start Guide, wd add, WD Archive Commands, wd info, wd list, wd remove

### Community 116 - "EnumPropertyEditorViewModel"
Cohesion: 0.29
Nodes (5): object, ObservableCollection, Type, EnumPropertyEditorViewModel, EnumValueViewModel

### Community 117 - "InMemoryArchiveDataSourceTests"
Cohesion: 0.23
Nodes (4): ReadOnlyMemory, InMemoryArchiveDataSource, Fact, InMemoryArchiveDataSourceTests

### Community 118 - "EntityConverter"
Cohesion: 0.25
Nodes (7): JsonSerializerOptions, Type, Utf8JsonWriter, EntityConverter, TypeReader, JsonConverter, Utf8JsonReader

### Community 119 - ".GenerateSampleData"
Cohesion: 0.13
Nodes (6): Fact, ArchiveItemTests, Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "gltf.md"
Cohesion: 0.33
Nodes (3): Dynamic effect-preview contract, glTF API, Static-light authoring contract

### Community 123 - "EarthTool.TEX.Tests"
Cohesion: 0.29
Nodes (7): EarthTool.TEX.Tests, net8.0, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 124 - "EarthTool"
Cohesion: 0.29
Nodes (7): EarthTool, EarthTool.CLI, EarthTool.DAE, EarthTool.MSH, EarthTool.PAR, EarthTool.PAR.GUI, EarthTool.TEX

### Community 125 - "Static Light"
Cohesion: 0.33
Nodes (6): Active Static Light, Light Attachment, Light Parameters, Spot Heading, Spot Shape Values, Static Light

### Community 126 - "OfficialCorpusCliOracle"
Cohesion: 0.14
Nodes (13): CliProcessResult, CliReportOperation, IReadOnlyList, JsonElement, string, Task, CliBatchOracleResult, CliDiagnostic (+5 more)

### Community 127 - "Modify An Existing Mesh"
Cohesion: 0.29
Nodes (7): 1. Extract and export, 2. Import into Blender, 3. Edit or add geometry, 4. Preview all animation classes, 5. Export from Blender, 6. Import the edit and install it, Modify An Existing Mesh

### Community 128 - "StaticMeshAsset.cs"
Cohesion: 0.23
Nodes (9): Guid, IReadOnlyList, CommonMeshBaseHeader, DynamicObject, MeshArchiveFraming, StaticAnimationClass, StaticRenderObjectFlagMasks, StaticRenderObjectFlags (+1 more)

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "IArchive"
Cohesion: 0.15
Nodes (15): DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid, DateTime, Guid (+7 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 134 - ".GetMarkerAttachmentFlag"
Cohesion: 0.24
Nodes (3): EmitterActive, MarkerRecordCount, Span

### Community 135 - "ConvertCommand"
Cohesion: 0.11
Nodes (18): IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand, CancellationToken, CommandContext (+10 more)

### Community 136 - "Dependabot Dependency Automation"
Cohesion: 0.50
Nodes (4): Dependabot Dependency Automation, Weekly GitHub Actions Updates, Weekly NuGet Updates, Security Check Job

### Community 137 - "GlbDocument.cs"
Cohesion: 0.08
Nodes (25): CarrierKind, ImportPlanException, DynamicMetadataIdentityException, ICollection, int, Path, string, Value (+17 more)

### Community 138 - "Setup .NET Environment"
Cohesion: 0.67
Nodes (3): .NET SDK Setup, NuGet Package Cache, Setup .NET Environment

### Community 139 - "Mesh Attachments 1..49"
Cohesion: 0.67
Nodes (3): Trailing Hierarchy Unwind Count, Mesh Attachments 1..49, Mesh Extents

### Community 140 - "MeshAssetLineageId"
Cohesion: 0.25
Nodes (7): Guid, IEnumerable, WalkingSkeletonConsumer, MeshAssetLineageId, MshBuildResult, IEnumerable, MshExpert

### Community 143 - "FlagsPropertyEditorViewModel"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 148 - "MshOperationProfile"
Cohesion: 0.18
Nodes (12): CancellationToken, Stream, Task, CancellationToken, Stream, string, Task, IMshReader (+4 more)

### Community 149 - "Mesh Artist Quick Start And Cheat Sheet"
Cohesion: 0.33
Nodes (6): Attachment Identifier Cheat Sheet, Choose The Correct Workflow, Create A Standalone MSH, Directional Empty Presentation In Blender, Fast Checks Before Import, Mesh Artist Quick Start And Cheat Sheet

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "CommandTypeRegistrar"
Cohesion: 0.22
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 152 - "Migrate From COLLADA To glTF"
Cohesion: 0.33
Nodes (6): API migration, Attachment helper name migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 153 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 154 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 155 - "EarthTool.TEX.GUI/App.axaml.cs"
Cohesion: 0.13
Nodes (10): EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs (+2 more)

### Community 156 - "IExtractor"
Cohesion: 0.33
Nodes (3): Task, IExtractor, IWDExtractor

### Community 158 - "MshCanonicalSerializer"
Cohesion: 0.10
Nodes (21): Matrix4x4, Vector3, StaticAnimationTracks, StaticRenderObject, CanonicalStaticVertex, CanonicalTriangle, StaticAnimationReplacement, StaticRenderObjectAddition (+13 more)

### Community 159 - "TexPreview"
Cohesion: 0.23
Nodes (9): IReadOnlyDictionary, IReadOnlyList, DynamicTexPreviewLoadResult, PreviewResolution, TexPreview, TexPreviewLoadResult, PreviewResolution, PreviewResolutionKind (+1 more)

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - "ViewLocator"
Cohesion: 0.06
Nodes (19): EarthTool.PAR.GUI, EarthTool.TEX.GUI, EarthTool.WD.GUI, AppBuilder, STAThread, Program, Control, ViewLocator (+11 more)

### Community 164 - "Decision consequences for later tickets"
Cohesion: 0.40
Nodes (5): Decision consequences for later tickets, EarthTool metadata requirements, Native glTF candidates, Required fingerprints and invalidation, What stock Blender cannot promise

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Historical Measured Result, Official MSH Qualification Performance, Stage Profiling, Aggregate release qualification, Blender matrix, Official MSH corpus

### Community 167 - "Tested build and fixture"
Cohesion: 0.67
Nodes (3): Diagnostic asset, Stock options, Tested build and fixture

### Community 168 - "Extras and custom properties"
Cohesion: 0.67
Nodes (3): Extras and custom properties, JSON value behavior, Scope survival matrix

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 170 - ".EditImportSamplesCubicSplineWithoutPreservingTangents"
Cohesion: 0.22
Nodes (4): Action, IReadOnlyList, JsonObject, List

### Community 171 - "ArchiverService"
Cohesion: 0.10
Nodes (14): IArchiveFactory, Stream, ICompressor, ReadOnlySpan, Stream, IDecompressor, PathValidator, Encoding (+6 more)

### Community 172 - "Underscore-prefixed custom attributes"
Cohesion: 0.67
Nodes (3): Identity, order, collision, and merge behavior, Supported import shapes, Underscore-prefixed custom attributes

### Community 173 - "ConvertCommand"
Cohesion: 0.14
Nodes (14): CommonCommand, CommonSettings, IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, Settings (+6 more)

### Community 175 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 176 - "IntCollectionPropertyEditorViewModel"
Cohesion: 0.60
Nodes (3): IEnumerable, string, IntCollectionPropertyEditorViewModel

### Community 182 - "Building"
Cohesion: 0.10
Nodes (11): BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, ResourceInputOutputFlags, SpaceStationType, Encoding, Encoding (+3 more)

### Community 189 - ".Create"
Cohesion: 0.11
Nodes (8): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord, InlineData, Theory

## Knowledge Gaps
- **333 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+328 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `IValueConverter`, `ParFile`, `EarthTool.Common`, `IEarthInfo`, `EarthTool.MSH.Assets`?**
  _High betweenness centrality (0.160) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `Task` to `IArchiveItem`, `DynamicGltfInterchangeTests`?**
  _High betweenness centrality (0.104) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `StaticMeshAsset.cs`, `MetadataConflictCollector`, `GltfContracts.cs`, `GlbDocument.cs`, `DynamicGltfInterchangeTests`, `StaticAnimationProjection`, `DynamicEffectSemantics`, `MshOperationProfile`, `MeshAssetAuthoring.cs`?**
  _High betweenness centrality (0.101) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _333 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MeshAsset` be split into smaller, more focused modules?**
  _Cohesion score 0.10465116279069768 - nodes in this community are weakly interconnected._
- **Should `Runner` be split into smaller, more focused modules?**
  _Cohesion score 0.12772133526850507 - nodes in this community are weakly interconnected._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._