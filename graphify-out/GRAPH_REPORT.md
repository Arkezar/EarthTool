# Graph Report - EarthTool  (2026-08-05)

## Corpus Check
- 369 files · ~300,716 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4788 nodes · 14493 edges · 178 communities (174 shown, 4 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 1028 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `4e4b3b3a`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- .WriteFileAsync
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- DynamicGltfDocument
- IReadOnlyList
- .ToByteArray
- .Load
- IValueConverter
- MainWindowViewModel
- OperationResult
- DynamicGltfInterchangeTests
- Vector3
- MshV1Decoder
- .OpenArchive
- DynamicSemanticFailure
- Dynamic MESH Binary Layout
- GltfImportPlanSerializer
- MshOperationProfile
- release-qualification.mjs
- MainWindowViewModel
- WDExtractorTests
- MainWindowViewModel
- ITransactionalFileSystem
- Vehicle
- Common MSH Base Header
- MetadataGraphValidationTests
- ArchiveTests
- EarthTool.MSH.Assets
- ArchiverServiceTests
- EarthTool.CLI
- DynamicMeshAssetTests
- .Create
- .ResolveMetadataConflicts
- Program
- CanonicalDynamicObject
- StaticMeshEditSession
- EarthTool.CLI.Commands.WD
- DynamicGltfDocument
- EarthTool.WD.Models
- EarthTool.PAR.GUI.ViewModels
- GltfInterchange
- Static Mesh Header
- StaticObject Record
- JsonElement
- StaticAnimationProjection
- EntityDetailsViewModel
- ResearchReferenceCollectionEditorViewModel
- PublicApiApproval
- IEarthInfo
- .Create
- GlbDocument
- OfficialCorpusQualification
- IReadOnlyList
- DynamicEffectType
- Entity
- EarthTool.MSH.Tests
- GltfPlanAndReport.cs
- DialogService
- EarthTool.PAR
- EarthTool.PAR.Enums
- DestructibleEntity
- EditableEntity
- ConvertCommand
- EarthTool.PAR.GUI
- MetadataEnvelope
- VerticalTransporter
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- OneTriangleMshFixture
- Equipment
- Task
- InteractableEntity
- AuthoringValidation
- ParameterReaderTests.cs
- EarthTool.sln
- EarthTool.Common.Interfaces
- JsonElement
- BinaryExtensions
- Fact
- StaticMeshAsset
- Blender 4.5 glTF round-trip research
- DynamicEffectBehaviorTests
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- ParFile
- ICompressor
- .WriteReconciledRecord
- EarthTool WD Archive Manager
- MeshAssetLineageId
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- glTF API
- .CreateMockHeader
- EarthTool.Common.GUI.Enums
- NotificationService
- EarthTool Suite
- WD Central Directory
- OperationDiagnostic
- IArchiveItem
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- WorkerContext
- Reader
- IUndoRedoService
- JsonElement
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- QualificationProfiler
- PropertyEditorFactory
- MainWindow
- .GenerateSampleData
- EarthTool.TEX
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- Modify An Existing Mesh
- PassiveEntity
- Base Header
- ArchiveInfoViewModel
- UnitTest1.cs
- EarthTool Installation Guide
- DynamicCoverage
- ParsedGltfAnimationChannel
- ConvertCommand
- Dependabot Dependency Automation
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- Setup .NET Environment
- Mesh Attachments 1..49
- DynamicEffectExtension
- Code Quality Analysis Job
- Dynamic Color
- DynamicEffectRecipeTests
- .Match
- Mesh Artist Quick Start And Cheat Sheet
- package.json
- EarthTool.CLI/Program.cs
- Migrate From COLLADA To glTF
- FlagsPropertyEditorViewModel
- App
- MshCanonicalSerializer
- Runner
- validate-glb.mjs
- EarthTool.TEX.GUI/App.axaml.cs
- Decision consequences for later tickets
- Official MSH Qualification Performance
- Tested build and fixture
- Extras and custom properties
- EarthTool.CLI.Tests
- GltfWalkingSkeletonTests
- TreeNodeViewModelBase
- Underscore-prefixed custom attributes
- IExtractor
- TexPreviewLoader
- .ToByteArray
- EnumPropertyEditorViewModel
- Task
- IReadOnlyList
- EquipableEntity
- .Create

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 249 edges
2. `GltfInterchange` - 201 edges
3. `GlbDocument` - 152 edges
4. `DynamicGltfDocument` - 121 edges
5. `DynamicGltfInterchangeTests` - 96 edges
6. `OperationDiagnostic` - 90 edges
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

## Communities (178 total, 4 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - ".WriteFileAsync"
Cohesion: 0.12
Nodes (12): Stream, ITransactionalFileSystem, TransactionalFileSystem, CancellationToken, Exception, IEnumerable, ILogger, Stream (+4 more)

### Community 2 - "AssetResult"
Cohesion: 0.26
Nodes (6): AssetResult, DiagnosticKey, AssetResult, OperationCounts, ProfileScope, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.10
Nodes (16): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+8 more)

### Community 5 - "DynamicGltfDocument"
Cohesion: 0.17
Nodes (8): Translation, Vector2, Vector3, DynamicAnimationTrack, DynamicEditedPreview, DynamicEffectPreview, NativeObjectScope, ModelScale

### Community 6 - "IReadOnlyList"
Cohesion: 0.07
Nodes (33): Discarded, IDictionary, IReadOnlyDictionary, IReadOnlyList, ISet, List, Matrix4x4, EmitterOwnershipPlan (+25 more)

### Community 7 - ".ToByteArray"
Cohesion: 0.07
Nodes (23): Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding, IEnumerable (+15 more)

### Community 8 - ".Load"
Cohesion: 0.11
Nodes (16): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+8 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.10
Nodes (10): Task, IParFileService, bool, ILogger, ObservableCollection, ReactiveCommand, string, Task (+2 more)

### Community 11 - "OperationResult"
Cohesion: 0.11
Nodes (23): IReadOnlyList, OperationResult, GltfDynamicEditImportResult, GltfEditImportOptions, GltfEditImportResult, GltfExportReceipt, GltfMeshEditImportResult, GltfMetadataConflictResolution (+15 more)

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.07
Nodes (19): DynamicMeshAsset, Action, Fact, Guid, IEnumerable, InlineData, int, IReadOnlyList (+11 more)

### Community 13 - "Vector3"
Cohesion: 0.10
Nodes (12): Action, BinaryWriter, float, MemoryStream, Quaternion, Translation, Vector3, AttachmentHeadingProjection (+4 more)

### Community 14 - "MshV1Decoder"
Cohesion: 0.10
Nodes (24): DecodedStaticRecord, int, ReadOnlySpan, DynamicMeshDecoder, CancellationToken, IReadOnlyDictionary, IReadOnlyList, List (+16 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "DynamicSemanticFailure"
Cohesion: 0.13
Nodes (11): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, Vector3, DynamicEffectEvaluation (+3 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "GltfImportPlanSerializer"
Cohesion: 0.06
Nodes (31): BufferPath, ConflictKey, Directory, CancellationToken, IEnumerable, IReadOnlyDictionary, JsonElement, SeparateGltfPackage (+23 more)

### Community 19 - "MshOperationProfile"
Cohesion: 0.10
Nodes (19): CancellationToken, Stream, Task, Guid, IReadOnlyList, AnimationClassBytes, CommonMeshBaseHeader, MeshArchiveFraming (+11 more)

### Community 20 - "release-qualification.mjs"
Cohesion: 0.07
Nodes (62): corpusBinaryStages, corpusInterchangeStages, recognizedDynamicEffectTypes, assertPrivacySafe(), buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames() (+54 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.09
Nodes (16): INotificationService, Task, bool, HashSet, ILogger, object, ObservableCollection, ReactiveCommand (+8 more)

### Community 22 - "WDExtractorTests"
Cohesion: 0.25
Nodes (9): DateTime, Guid, Fact, string, Task, WDExtractorTests, ILogger, Task (+1 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.09
Nodes (14): Bitmap, IEnumerable, Task, IDialogService, ILogger, int, List, ObservableCollection (+6 more)

### Community 24 - "ITransactionalFileSystem"
Cohesion: 0.06
Nodes (10): Stream, ITransactionalFileSystem, TransactionalFileSystem, CancellationTokenSource, Stream, CancellingAfterSidecarTransactionalFileSystem, CorruptingSidecarTransactionalFileSystem, FailingManifestTransactionalFileSystem (+2 more)

### Community 25 - "Vehicle"
Cohesion: 0.09
Nodes (18): VehicleObjectType, Encoding, IEnumerable, Builder, Encoding, IEnumerable, Harvester, Encoding (+10 more)

### Community 26 - "Common MSH Base Header"
Cohesion: 0.10
Nodes (23): Model MSH Framing and Record Extensions Explicitly, Canonical Next Record Markers, MSH Footprint API, MSH Horizontal Extents API, IMeshBaseHeader, Legacy MSH Model Migration, MSH API, MSH Slots API (+15 more)

### Community 27 - "MetadataGraphValidationTests"
Cohesion: 0.13
Nodes (13): Baseline, Action, Bytes, Fact, Func, Guid, ICollection, InlineData (+5 more)

### Community 28 - "ArchiveTests"
Cohesion: 0.13
Nodes (9): IEarthInfoFactory, bool, DateTime, IReadOnlyCollection, MemoryMappedFile, Archive, Fact, ArchiveTests (+1 more)

### Community 29 - "EarthTool.MSH.Assets"
Cohesion: 0.05
Nodes (51): AsyncCommand, CliResult, EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.CLI.Commands.MSH (+43 more)

### Community 30 - "ArchiverServiceTests"
Cohesion: 0.15
Nodes (10): CancellationToken, CommandContext, CancellationToken, CommandContext, DateTime, Guid, IArchiver, Fact (+2 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Asset, byte, Bytes, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - ".Create"
Cohesion: 0.13
Nodes (9): int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture, OmniRecord (+1 more)

### Community 34 - ".ResolveMetadataConflicts"
Cohesion: 0.09
Nodes (8): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, MetadataConflictResolutionResult, ParseScopeResolution, ISet, ParsedGlb, MetadataConflictResolutionResult

### Community 35 - "Program"
Cohesion: 0.50
Nodes (3): AppBuilder, STAThread, Program

### Community 36 - "CanonicalDynamicObject"
Cohesion: 0.26
Nodes (10): IEnumerable, Vector3, CanonicalDynamicAlpha, CanonicalDynamicEffectShape, CanonicalDynamicFrameSequence, CanonicalDynamicRecipe, CanonicalDynamicSpriteSheet, CanonicalDynamicTerrainLight (+2 more)

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.09
Nodes (22): UnchangedEmitterOwnership, SourceObjectId, StaticRenderObjectId, StaticSourceObject, bool, Dictionary, HashSet, ICollection (+14 more)

### Community 38 - "EarthTool.CLI.Commands.WD"
Cohesion: 0.07
Nodes (33): Command, CommandSettings, CommonSettings, AddCommand, CreateCommand, CancellationToken, CommandContext, DebugCommand (+25 more)

### Community 39 - "DynamicGltfDocument"
Cohesion: 0.10
Nodes (13): float, int, string, uint, DynamicAnimationLayout, DynamicGltfDocument, DynamicImageLayout, DynamicMeshLayout (+5 more)

### Community 40 - "EarthTool.WD.Models"
Cohesion: 0.07
Nodes (21): EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Models, EarthTool.WD.Interfaces, EarthTool.WD.Models, ReadOnlyMemory, IArchiveDataSource, bool, ReadOnlyMemory (+13 more)

### Community 41 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.08
Nodes (11): EarthTool.PAR.GUI, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, EarthTool.Common.GUI, ValidationError, ValidationSeverity (+3 more)

### Community 42 - "GltfInterchange"
Cohesion: 0.04
Nodes (28): AnimationEditPlan, AnimationReplacement, Action, BinaryWriter, Func, ICollection, IEnumerable, IReadOnlyCollection (+20 more)

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - "JsonElement"
Cohesion: 0.16
Nodes (5): JsonDocument, JsonElement, Matrix4x4, ReadOnlySpan, GltfImportIntent

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.15
Nodes (13): BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionFingerprint, AnimationProjectionSet (+5 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.09
Nodes (21): Action, IEnumerable, IPropertyEditorFactory, Action, bool, IEnumerable, ILogger, ObservableCollection (+13 more)

### Community 48 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.14
Nodes (13): ViewModelBase, bool, Dictionary, EditableResearch, Action, bool, IEnumerable, ObservableCollection (+5 more)

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
Cohesion: 0.08
Nodes (13): CarrierKind, GltfOperationProfile, Guid, ICollection, IDictionary, Path, uint, Value (+5 more)

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.17
Nodes (10): ContentFingerprint, BinaryWriter, IEnumerable, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification (+2 more)

### Community 54 - "IReadOnlyList"
Cohesion: 0.13
Nodes (16): AnimationObjectLayout, PartitionMatch, IReadOnlyList, NativeProjectionFingerprint, AnimationLayout, ByteArrayComparer, GeometryPartition, ParsedGltfPrimitive (+8 more)

### Community 55 - "DynamicEffectType"
Cohesion: 0.16
Nodes (12): DynamicEffectType, IReadOnlyDictionary, IReadOnlyList, DynamicAuthoringDefaults, DynamicAuthoringRequirement, DynamicBehaviorField, DynamicBehaviorFinding, DynamicEffectBehavior (+4 more)

### Community 56 - "Entity"
Cohesion: 0.10
Nodes (20): EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List, ValidationResult, IEnumerable, ILogger (+12 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "GltfPlanAndReport.cs"
Cohesion: 0.14
Nodes (10): Guid, int, IReadOnlyList, string, Utf8JsonWriter, GltfCliReport, GltfCliReportFormat, GltfCliReportOperationKind (+2 more)

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 62 - "EarthTool.PAR.Enums"
Cohesion: 0.10
Nodes (4): EarthTool.PAR.Extensions, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Models

### Community 63 - "DestructibleEntity"
Cohesion: 0.05
Nodes (28): DamageFlags, ExplosionFlags, HitType, MissileType, RocketType, StandType, StoreableFlags, WasteSize (+20 more)

### Community 64 - "EditableEntity"
Cohesion: 0.17
Nodes (8): EntityClassType, bool, Dictionary, EditableEntity, ObservableCollection, EntityListItemViewModel, NewValue, OldValue

### Community 65 - "ConvertCommand"
Cohesion: 0.09
Nodes (21): CommonCommand, CommonSettings, CancellationToken, CommandContext, Task, CommonCommand, IEnumerable, JsonSerializerOptions (+13 more)

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "MetadataEnvelope"
Cohesion: 0.15
Nodes (18): bool, GltfOperationProfile, IEnumerable, int, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList, List (+10 more)

### Community 68 - "VerticalTransporter"
Cohesion: 0.12
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 72 - "Equipment"
Cohesion: 0.09
Nodes (18): LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable, Equipment (+10 more)

### Community 73 - "Task"
Cohesion: 0.06
Nodes (30): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+22 more)

### Community 74 - "InteractableEntity"
Cohesion: 0.09
Nodes (17): BarrelBetaType, ShadowType, TargetType, WeaponFireType, Encoding, IEnumerable, InteractableEntity, Encoding (+9 more)

### Community 75 - "AuthoringValidation"
Cohesion: 0.10
Nodes (14): Guid, IReadOnlyList, Vector2, AuthoringValidation, CanonicalHorizontalExtents, CanonicalStaticFootprint, CanonicalStaticObjectRole, CanonicalStaticRenderObject (+6 more)

### Community 76 - "ParameterReaderTests.cs"
Cohesion: 0.12
Nodes (11): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.PAR.Tests.Services, EarthTool.PAR.Tests.Factories, EarthTool.PAR.Tests.Models, EarthTool.PAR.Factories, Fact (+3 more)

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.04
Nodes (35): EarthTool.WD.GUI.ViewModels, EarthTool.CLI.Commands.PAR, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.TEX, EarthTool.PAR, EarthTool.WD.Tests.Services (+27 more)

### Community 79 - "JsonElement"
Cohesion: 0.40
Nodes (4): DynamicEditedPreview, JsonElement, ReadOnlySpan, DynamicPreviewException

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - "Fact"
Cohesion: 0.09
Nodes (8): AnimationLengths, JsonDocument, JsonElement, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 82 - "StaticMeshAsset"
Cohesion: 0.15
Nodes (12): AnimationLayout, GltfArtistObjectLocalIds, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, Utf8JsonWriter, GltfPackage, PartitionLayout (+4 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.20
Nodes (10): Animations, Blender 4.5 glTF round-trip research, Conclusion, Evidence model, Meshes, primitives, and topology, Nodes, hierarchy, scenes, and transforms, Primary sources, Punctual lights (+2 more)

### Community 84 - "DynamicEffectBehaviorTests"
Cohesion: 0.21
Nodes (6): Action, Fact, DynamicEffectBehaviorTests, RepresentationTestCase, IReadOnlySet, RepresentationTestCase

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "ParFile"
Cohesion: 0.08
Nodes (19): Writer, ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile, BinaryReader (+11 more)

### Community 88 - "ICompressor"
Cohesion: 0.09
Nodes (15): Encoding, IArchiveFactory, Stream, ICompressor, ReadOnlySpan, Stream, IDecompressor, PathValidator (+7 more)

### Community 89 - ".WriteReconciledRecord"
Cohesion: 0.16
Nodes (6): ICollection, ISet, Stream, End, NativeObjectGraph, Start

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "MeshAssetLineageId"
Cohesion: 0.14
Nodes (7): Guid, IEnumerable, WalkingSkeletonConsumer, MeshAssetLineageId, MshBuildResult, IEnumerable, MshExpert

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "glTF API"
Cohesion: 0.40
Nodes (5): Dynamic effect-preview contract, glTF API, Reports and compatibility, Static authoring authority and inference matrix, Static-light authoring contract

### Community 95 - ".CreateMockHeader"
Cohesion: 0.26
Nodes (3): Fact, ArchiveItemTests, Guid

### Community 96 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.09
Nodes (16): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, EarthTool.Common.GUI.Views, IServiceCollection, ServiceCollectionExtensions (+8 more)

### Community 97 - "NotificationService"
Cohesion: 0.19
Nodes (7): NotificationType, Exception, NotificationEventArgs, Exception, ILogger, NotificationService, EventArgs

### Community 98 - "EarthTool Suite"
Cohesion: 0.22
Nodes (10): EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management, MSH Model Export Workflow (+2 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "OperationDiagnostic"
Cohesion: 0.08
Nodes (22): Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfArtistObjectLocalIds, GltfDiagnosticCodes, GltfExportOptions, GltfLightHandle (+14 more)

### Community 101 - "IArchiveItem"
Cohesion: 0.08
Nodes (16): Type, CommandTypeResolver, DateTime, IReadOnlyCollection, IArchive, DateTime, Guid, ReadOnlyMemory (+8 more)

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
Cohesion: 0.17
Nodes (10): List, KhronosValidatorServer, ValidatorResult, WorkerContext, IAsyncDisposable, KhronosValidatorServer, Process, ValidatorCode (+2 more)

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - "IUndoRedoService"
Cohesion: 0.06
Nodes (23): Action, DateTime, UndoAction, Action, IEnumerable, IUndoRedoService, Action, IEnumerable (+15 more)

### Community 109 - "JsonElement"
Cohesion: 0.18
Nodes (10): DynamicSceneLayout, CancellationToken, GltfOperationProfile, IEnumerable, InterchangeBaseline, JsonDocument, ReadOnlyMemory, DynamicGltfImport (+2 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.09
Nodes (16): Stream, CliReportFileSystem, ICliReportFileSystem, CancellationToken, Exception, Func, IEnumerable, IReadOnlyList (+8 more)

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

### Community 116 - "QualificationProfiler"
Cohesion: 0.20
Nodes (11): Dictionary, int, long, object, string, ProfileScope, QualificationProfiler, TimingAggregate (+3 more)

### Community 117 - "PropertyEditorFactory"
Cohesion: 0.27
Nodes (7): Action, HashSet, IEnumerable, ILogger, Type, PropertyEditorFactory, PropertyInfo

### Community 118 - "MainWindow"
Cohesion: 0.16
Nodes (8): Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs, RoutedEventArgs, Window

### Community 119 - ".GenerateSampleData"
Cohesion: 0.24
Nodes (4): Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

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
Cohesion: 0.14
Nodes (13): CliProcessResult, CliReportOperation, IReadOnlyList, JsonElement, string, Task, CliBatchOracleResult, CliDiagnostic (+5 more)

### Community 127 - "Modify An Existing Mesh"
Cohesion: 0.29
Nodes (7): 1. Extract and export, 2. Import into Blender, 3. Edit or add geometry, 4. Preview all animation classes, 5. Export from Blender, 6. Import the edit and install it, Modify An Existing Mesh

### Community 128 - "PassiveEntity"
Cohesion: 0.17
Nodes (9): ArtifactType, PassiveMask, Encoding, IEnumerable, PassiveEntity, Encoding, IEnumerable, Artifact (+1 more)

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "ArchiveInfoViewModel"
Cohesion: 0.17
Nodes (7): DateTime, int, long, string, ArchiveInfoViewModel, ArchiveItemViewModel, ViewModelBase

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - "DynamicCoverage"
Cohesion: 0.22
Nodes (5): DynamicCoverage, IDictionary, IReadOnlyDictionary, ISet, DynamicCoverage

### Community 134 - "ParsedGltfAnimationChannel"
Cohesion: 0.33
Nodes (4): int, string, ParsedAnimationBuilder, ParsedGltfAnimationChannel

### Community 135 - "ConvertCommand"
Cohesion: 0.10
Nodes (18): IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand, CancellationToken, CommandContext (+10 more)

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
Cohesion: 0.40
Nodes (5): ReadOnlySpan, DynamicAlphaTiming, DynamicEffectExtension, DynamicLightType, EffectRectangle

### Community 143 - "DynamicEffectRecipeTests"
Cohesion: 0.42
Nodes (4): Fact, Guid, Task, DynamicEffectRecipeTests

### Community 149 - "Mesh Artist Quick Start And Cheat Sheet"
Cohesion: 0.29
Nodes (7): Attachment Identifier Cheat Sheet, Choose The Correct Workflow, Create A Standalone MSH, Directional Empty Presentation In Blender, Fast Checks Before Import, Mesh Artist Quick Start And Cheat Sheet, Read the import report

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "EarthTool.CLI/Program.cs"
Cohesion: 0.22
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 152 - "Migrate From COLLADA To glTF"
Cohesion: 0.33
Nodes (6): API migration, Attachment helper name migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 154 - "FlagsPropertyEditorViewModel"
Cohesion: 0.24
Nodes (6): bool, object, ObservableCollection, Type, FlagsPropertyEditorViewModel, FlagValueViewModel

### Community 155 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 158 - "MshCanonicalSerializer"
Cohesion: 0.08
Nodes (26): CanonicalStaticRecord, byte, Matrix4x4, Vector3, StaticAnimationClass, StaticAnimationTracks, StaticRenderObject, CanonicalStaticVertex (+18 more)

### Community 159 - "Runner"
Cohesion: 0.20
Nodes (6): ChannelReader, ChannelWriter, Guid, Task, Runner, ValidatorAggregate

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - "EarthTool.TEX.GUI/App.axaml.cs"
Cohesion: 0.09
Nodes (12): EarthTool.TEX.GUI, EarthTool.TEX.GUI.Views, Control, ViewLocator, AppBuilder, STAThread, Program, Control (+4 more)

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

### Community 170 - "GltfWalkingSkeletonTests"
Cohesion: 0.06
Nodes (13): BlenderOutputEvidence, Action, Guid, IEnumerable, InlineData, IReadOnlyList, List, Theory (+5 more)

### Community 171 - "TreeNodeViewModelBase"
Cohesion: 0.07
Nodes (26): Encoding, IBinarySerializable, Faction, ResearchType, ObservableCollection, EntityGroupNodeViewModel, ObservableCollection, EntityGroupsRootNodeViewModel (+18 more)

### Community 172 - "Underscore-prefixed custom attributes"
Cohesion: 0.67
Nodes (3): Identity, order, collision, and merge behavior, Supported import shapes, Underscore-prefixed custom attributes

### Community 173 - "IExtractor"
Cohesion: 0.33
Nodes (3): Task, IExtractor, IWDExtractor

### Community 174 - "TexPreviewLoader"
Cohesion: 0.05
Nodes (39): Func, IEnumerable, IReadOnlyList, SafeResourceLookup, SafeResourceMatch, BinaryReader, byte, CancellationToken (+31 more)

### Community 175 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 176 - "EnumPropertyEditorViewModel"
Cohesion: 0.29
Nodes (5): object, ObservableCollection, Type, EnumPropertyEditorViewModel, EnumValueViewModel

### Community 179 - "Task"
Cohesion: 0.09
Nodes (4): Fact, JsonObject, Task, Action

### Community 180 - "IReadOnlyList"
Cohesion: 0.12
Nodes (15): DynamicAnimationLayout, DynamicAnimationTrack, DynamicEffectPreview, DynamicImageLayout, DynamicMeshLayout, DynamicObjectScope, DynamicRecordSlice, BinaryWriter (+7 more)

### Community 182 - "EquipableEntity"
Cohesion: 0.07
Nodes (19): BuildingExType, BuildingTabType, BuildingType, ConnectorType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags (+11 more)

### Community 189 - ".Create"
Cohesion: 0.09
Nodes (7): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord, JsonNode

## Knowledge Gaps
- **334 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+329 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `ConvertCommand`, `ArchiveInfoViewModel`, `ConvertCommand`, `EarthTool.WD.Models`, `IValueConverter`, `IEarthInfo`, `ParFile`, `ArchiveTests`, `EarthTool.MSH.Assets`?**
  _High betweenness centrality (0.163) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `MetadataEnvelope`, `OperationDiagnostic`, `CanonicalDynamicObject`, `IReadOnlyList`, `AuthoringValidation`, `StaticAnimationProjection`, `DynamicSemanticFailure`, `MshOperationProfile`, `DynamicEffectType`, `GltfPlanAndReport.cs`?**
  _High betweenness centrality (0.112) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `Task` to `GltfImportPlanSerializer`, `CanonicalDynamicObject`, `IArchiveItem`?**
  _High betweenness centrality (0.104) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _334 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.WriteFileAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.11948790896159317 - nodes in this community are weakly interconnected._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10465116279069768 - nodes in this community are weakly interconnected._