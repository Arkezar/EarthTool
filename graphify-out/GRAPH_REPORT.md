# Graph Report - EarthTool  (2026-08-05)

## Corpus Check
- 373 files · ~304,488 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4822 nodes · 14602 edges · 179 communities (169 shown, 10 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 1031 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `ec5f44c0`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- .WriteFileAsync
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- DynamicGltfDocument
- .GetEmitterHierarchyState
- .ToByteArray
- .ResolveAndLoad
- IValueConverter
- MainWindowViewModel
- OperationResult
- DynamicGltfInterchangeTests
- Vector3
- MshDecodeContext
- .OpenArchive
- MeshAsset
- Dynamic MESH Binary Layout
- GltfPlanAndReportTests
- .Create
- release-qualification.mjs
- MainWindowViewModel
- IArchive
- MainWindowViewModel
- ITransactionalFileSystem
- Vehicle
- Common MSH Base Header
- MetadataGraphValidationTests
- .CreateMockHeader
- EarthTool.MSH.Assets
- .ValidateManifestInventory
- EarthTool.CLI
- DynamicMeshAssetTests
- .Create
- OperationDiagnostic
- IDialogService
- GlbDocument.cs
- StaticMeshEditSession
- ArchiverServiceTests
- .ReadRibbonPreview
- .CreateCurrentStaticLightGuards
- EarthTool.PAR.GUI.ViewModels
- GltfInterchange
- Static Mesh Header
- StaticObject Record
- GlbDocument
- StaticAnimationProjection
- EntityDetailsViewModel
- ResearchReferenceCollectionEditorViewModel
- PublicApiApproval
- IEarthInfo
- .Create
- JsonElement
- OfficialCorpusQualification
- IReadOnlyList
- GltfImportPlanSerializer
- Entity
- EarthTool.MSH.Tests
- GltfCliReportOperation
- DialogService
- EarthTool.PAR
- .CreateJson
- EarthTool.PAR.Enums
- DestructibleEntity
- .EditStatic
- ConvertCommand
- EarthTool.PAR.GUI
- MetadataEnvelope
- VerticalTransporter
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- OneTriangleMshFixture
- InteractableEntity
- Task
- Equipment
- MshOperationProfile
- ParameterReaderTests.cs
- EarthTool.sln
- EarthTool.Common.Interfaces
- Program
- BinaryExtensions
- GltfWalkingSkeletonTests
- StaticMeshAsset
- Blender 4.5 glTF round-trip research
- .RemoveGlbMetadata
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- ParFile
- .Pack
- 0003-create-immutable-msh-assets-from-gltf.md
- EarthTool WD Archive Manager
- MeshAssetLineageId
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- glTF API
- EarthTool.Common.GUI.Enums
- NotificationService
- EarthTool Suite
- WD Central Directory
- GltfContracts.cs
- IArchiveItem
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- WorkerContext
- Reader
- IUndoRedoService
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- TexPreviewLoader
- PropertyEditorViewModel
- MainWindow
- .GenerateSampleData
- EarthTool.TEX
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- Modify An Existing Mesh
- Base Header
- TreeItemViewModel
- UnitTest1.cs
- EarthTool Installation Guide
- ConvertCommand
- Dependabot Dependency Automation
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- Setup .NET Environment
- Mesh Attachments 1..49
- DynamicEffectExtension
- Code Quality Analysis Job
- Dynamic Color
- .WriteReconciledRecord
- ITexFile
- Mesh Artist Quick Start And Cheat Sheet
- package.json
- Migrate From COLLADA To glTF
- FlagsPropertyEditorViewModel
- CommandTypeRegistrar
- CommonCommand
- MshCanonicalSerializer
- Runner
- validate-glb.mjs
- ViewLocator
- Decision consequences for later tickets
- Official MSH Qualification Performance
- Tested build and fixture
- Extras and custom properties
- EarthTool.CLI.Tests
- ItemCommand
- Underscore-prefixed custom attributes
- IExtractor
- .ToByteArray
- EnumPropertyEditorViewModel
- Missile
- MappedArchiveDataSource
- Task
- IReadOnlyList
- StaticSourceObject
- EquipableEntity
- QualificationProfiler
- AnimationClassBytes
- .Write_And_Read_AreSymmetric
- .Create
- ParameterReaderTests

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 249 edges
2. `GltfInterchange` - 201 edges
3. `GlbDocument` - 152 edges
4. `DynamicGltfDocument` - 121 edges
5. `DynamicGltfInterchangeTests` - 96 edges
6. `OperationDiagnostic` - 90 edges
7. `EarthTool.PAR.Enums` - 90 edges
8. `StaticMeshAsset` - 84 edges
9. `OperationResult` - 79 edges
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

## Communities (179 total, 10 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - ".WriteFileAsync"
Cohesion: 0.12
Nodes (13): Stream, ITransactionalFileSystem, TransactionalFileSystem, IMshValidator, CancellationToken, Exception, IEnumerable, ILogger (+5 more)

### Community 2 - "AssetResult"
Cohesion: 0.22
Nodes (7): AssetResult, DiagnosticKey, AssetResult, KhronosValidatorServer, OperationCounts, ProfileScope, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.11
Nodes (15): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+7 more)

### Community 5 - "DynamicGltfDocument"
Cohesion: 0.10
Nodes (16): float, int, string, uint, Vector2, Vector3, DynamicAnimationLayout, DynamicAnimationTrack (+8 more)

### Community 7 - ".ToByteArray"
Cohesion: 0.07
Nodes (23): Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding, IEnumerable (+15 more)

### Community 8 - ".ResolveAndLoad"
Cohesion: 0.10
Nodes (16): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+8 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.07
Nodes (16): IEnumerable, Task, IParFileService, ObservableCollection, EntityGroupsRootNodeViewModel, bool, ILogger, ObservableCollection (+8 more)

### Community 11 - "OperationResult"
Cohesion: 0.14
Nodes (18): IReadOnlyList, OperationResult, GltfDynamicEditImportResult, GltfEditImportOptions, GltfEditImportResult, GltfMeshEditImportResult, GltfMetadataConflictResolution, GltfNewModelImportResult (+10 more)

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.06
Nodes (36): DynamicAlphaTiming, DynamicEffectType, DynamicLightType, DynamicMeshAsset, EffectRectangle, IEnumerable, Vector3, CanonicalDynamicAlpha (+28 more)

### Community 13 - "Vector3"
Cohesion: 0.11
Nodes (12): Action, BinaryWriter, float, Quaternion, Translation, Vector3, AttachmentHeadingProjection, ProjectedAttachment (+4 more)

### Community 14 - "MshDecodeContext"
Cohesion: 0.12
Nodes (20): DecodedStaticRecord, MeshArchiveFraming, int, DynamicMeshDecoder, CancellationToken, IReadOnlyDictionary, IReadOnlyList, List (+12 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "MeshAsset"
Cohesion: 0.16
Nodes (13): CancellationToken, Stream, Task, Action, byte, Func, MeshAsset, MeshAssetKind (+5 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "GltfPlanAndReportTests"
Cohesion: 0.13
Nodes (14): BufferPath, ConflictKey, Directory, Fact, Guid, InlineData, JsonNode, Task (+6 more)

### Community 19 - ".Create"
Cohesion: 0.17
Nodes (6): AnimationLengths, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 20 - "release-qualification.mjs"
Cohesion: 0.07
Nodes (62): corpusBinaryStages, corpusInterchangeStages, recognizedDynamicEffectTypes, assertPrivacySafe(), buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames() (+54 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.10
Nodes (11): INotificationService, ITextFlagService, bool, ILogger, object, ObservableCollection, ReactiveCommand, string (+3 more)

### Community 22 - "IArchive"
Cohesion: 0.07
Nodes (29): DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid, IArchiveFactory, Stream (+21 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.13
Nodes (12): Bitmap, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap, string (+4 more)

### Community 24 - "ITransactionalFileSystem"
Cohesion: 0.05
Nodes (13): Stream, ITransactionalFileSystem, TransactionalFileSystem, int, Stream, ManifestFailingFileSystem, CancellationTokenSource, Stream (+5 more)

### Community 25 - "Vehicle"
Cohesion: 0.09
Nodes (18): VehicleObjectType, Encoding, IEnumerable, Builder, Encoding, IEnumerable, Harvester, Encoding (+10 more)

### Community 26 - "Common MSH Base Header"
Cohesion: 0.10
Nodes (23): Model MSH Framing and Record Extensions Explicitly, Canonical Next Record Markers, MSH Footprint API, MSH Horizontal Extents API, IMeshBaseHeader, Legacy MSH Model Migration, MSH API, MSH Slots API (+15 more)

### Community 27 - "MetadataGraphValidationTests"
Cohesion: 0.13
Nodes (13): Baseline, Action, Bytes, Fact, Func, Guid, ICollection, InlineData (+5 more)

### Community 28 - ".CreateMockHeader"
Cohesion: 0.13
Nodes (14): ResourceType, Guid, Stream, IEarthInfoFactory, bool, DateTime, IReadOnlyCollection, MemoryMappedFile (+6 more)

### Community 29 - "EarthTool.MSH.Assets"
Cohesion: 0.07
Nodes (35): CliResult, EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.CLI.Commands.MSH, EarthTool.MSH (+27 more)

### Community 30 - ".ValidateManifestInventory"
Cohesion: 0.24
Nodes (5): CarrierKind, ICollection, Path, Value, Envelope

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Asset, byte, Bytes, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - ".Create"
Cohesion: 0.15
Nodes (9): int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture, OmniRecord (+1 more)

### Community 34 - "OperationDiagnostic"
Cohesion: 0.07
Nodes (9): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, Func, IEnumerable, JsonObject, MetadataConflictResolutionResult, ParseScopeResolution (+1 more)

### Community 36 - "GlbDocument.cs"
Cohesion: 0.11
Nodes (19): ImportPlanException, DynamicMetadataIdentityException, int, string, MalformedMetadataException, MetadataAnimationClass, MetadataAnimationProjection, MetadataPartition (+11 more)

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.12
Nodes (12): StaticRenderObjectId, bool, Dictionary, ICollection, IEnumerable, int, Matrix4x4, Vector2 (+4 more)

### Community 38 - "ArchiverServiceTests"
Cohesion: 0.05
Nodes (41): Command, EarthTool.CLI.Commands.WD, CancellationToken, CommandContext, AddCommand, CancellationToken, CommandContext, CreateCommand (+33 more)

### Community 39 - ".ReadRibbonPreview"
Cohesion: 0.36
Nodes (3): DynamicEditedPreview, ReadOnlySpan, DynamicPreviewException

### Community 41 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.05
Nodes (27): EarthTool.PAR.GUI, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, EntityClassType, Faction, ResearchType (+19 more)

### Community 42 - "GltfInterchange"
Cohesion: 0.04
Nodes (47): AnimationEditPlan, AnimationReplacement, GltfLightHandle, GltfMaterialHandle, GltfNewModelImportOptions, GltfNodeHandle, ICollection, IDictionary (+39 more)

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - "GlbDocument"
Cohesion: 0.08
Nodes (10): GltfOperationProfile, IDictionary, JsonDocument, JsonElement, Matrix4x4, ReadOnlySpan, uint, GlbDocument (+2 more)

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.14
Nodes (15): AnimationObjectLayout, BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionFingerprint (+7 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.07
Nodes (23): bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, Action, bool (+15 more)

### Community 48 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.20
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (17): FileFlags, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo, Encoding (+9 more)

### Community 51 - ".Create"
Cohesion: 0.07
Nodes (21): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, Fact, IEnumerable (+13 more)

### Community 52 - "JsonElement"
Cohesion: 0.16
Nodes (11): DynamicSceneLayout, CancellationToken, GltfOperationProfile, InterchangeBaseline, JsonDocument, JsonElement, ReadOnlyMemory, Translation (+3 more)

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.29
Nodes (8): BinaryWriter, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification, OperationCounts, ValidatorCode

### Community 54 - "IReadOnlyList"
Cohesion: 0.12
Nodes (19): AnimationLayout, Discarded, PartitionMatch, IReadOnlyList, MemoryStream, ByteArrayComparer, GeometryPartition, ParsedGltfMesh (+11 more)

### Community 55 - "GltfImportPlanSerializer"
Cohesion: 0.12
Nodes (10): CancellationToken, IEnumerable, IReadOnlyDictionary, JsonElement, SeparateGltfPackage, Stream, Task, GltfImportPlanSerializer (+2 more)

### Community 56 - "Entity"
Cohesion: 0.06
Nodes (28): EarthTool.PAR.Models.Serialization, Encoding, IBinarySerializable, EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List (+20 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "GltfCliReportOperation"
Cohesion: 0.15
Nodes (14): GltfExportReceipt, NativeProjectionFingerprint, Guid, int, IReadOnlyList, string, Utf8JsonWriter, GltfCliReport (+6 more)

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - ".CreateJson"
Cohesion: 0.14
Nodes (6): DynamicAnimationLayout, DynamicAnimationTrack, DynamicImageLayout, DynamicMeshLayout, BinaryWriter, Utf8JsonWriter

### Community 62 - "EarthTool.PAR.Enums"
Cohesion: 0.11
Nodes (4): EarthTool.PAR.Extensions, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Models

### Community 63 - "DestructibleEntity"
Cohesion: 0.05
Nodes (30): ArtifactType, ExplosionFlags, PassiveMask, StandType, StoreableFlags, WasteSize, Encoding, IEnumerable (+22 more)

### Community 64 - ".EditStatic"
Cohesion: 0.33
Nodes (3): Guid, IEnumerable, WalkingSkeletonConsumer

### Community 65 - "ConvertCommand"
Cohesion: 0.23
Nodes (9): CommonCommand, CommonSettings, IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, Settings (+1 more)

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "MetadataEnvelope"
Cohesion: 0.13
Nodes (23): Guid, Projection, Version, MetadataConflictException, MetadataEnvelope, bool, GltfOperationProfile, IEnumerable (+15 more)

### Community 68 - "VerticalTransporter"
Cohesion: 0.12
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 72 - "InteractableEntity"
Cohesion: 0.08
Nodes (18): BarrelBetaType, ConnectorType, ShadowType, TargetType, WeaponFireType, Encoding, IEnumerable, InteractableEntity (+10 more)

### Community 73 - "Task"
Cohesion: 0.06
Nodes (30): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+22 more)

### Community 74 - "Equipment"
Cohesion: 0.09
Nodes (18): LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable, Equipment (+10 more)

### Community 75 - "MshOperationProfile"
Cohesion: 0.10
Nodes (14): HashSet, IReadOnlyList, List, AuthoringValidation, CanonicalHorizontalExtents, CanonicalStaticFootprint, CanonicalStaticObjectRole, CanonicalStaticRenderObject (+6 more)

### Community 76 - "ParameterReaderTests.cs"
Cohesion: 0.14
Nodes (10): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.PAR.Tests.Services, EarthTool.PAR.Tests.Factories, EarthTool.PAR.Tests.Models, EarthTool.PAR.Factories, MissileSerializationTests (+2 more)

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.04
Nodes (37): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Factories, EarthTool.TEX, EarthTool.PAR, EarthTool.WD.Tests.Services (+29 more)

### Community 79 - "Program"
Cohesion: 0.40
Nodes (3): AppBuilder, STAThread, Program

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - "GltfWalkingSkeletonTests"
Cohesion: 0.06
Nodes (11): Action, Guid, IReadOnlyList, JsonDocument, JsonElement, JsonObject, List, Vector3 (+3 more)

### Community 82 - "StaticMeshAsset"
Cohesion: 0.15
Nodes (10): GltfArtistObjectLocalIds, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, NativeProjectionFingerprint, Utf8JsonWriter, GltfPackage, StaticMeshAsset (+2 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.20
Nodes (10): Animations, Blender 4.5 glTF round-trip research, Conclusion, Evidence model, Meshes, primitives, and topology, Nodes, hierarchy, scenes, and transforms, Primary sources, Punctual lights (+2 more)

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.18
Nodes (6): ContentFingerprint, IEnumerable, Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "ParFile"
Cohesion: 0.12
Nodes (14): Reader, FileType, ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile (+6 more)

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "MeshAssetLineageId"
Cohesion: 0.13
Nodes (14): ArgumentException, Guid, MeshAssetLineageId, MeshAssetOrigin, IEnumerable, MshExpert, Dictionary, Func (+6 more)

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "glTF API"
Cohesion: 0.40
Nodes (5): Dynamic effect-preview contract, glTF API, Reports and compatibility, Static authoring authority and inference matrix, Static-light authoring contract

### Community 96 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.09
Nodes (17): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, EarthTool.Common.GUI.Views, IServiceCollection, ServiceCollectionExtensions (+9 more)

### Community 97 - "NotificationService"
Cohesion: 0.19
Nodes (7): NotificationType, Exception, NotificationEventArgs, Exception, ILogger, NotificationService, EventArgs

### Community 98 - "EarthTool Suite"
Cohesion: 0.22
Nodes (10): EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management, MSH Model Export Workflow (+2 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "GltfContracts.cs"
Cohesion: 0.11
Nodes (19): Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfArtistObjectLocalIds, GltfDiagnosticCodes, GltfExportOptions, GltfMeshResourceLimits (+11 more)

### Community 101 - "IArchiveItem"
Cohesion: 0.09
Nodes (14): CommandSettings, EarthTool.CLI.Commands, Type, CommandTypeResolver, CommonSettings, ReadOnlyMemory, IArchiveItem, HashSet (+6 more)

### Community 102 - "EarthTool Documentation"
Cohesion: 0.25
Nodes (9): Dependency Inversion, EarthTool Architecture, Interface Segregation, Layered Modular Architecture, Central Package Management, EarthTool Project Structure, EarthTool Module Dependency Rules, EarthTool Test Project Structure (+1 more)

### Community 103 - "EarthTool.Common"
Cohesion: 0.25
Nodes (9): EarthTool.CLI, EarthTool.Common, EarthTool.DAE, EarthTool.MSH, EarthTool.PAR, EarthTool.TEX, EarthTool.WD, EarthTool.WD.GUI (+1 more)

### Community 104 - "Entity"
Cohesion: 0.25
Nodes (9): PAR Parameter Editing, PAR Binary Serialization, EarthTool PAR Entity Hierarchy, Entity, IBinarySerializable, ParameterEntry, Polymorphic JSON Deserialization, Research (+1 more)

### Community 105 - "DestructibleEntity"
Cohesion: 0.28
Nodes (9): Capability Stacking Architecture, DestructibleEntity, EquipableEntity, Equipment, InteractableEntity, PassiveEntity, TypedEntity, Vehicle (+1 more)

### Community 106 - "WorkerContext"
Cohesion: 0.19
Nodes (9): List, KhronosValidatorServer, ValidatorResult, WorkerContext, IAsyncDisposable, Process, ValidatorCode, ValidatorResult (+1 more)

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - "IUndoRedoService"
Cohesion: 0.06
Nodes (23): Action, DateTime, UndoAction, Action, IEnumerable, IUndoRedoService, Action, IEnumerable (+15 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.07
Nodes (29): AsyncCommand, Stream, CliReportFileSystem, ICliReportFileSystem, CancellationToken, Exception, Func, IEnumerable (+21 more)

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

### Community 116 - "TexPreviewLoader"
Cohesion: 0.07
Nodes (31): Func, IEnumerable, IReadOnlyList, SafeResourceLookup, SafeResourceMatch, BinaryReader, byte, CancellationToken (+23 more)

### Community 117 - "PropertyEditorViewModel"
Cohesion: 0.14
Nodes (16): Action, IEnumerable, IPropertyEditorFactory, Action, HashSet, IEnumerable, ILogger, Type (+8 more)

### Community 118 - "MainWindow"
Cohesion: 0.16
Nodes (8): Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs, RoutedEventArgs, Window

### Community 119 - ".GenerateSampleData"
Cohesion: 0.12
Nodes (9): bool, ReadOnlyMemory, ArchiveItem, Fact, ArchiveItemTests, Fact, MemoryMappedFile, string (+1 more)

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

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
Cohesion: 0.15
Nodes (12): CliProcessResult, CliReportOperation, IReadOnlyList, JsonElement, string, Task, CliBatchOracleResult, CliDiagnostic (+4 more)

### Community 127 - "Modify An Existing Mesh"
Cohesion: 0.29
Nodes (7): 1. Extract and export, 2. Import into Blender, 3. Edit or add geometry, 4. Preview all animation classes, 5. Export from Blender, 6. Import the edit and install it, Modify An Existing Mesh

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "TreeItemViewModel"
Cohesion: 0.10
Nodes (12): DateTime, int, long, string, ArchiveInfoViewModel, ArchiveItemViewModel, HashSet, bool (+4 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 135 - "ConvertCommand"
Cohesion: 0.14
Nodes (14): IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+6 more)

### Community 136 - "Dependabot Dependency Automation"
Cohesion: 0.50
Nodes (4): Dependabot Dependency Automation, Weekly GitHub Actions Updates, Weekly NuGet Updates, Security Check Job

### Community 137 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 138 - "Setup .NET Environment"
Cohesion: 0.67
Nodes (3): .NET SDK Setup, NuGet Package Cache, Setup .NET Environment

### Community 139 - "Mesh Attachments 1..49"
Cohesion: 0.67
Nodes (3): Trailing Hierarchy Unwind Count, Mesh Attachments 1..49, Mesh Extents

### Community 140 - "DynamicEffectExtension"
Cohesion: 0.05
Nodes (31): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, ReadOnlySpan, DynamicEffectExtension (+23 more)

### Community 143 - ".WriteReconciledRecord"
Cohesion: 0.15
Nodes (6): ICollection, ISet, Stream, End, NativeObjectGraph, Start

### Community 148 - "ITexFile"
Cohesion: 0.15
Nodes (13): IEnumerable, TexHeader, TexImage, ITexFile, BinaryReader, IEnumerable, TexFile, TexHeader (+5 more)

### Community 149 - "Mesh Artist Quick Start And Cheat Sheet"
Cohesion: 0.29
Nodes (7): Attachment Identifier Cheat Sheet, Choose The Correct Workflow, Create A Standalone MSH, Directional Empty Presentation In Blender, Fast Checks Before Import, Mesh Artist Quick Start And Cheat Sheet, Read the import report

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 152 - "Migrate From COLLADA To glTF"
Cohesion: 0.33
Nodes (6): API migration, Attachment helper name migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 154 - "FlagsPropertyEditorViewModel"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 155 - "CommandTypeRegistrar"
Cohesion: 0.08
Nodes (14): Application, Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, IServiceCollection, App (+6 more)

### Community 156 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 158 - "MshCanonicalSerializer"
Cohesion: 0.09
Nodes (25): CanonicalStaticRecord, IReadOnlyList, Matrix4x4, Vector3, StaticAnimationTracks, StaticRenderObject, CanonicalStaticVertex, CanonicalTriangle (+17 more)

### Community 159 - "Runner"
Cohesion: 0.16
Nodes (8): ChannelReader, ChannelWriter, DynamicCoverage, CliOracleResult, Guid, Task, Runner, ValidatorAggregate

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - "ViewLocator"
Cohesion: 0.10
Nodes (11): EarthTool.TEX.GUI, Control, ViewLocator, AppBuilder, STAThread, Program, Control, ViewLocator (+3 more)

### Community 164 - "Decision consequences for later tickets"
Cohesion: 0.40
Nodes (5): Decision consequences for later tickets, EarthTool metadata requirements, Native glTF candidates, Required fingerprints and invalidation, What stock Blender cannot promise

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Historical Measured Result, Official MSH Qualification Performance, Stage Profiling, Blender matrix, Local pre-publish qualification, Official MSH corpus

### Community 167 - "Tested build and fixture"
Cohesion: 0.67
Nodes (3): Diagnostic asset, Stock options, Tested build and fixture

### Community 168 - "Extras and custom properties"
Cohesion: 0.67
Nodes (3): Extras and custom properties, JSON value behavior, Scope survival matrix

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 171 - "ItemCommand"
Cohesion: 0.25
Nodes (6): EarthTool.CLI.Commands.PAR, CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings

### Community 172 - "Underscore-prefixed custom attributes"
Cohesion: 0.67
Nodes (3): Identity, order, collision, and merge behavior, Supported import shapes, Underscore-prefixed custom attributes

### Community 173 - "IExtractor"
Cohesion: 0.33
Nodes (3): Task, IExtractor, IWDExtractor

### Community 175 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 176 - "EnumPropertyEditorViewModel"
Cohesion: 0.29
Nodes (5): object, ObservableCollection, Type, EnumPropertyEditorViewModel, EnumValueViewModel

### Community 177 - "Missile"
Cohesion: 0.14
Nodes (8): DamageFlags, HitType, MissileType, RocketType, Encoding, IEnumerable, Missile, Fact

### Community 178 - "MappedArchiveDataSource"
Cohesion: 0.10
Nodes (14): ReadOnlyMemory, IArchiveDataSource, ReadOnlyMemory, InMemoryArchiveDataSource, int, MemoryMappedFile, ReadOnlyMemory, MappedArchiveDataSource (+6 more)

### Community 179 - "Task"
Cohesion: 0.09
Nodes (3): Fact, Task, Action

### Community 180 - "IReadOnlyList"
Cohesion: 0.16
Nodes (14): DynamicEffectPreview, DynamicObjectScope, DynamicRecordSlice, IDictionary, IEnumerable, IReadOnlyDictionary, IReadOnlyList, NativeProjectionFingerprint (+6 more)

### Community 181 - "StaticSourceObject"
Cohesion: 0.13
Nodes (15): IReadOnlyDictionary, SourceObjectId, StaticAnimationClass, StaticRenderObjectFlagMasks, StaticRenderObjectFlags, StaticSourceObject, IReadOnlyDictionary, IDictionary (+7 more)

### Community 182 - "EquipableEntity"
Cohesion: 0.07
Nodes (18): BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags, SpaceStationType (+10 more)

### Community 183 - "QualificationProfiler"
Cohesion: 0.13
Nodes (15): Dictionary, IDictionary, int, IReadOnlyDictionary, ISet, long, object, string (+7 more)

### Community 185 - "AnimationClassBytes"
Cohesion: 0.12
Nodes (7): byte, int, IReadOnlyList, ReadOnlySpan, Span, CommonMeshBaseHeader, AnimationClassBytes

### Community 186 - ".Write_And_Read_AreSymmetric"
Cohesion: 0.29
Nodes (5): Writer, Fact, ParameterWriterTests, Encoding, ParTestData

### Community 189 - ".Create"
Cohesion: 0.07
Nodes (12): AttachmentRecord, BlenderOutputEvidence, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord, IEnumerable (+4 more)

## Knowledge Gaps
- **335 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+330 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `IArchiveItem`, `IValueConverter`, `IEarthInfo`, `ParFile`, `EarthTool.MSH.Assets`?**
  _High betweenness centrality (0.184) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `MetadataEnvelope`, `GltfContracts.cs`, `GlbDocument.cs`, `MshOperationProfile`, `DynamicEffectExtension`, `DynamicGltfInterchangeTests`, `StaticAnimationProjection`, `StaticSourceObject`, `GltfCliReportOperation`, `MeshAssetLineageId`?**
  _High betweenness centrality (0.111) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `Task` to `DynamicGltfInterchangeTests`, `IArchiveItem`, `GltfImportPlanSerializer`?**
  _High betweenness centrality (0.090) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _335 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.WriteFileAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.11806543385490754 - nodes in this community are weakly interconnected._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.11149825783972125 - nodes in this community are weakly interconnected._