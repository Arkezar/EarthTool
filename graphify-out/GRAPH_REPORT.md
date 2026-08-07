# Graph Report - EarthTool  (2026-08-07)

## Corpus Check
- 379 files · ~254,207 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4452 nodes · 11952 edges · 194 communities (185 shown, 9 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 662 edges (avg confidence: 0.81)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9b751213`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- EntityGroupNodeViewModel
- MshOperationProfile
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- DynamicEffectBehavior
- .Assemble
- GltfImportPlanSerializer
- .ResolveAndLoad
- IValueConverter
- MainWindowViewModel
- .ExportGltfFileAsync
- DynamicGltfInterchangeTests
- CanonicalStaticGltfCreationTests
- EarthTool.MSH.Assets
- .OpenArchive
- CanonicalDynamicGltfImporter
- Dynamic MESH Binary Layout
- GltfImportPlan
- GltfPlanAndReportTests
- release-qualification.mjs
- DynamicEffectType
- .Create
- MainWindowViewModel
- CancellingAfterSidecarTransactionalFileSystem
- Vehicle
- Common MSH Base Header
- App
- .CreateMockHeader
- ConvertCommand
- EarthTool.TEX
- EarthTool.CLI
- DynamicMeshAssetTests
- Task
- Research
- GlbDocument
- DynamicEffectBehaviorTests
- GltfWalkingSkeletonTests
- OperationResult
- CanonicalAuthoringMetadata
- DynamicSemanticFailure
- MshCanonicalSerializer
- GltfPlanAndReport.cs
- Static Mesh Header
- StaticObject Record
- .CreateEffectPreview
- ProjectedAnimationFrame
- EntityDetailsViewModel
- Vector3
- PublicApiApproval
- IEarthInfo
- CanonicalMeshAuthoringTests
- .WriteReportAsync
- PublicCutoverAcceptanceTests
- .ToByteArray
- EarthTool.PAR.Models.Abstracts
- Entity
- EarthTool.MSH.Tests
- CanonicalBaseHeaderEncoder
- DialogService
- EarthTool.PAR
- EarthTool.TEX.GUI/App.axaml.cs
- MainWindowViewModel
- CanonicalGltfMetadataContractTests
- MshDecodeContext
- .Match
- EarthTool.PAR.GUI
- EarthTool.WD.Models
- EarthTool.Common
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- ITransactionalFileSystem
- InteractableEntity
- Task
- StaticMeshAssetTests
- EditableEntity
- UndoRedoService
- EarthTool.sln
- EarthTool.Common.Interfaces
- IArchive
- BinaryExtensions
- .ExportGlbAsync
- Program
- Blender 4.5 glTF round-trip research
- StaticMeshSequenceFixture
- .RunAsync
- EarthTool.Common.GUI
- CanonicalStaticRenderObjectSequenceEncoder
- Task
- 0003-create-immutable-msh-assets-from-gltf.md
- EarthTool WD Archive Manager
- DynamicGltfDocument
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- TexPreviewLoader
- EarthTool.Common.GUI.Enums
- NotificationService
- EarthTool Suite
- WD Central Directory
- EarthTool.PAR.Enums
- .Write_And_Read_AreSymmetric
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- .NewModelImportRejectsAmbiguousOrOutOfRangeAnimationClasses
- Reader
- IArchiveItem
- GltfInterchange
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- OneTriangleMshFixture
- DynamicEffectRecipeTests
- .GenerateSampleData
- ArchiverService
- EarthTool.TEX
- EarthTool.PAR.Extensions
- ConvertCommand
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- DynamicEffectExtension
- MeshAssetAuthoring.cs
- Base Header
- Program
- UnitTest1.cs
- EarthTool Installation Guide
- CommandTypeRegistrar
- StaticHierarchy
- ExportGltfSettings
- Dependabot Dependency Automation
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- Setup .NET Environment
- Mesh Attachments 1..49
- ParameterReader
- Code Quality Analysis Job
- Dynamic Color
- EarthTool.CLI.Commands.WD
- EarthTool.PAR.Models
- Mesh Artist Quick Start And Cheat Sheet
- package.json
- .CreateStatic
- Migrate From COLLADA To glTF
- PropertyEditorViewModel
- FlagsPropertyEditorViewModel
- StaticMeshAsset
- PropertyEditorFactory
- OfficialCorpusQualification
- validate-glb.mjs
- TreeItemViewModel
- Decision consequences for later tickets
- Official MSH Qualification Performance
- Runner
- Tested build and fixture
- TexFile
- EarthTool.CLI.Tests
- ItemCommand
- WorkerContext
- ViewLocator
- ResearchReferenceCollectionEditorViewModel
- GltfSourceLossDiagnosticsTests
- .GetMinimumSerializedLength
- EnumPropertyEditorViewModel
- ParameterReaderTests
- Metadata Envelope Reference
- ManifestFailingFileSystem
- ParFile
- IntCollectionPropertyEditorViewModel
- Underscore-prefixed custom attributes
- CountingByteEnumerable
- migration-gltf-canonical-creation.md
- 0004-canonically-regenerate-mesh-assets-from-gltf.md
- ParsedGlb
- glTF API
- IUndoRedoService
- .Create
- .Create
- IDialogService
- CommonCommand
- .ToByteArray
- CommonMeshBaseHeader

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 115 edges
2. `GltfInterchange` - 114 edges
3. `GlbDocument` - 96 edges
4. `EarthTool.PAR.Enums` - 90 edges
5. `OperationDiagnostic` - 88 edges
6. `OperationResult` - 68 edges
7. `EarthTool.PAR.Models` - 64 edges
8. `StaticMeshAsset` - 61 edges
9. `DynamicGltfDocument` - 58 edges
10. `EarthTool.Common.Interfaces` - 57 edges

## Surprising Connections (you probably didn't know these)
- `Memory-Mapped Archive Data Source` --semantically_similar_to--> `WD Reading Algorithm`  [INFERRED] [semantically similar]
  docs/architecture.md → docs/WD_FORMAT.md
- `CommitFailingReportFileSystem` --implements--> `ICliReportFileSystem`  [EXTRACTED]
  EarthTool.CLI.Tests/InternalMshCommandHostTests.cs → EarthTool.CLI/Commands/MSH/CliReportFileSystem.cs
- `GltfCommandExecutor` --references--> `GltfInterchange`  [EXTRACTED]
  EarthTool.CLI/Commands/MSH/GltfCommandExecutor.cs → EarthTool.GLTF/GltfInterchange.cs
- `GltfCommandExecutor` --references--> `GltfCliReportSerializer`  [EXTRACTED]
  EarthTool.CLI/Commands/MSH/GltfCommandExecutor.cs → EarthTool.GLTF/GltfPlanAndReport.cs
- `GltfCommandExecutor` --references--> `GltfImportPlanSerializer`  [EXTRACTED]
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

## Communities (194 total, 9 thin omitted)

### Community 0 - "EntityGroupNodeViewModel"
Cohesion: 0.07
Nodes (21): EntityClassType, Faction, ResearchType, ObservableCollection, EntityGroupNodeViewModel, ObservableCollection, EntityGroupsRootNodeViewModel, ObservableCollection (+13 more)

### Community 1 - "MshOperationProfile"
Cohesion: 0.19
Nodes (10): MshOperationProfile, CancellationToken, Exception, IEnumerable, ILogger, Stream, Task, MshReader (+2 more)

### Community 2 - "AssetResult"
Cohesion: 0.28
Nodes (6): DiagnosticKey, GltfExportOptions, CliDiagnostic, AssetResult, OperationCounts, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, EventId, Exception, Fact, Func (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.11
Nodes (15): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+7 more)

### Community 5 - "DynamicEffectBehavior"
Cohesion: 0.17
Nodes (9): IReadOnlyDictionary, IReadOnlyList, DynamicAuthoringDefaults, DynamicAuthoringRequirement, DynamicBehaviorField, DynamicBehaviorFinding, DynamicEffectBehavior, DynamicEffectDescriptor (+1 more)

### Community 6 - ".Assemble"
Cohesion: 0.12
Nodes (14): HashSet, IReadOnlyList, List, Vector2, Vector3, AuthoringValidation, CanonicalStaticVertex, MshBuildResult (+6 more)

### Community 7 - "GltfImportPlanSerializer"
Cohesion: 0.35
Nodes (4): JsonElement, GltfImportPlanSerializer, ImportPlanException, JsonValueKind

### Community 8 - ".ResolveAndLoad"
Cohesion: 0.11
Nodes (18): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+10 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.08
Nodes (14): IEnumerable, Task, IParFileService, bool, ILogger, ObservableCollection, ReactiveCommand, string (+6 more)

### Community 11 - ".ExportGltfFileAsync"
Cohesion: 0.13
Nodes (8): GltfOperationProfile, CancellationToken, Stream, Task, Stream, ITransactionalFileSystem, TransactionalFileSystem, SeparateGltfPackage

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.12
Nodes (11): DynamicMeshAsset, Fact, IEnumerable, InlineData, JsonDocument, JsonElement, Task, Theory (+3 more)

### Community 13 - "CanonicalStaticGltfCreationTests"
Cohesion: 0.11
Nodes (19): Action, Fact, Guid, IEnumerable, InlineData, IReadOnlyList, JsonNode, JsonObject (+11 more)

### Community 14 - "EarthTool.MSH.Assets"
Cohesion: 0.08
Nodes (31): EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.CLI.Commands.MSH, EarthTool.MSH, EarthTool.Consumer.Tests (+23 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "CanonicalDynamicGltfImporter"
Cohesion: 0.11
Nodes (19): CanonicalDynamicGraph, CanonicalDynamicNode, CanonicalDynamicPreview, CancellationToken, GltfNewModelImportOptions, GltfOperationProfile, Guid, ICollection (+11 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "GltfImportPlan"
Cohesion: 0.16
Nodes (10): CancellationToken, IReadOnlyDictionary, Stream, Task, GltfImportPlan, ImportPlanException, DynamicPreviewException, UnsupportedGltfDomainException (+2 more)

### Community 19 - "GltfPlanAndReportTests"
Cohesion: 0.17
Nodes (10): BufferPath, Directory, Fact, Guid, InlineData, JsonNode, Task, Theory (+2 more)

### Community 20 - "release-qualification.mjs"
Cohesion: 0.07
Nodes (61): corpusBinaryStages, corpusInterchangeStages, recognizedDynamicEffectTypes, assertPrivacySafe(), buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames() (+53 more)

### Community 21 - "DynamicEffectType"
Cohesion: 0.24
Nodes (12): AuthoredEffect, DynamicEffectType, IEnumerable, Vector3, CanonicalDynamicAlpha, CanonicalDynamicEffectShape, CanonicalDynamicFrameSequence, CanonicalDynamicRecipe (+4 more)

### Community 22 - ".Create"
Cohesion: 0.24
Nodes (10): Action, Fact, Guid, IEnumerable, InlineData, JsonObject, Task, Theory (+2 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.12
Nodes (13): Bitmap, INotificationService, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap (+5 more)

### Community 24 - "CancellingAfterSidecarTransactionalFileSystem"
Cohesion: 0.07
Nodes (7): CancellationTokenSource, Stream, CancellingAfterSidecarTransactionalFileSystem, CorruptingSidecarTransactionalFileSystem, FailingManifestTransactionalFileSystem, FailingSidecarTransactionalFileSystem, FailingTransactionalFileSystem

### Community 25 - "Vehicle"
Cohesion: 0.07
Nodes (18): VehicleObjectType, Encoding, IEnumerable, Builder, Encoding, IEnumerable, Harvester, Encoding (+10 more)

### Community 26 - "Common MSH Base Header"
Cohesion: 0.10
Nodes (23): Model MSH Framing and Record Extensions Explicitly, Canonical Next Record Markers, MSH Footprint API, MSH Horizontal Extents API, IMeshBaseHeader, Legacy MSH Model Migration, MSH API, MSH Slots API (+15 more)

### Community 27 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 28 - ".CreateMockHeader"
Cohesion: 0.13
Nodes (14): ResourceType, Guid, Stream, IEarthInfoFactory, bool, DateTime, IReadOnlyCollection, MemoryMappedFile (+6 more)

### Community 29 - "ConvertCommand"
Cohesion: 0.27
Nodes (7): IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, IReader, Settings

### Community 30 - "EarthTool.TEX"
Cohesion: 0.14
Nodes (10): EarthTool.TEX, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, IServiceCollection, HostExtensions, IEnumerable, TexHeader, TexImage (+2 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Bytes, Asset, byte, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - "Task"
Cohesion: 0.16
Nodes (4): Fact, JsonDocument, Task, Action

### Community 34 - "Research"
Cohesion: 0.15
Nodes (10): IDictionary, IEnumerable, Encoding, IBinarySerializable, IEnumerable, Research, Encoding, IEnumerable (+2 more)

### Community 35 - "GlbDocument"
Cohesion: 0.11
Nodes (10): GltfOperationProfile, JsonDocument, JsonElement, Matrix4x4, ReadOnlySpan, Span, uint, GlbDocument (+2 more)

### Community 36 - "DynamicEffectBehaviorTests"
Cohesion: 0.23
Nodes (6): Action, Fact, DynamicEffectBehaviorTests, RepresentationTestCase, IReadOnlySet, RepresentationTestCase

### Community 37 - "GltfWalkingSkeletonTests"
Cohesion: 0.11
Nodes (7): Action, IReadOnlyList, JsonObject, List, Vector3, GltfWalkingSkeletonTests, JsonArray

### Community 38 - "OperationResult"
Cohesion: 0.13
Nodes (18): CancellationToken, FailingMshWriter, IReadOnlyList, OperationResult, CancellationToken, Stream, Task, WalkingSkeletonConsumer (+10 more)

### Community 39 - "CanonicalAuthoringMetadata"
Cohesion: 0.05
Nodes (38): Carrier, bool, GltfOperationProfile, GltfStaticObjectRoles, HashSet, IEnumerable, int, IReadOnlyDictionary (+30 more)

### Community 40 - "DynamicSemanticFailure"
Cohesion: 0.13
Nodes (11): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, Vector3, DynamicEffectEvaluation (+3 more)

### Community 41 - "MshCanonicalSerializer"
Cohesion: 0.16
Nodes (8): Encoding, Guid, int, IReadOnlyList, ReadOnlySpan, Vector3, MshCanonicalSerializer, ReadOnlyListCopyExtensions

### Community 42 - "GltfPlanAndReport.cs"
Cohesion: 0.17
Nodes (11): Guid, IEnumerable, int, IReadOnlyList, string, Utf8JsonWriter, GltfCliReport, GltfCliReportFormat (+3 more)

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - ".CreateEffectPreview"
Cohesion: 0.24
Nodes (4): Vector2, Vector3, DynamicAnimationTrack, DynamicEffectPreview

### Community 46 - "ProjectedAnimationFrame"
Cohesion: 0.20
Nodes (12): AnimationObjectLayout, IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionSet, ProjectedAnimationClip, ProjectedAnimationFrame (+4 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.13
Nodes (12): Action, bool, IEnumerable, ILogger, ObservableCollection, ReactiveCommand, string, Type (+4 more)

### Community 48 - "Vector3"
Cohesion: 0.09
Nodes (18): BinaryWriter, float, int, MemoryStream, Quaternion, string, Translation, Vector3 (+10 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (17): FileFlags, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo, Encoding (+9 more)

### Community 51 - "CanonicalMeshAuthoringTests"
Cohesion: 0.18
Nodes (5): Fact, Guid, IReadOnlyDictionary, Task, CanonicalMeshAuthoringTests

### Community 52 - ".WriteReportAsync"
Cohesion: 0.17
Nodes (4): Stream, CliReportFileSystem, ICliReportFileSystem, Exception

### Community 53 - "PublicCutoverAcceptanceTests"
Cohesion: 0.21
Nodes (7): CliResult, Fact, Task, CliResult, PublicCutoverAcceptanceTests, GeneratedRegex, Regex

### Community 54 - ".ToByteArray"
Cohesion: 0.06
Nodes (24): EarthTool.PAR.Tests.Factories, Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding (+16 more)

### Community 55 - "EarthTool.PAR.Models.Abstracts"
Cohesion: 0.05
Nodes (29): EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Factories, ArtifactType, PassiveMask, StandType, Encoding, IEnumerable, DestructibleEntity (+21 more)

### Community 56 - "Entity"
Cohesion: 0.08
Nodes (23): EarthTool.PAR.Models.Serialization, EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List, ValidationError, ValidationResult (+15 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "CanonicalBaseHeaderEncoder"
Cohesion: 0.11
Nodes (17): CornerPassageMaps, AnimationClassBytes, CanonicalStaticFootprint, byte, int, IReadOnlyDictionary, IReadOnlyList, ReadOnlySpan (+9 more)

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - "EarthTool.TEX.GUI/App.axaml.cs"
Cohesion: 0.13
Nodes (10): EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs (+2 more)

### Community 62 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (10): ITextFlagService, bool, ILogger, object, ObservableCollection, ReactiveCommand, string, Task (+2 more)

### Community 63 - "CanonicalGltfMetadataContractTests"
Cohesion: 0.24
Nodes (7): Action, Fact, IEnumerable, JsonNode, JsonObject, Task, CanonicalGltfMetadataContractTests

### Community 64 - "MshDecodeContext"
Cohesion: 0.12
Nodes (20): DecodedStaticRecord, Guid, MeshArchiveFraming, MeshAssetOrigin, CancellationToken, IReadOnlyDictionary, IReadOnlyList, List (+12 more)

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "EarthTool.WD.Models"
Cohesion: 0.07
Nodes (22): EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Models, EarthTool.WD.Interfaces, EarthTool.WD.Models, ReadOnlyMemory, IArchiveDataSource, bool, ReadOnlyMemory (+14 more)

### Community 68 - "EarthTool.Common"
Cohesion: 0.08
Nodes (17): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.Common, EarthTool.CLI, EarthTool.PAR.Tests.Services, EarthTool.PAR.Tests.Models (+9 more)

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "ITransactionalFileSystem"
Cohesion: 0.19
Nodes (3): Stream, ITransactionalFileSystem, TransactionalFileSystem

### Community 72 - "InteractableEntity"
Cohesion: 0.04
Nodes (30): ConnectorType, LookRoundTypeFlags, RepairerCapabilityFlags, ShadowType, Encoding, IEnumerable, InteractableEntity, Encoding (+22 more)

### Community 73 - "Task"
Cohesion: 0.06
Nodes (28): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+20 more)

### Community 74 - "StaticMeshAssetTests"
Cohesion: 0.26
Nodes (5): Fact, InlineData, Task, Theory, StaticMeshAssetTests

### Community 75 - "EditableEntity"
Cohesion: 0.14
Nodes (11): bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, bool, FlagValueViewModel (+3 more)

### Community 76 - "UndoRedoService"
Cohesion: 0.14
Nodes (9): Action, DateTime, UndoAction, Action, IEnumerable, ILogger, int, UndoRedoService (+1 more)

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.05
Nodes (23): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Services, EarthTool.WD.GUI, EarthTool.Common.Models, EarthTool.WD.Services (+15 more)

### Community 79 - "IArchive"
Cohesion: 0.12
Nodes (13): CancellationToken, CommandContext, CancellationToken, CommandContext, DateTime, IReadOnlyCollection, IArchive, DateTime (+5 more)

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - ".ExportGlbAsync"
Cohesion: 0.20
Nodes (3): JsonNode, IReadOnlyCollection, IReadOnlyDictionary

### Community 82 - "Program"
Cohesion: 0.50
Nodes (3): AppBuilder, STAThread, Program

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.15
Nodes (13): Animations, Blender 4.5 glTF round-trip research, Conclusion, Evidence model, Extras and custom properties, JSON value behavior, Meshes, primitives, and topology, Nodes, hierarchy, scenes, and transforms (+5 more)

### Community 84 - "StaticMeshSequenceFixture"
Cohesion: 0.21
Nodes (7): int, IReadOnlyList, Matrix4x4, Vector3, Record, StaticMeshSequenceFixture, Record

### Community 85 - ".RunAsync"
Cohesion: 0.38
Nodes (4): Fact, Task, OfficialCorpusQualificationTests, Trait

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "CanonicalStaticRenderObjectSequenceEncoder"
Cohesion: 0.19
Nodes (9): CanonicalStaticRecord, StaticAnimationReplacement, ICollection, int, IReadOnlyDictionary, IReadOnlyList, Matrix4x4, Vector3 (+1 more)

### Community 88 - "Task"
Cohesion: 0.31
Nodes (5): AssetResult, ChannelReader, ChannelWriter, Task, ProfileScope

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "DynamicGltfDocument"
Cohesion: 0.08
Nodes (26): DynamicAnimationLayout, DynamicAnimationTrack, DynamicEffectPreview, DynamicImageLayout, DynamicMeshLayout, DynamicObjectScope, BinaryWriter, float (+18 more)

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 95 - "TexPreviewLoader"
Cohesion: 0.06
Nodes (30): Func, IEnumerable, IReadOnlyList, SafeResourceLookup, SafeResourceMatch, BinaryReader, byte, CancellationToken (+22 more)

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

### Community 100 - "EarthTool.PAR.Enums"
Cohesion: 0.04
Nodes (33): EarthTool.PAR.Enums, BarrelBetaType, BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, DamageFlags, ExplosionFlags (+25 more)

### Community 101 - ".Write_And_Read_AreSymmetric"
Cohesion: 0.29
Nodes (5): Writer, Fact, ParameterWriterTests, Encoding, ParTestData

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

### Community 106 - ".NewModelImportRejectsAmbiguousOrOutOfRangeAnimationClasses"
Cohesion: 0.26
Nodes (3): InlineData, JsonElement, Theory

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - "IArchiveItem"
Cohesion: 0.21
Nodes (5): ReadOnlyMemory, IArchiveItem, HashSet, TextFlagService, IComparable

### Community 109 - "GltfInterchange"
Cohesion: 0.06
Nodes (35): IReadOnlyDictionary, OperationDiagnostic, GltfNewModelStaticLightOptions, Func, Guid, ICollection, IDictionary, IEnumerable (+27 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.13
Nodes (13): CancellationToken, IEnumerable, IReadOnlyList, Task, TextWriter, GltfCommandExecutor, OperationStatus, GltfCliReportOperation (+5 more)

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

### Community 117 - "DynamicEffectRecipeTests"
Cohesion: 0.42
Nodes (4): Fact, Guid, Task, DynamicEffectRecipeTests

### Community 118 - ".GenerateSampleData"
Cohesion: 0.13
Nodes (6): Fact, ArchiveItemTests, Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 119 - "ArchiverService"
Cohesion: 0.07
Nodes (26): Encoding, DateTime, Guid, IArchiveFactory, Stream, ICompressor, ReadOnlySpan, Stream (+18 more)

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "EarthTool.PAR.Extensions"
Cohesion: 0.09
Nodes (16): EarthTool.PAR.Extensions, ResourceVehicleType, VerticalVehicleAnimationType, ParameterEntry, Encoding, IEnumerable, VerticalTransporter, Encoding (+8 more)

### Community 122 - "ConvertCommand"
Cohesion: 0.18
Nodes (10): CommonCommand, CommonSettings, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+2 more)

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
Cohesion: 0.16
Nodes (10): CliProcessResult, CliReportOperation, IEnumerable, IReadOnlyList, string, Task, CliOracleResult, CliProcessResult (+2 more)

### Community 127 - "DynamicEffectExtension"
Cohesion: 0.50
Nodes (3): ReadOnlySpan, DynamicEffectExtension, DynamicLightType

### Community 128 - "MeshAssetAuthoring.cs"
Cohesion: 0.15
Nodes (11): NewModelSourceDraft, Guid, IEnumerable, CanonicalHorizontalExtents, CanonicalStaticObjectRole, CanonicalStaticRenderObject, CanonicalStaticSourceObject, CanonicalTriangle (+3 more)

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "Program"
Cohesion: 0.40
Nodes (3): AppBuilder, STAThread, Program

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - "CommandTypeRegistrar"
Cohesion: 0.11
Nodes (11): EarthTool.CLI.Commands, Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, Type, CommandTypeResolver (+3 more)

### Community 134 - "StaticHierarchy"
Cohesion: 0.33
Nodes (4): List, StaticHierarchy, StaticSourceBuilder, StaticSourceBuilder

### Community 135 - "ExportGltfSettings"
Cohesion: 0.27
Nodes (9): AsyncCommand, CancellationToken, CommandContext, Task, ExportGltfCommand, ImportGltfCommand, ExportGltfSettings, GltfCommandSettings (+1 more)

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

### Community 140 - "ParameterReader"
Cohesion: 0.17
Nodes (7): EarthTool.Common.Bases, Reader, FileType, BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 143 - "EarthTool.CLI.Commands.WD"
Cohesion: 0.06
Nodes (34): Command, CommandSettings, EarthTool.CLI.Commands.WD, CommonSettings, AddCommand, CreateCommand, CancellationToken, CommandContext (+26 more)

### Community 148 - "EarthTool.PAR.Models"
Cohesion: 0.12
Nodes (7): EarthTool.CLI.Commands.PAR, EarthTool.PAR.GUI, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, EarthTool.PAR.Models

### Community 149 - "Mesh Artist Quick Start And Cheat Sheet"
Cohesion: 0.15
Nodes (13): Attachment Identifier Cheat Sheet, Choose The Correct Workflow, Create The Canonical MSH, Directional Empty Presentation In Blender, Edit The Scene, Export An MSH, Export From Blender, Fast Checks Before Import (+5 more)

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 152 - "Migrate From COLLADA To glTF"
Cohesion: 0.33
Nodes (6): API migration, Attachment helper name migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 153 - "PropertyEditorViewModel"
Cohesion: 0.21
Nodes (9): Action, IEnumerable, IPropertyEditorFactory, bool, ReactiveCommand, string, Type, Unit (+1 more)

### Community 154 - "FlagsPropertyEditorViewModel"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 155 - "StaticMeshAsset"
Cohesion: 0.06
Nodes (34): AnimationLayout, IDictionary, IEnumerable, IReadOnlyDictionary, IReadOnlyList, Utf8JsonWriter, GltfPackage, ParsedGltfMesh (+26 more)

### Community 158 - "PropertyEditorFactory"
Cohesion: 0.27
Nodes (7): Action, HashSet, IEnumerable, ILogger, Type, PropertyEditorFactory, PropertyInfo

### Community 159 - "OfficialCorpusQualification"
Cohesion: 0.29
Nodes (8): BinaryWriter, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification, OperationCounts, ValidatorCode

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - "TreeItemViewModel"
Cohesion: 0.12
Nodes (11): DateTime, int, long, string, ArchiveInfoViewModel, HashSet, bool, Guid (+3 more)

### Community 164 - "Decision consequences for later tickets"
Cohesion: 0.40
Nodes (5): Decision consequences for later tickets, EarthTool metadata requirements, Native glTF candidates, Required fingerprints and invalidation, What stock Blender cannot promise

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.25
Nodes (6): Before/After Protocol, Historical Measured Result, Official MSH Qualification Performance, Stage Profiling, Local pre-publish qualification, Official MSH corpus

### Community 166 - "Runner"
Cohesion: 0.08
Nodes (21): ContentFingerprint, DynamicCoverage, CliBatchOracleResult, Dictionary, IDictionary, IEnumerable, int, IReadOnlyDictionary (+13 more)

### Community 167 - "Tested build and fixture"
Cohesion: 0.67
Nodes (3): Diagnostic asset, Stock options, Tested build and fixture

### Community 168 - "TexFile"
Cohesion: 0.24
Nodes (8): BinaryReader, IEnumerable, TexFile, TexHeader, BinaryReader, IEnumerable, SKBitmap, TexImage

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 171 - "ItemCommand"
Cohesion: 0.31
Nodes (5): CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings

### Community 173 - "WorkerContext"
Cohesion: 0.17
Nodes (10): List, KhronosValidatorServer, ValidatorResult, WorkerContext, IAsyncDisposable, KhronosValidatorServer, Process, ValidatorCode (+2 more)

### Community 175 - "ViewLocator"
Cohesion: 0.10
Nodes (11): EarthTool.TEX.GUI, Control, ViewLocator, AppBuilder, STAThread, Program, Control, ViewLocator (+3 more)

### Community 176 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.23
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 178 - "GltfSourceLossDiagnosticsTests"
Cohesion: 0.24
Nodes (11): Code, DiagnosticSeverity, EventId, Fact, IEnumerable, IReadOnlyList, Path, string (+3 more)

### Community 179 - ".GetMinimumSerializedLength"
Cohesion: 0.25
Nodes (6): IEnumerable, TriangleCount, VertexCount, IEnumerable, TriangleCount, VertexCount

### Community 181 - "EnumPropertyEditorViewModel"
Cohesion: 0.29
Nodes (5): object, ObservableCollection, Type, EnumPropertyEditorViewModel, EnumValueViewModel

### Community 183 - "Metadata Envelope Reference"
Cohesion: 0.33
Nodes (6): Dynamic Effect — `ET_Dynamic_{n}_{Effect}` node, Metadata Envelope Reference, Static Light — `ET_SpotLight_{n}` or `ET_OmniLight_{n}` light, Static Source Object — `ET_Static_{n}` mesh node, Textured Material, Turret / Cannon — `ET_Turret_{n}` empty

### Community 184 - "ManifestFailingFileSystem"
Cohesion: 0.29
Nodes (3): int, Stream, ManifestFailingFileSystem

### Community 185 - "ParFile"
Cohesion: 0.22
Nodes (8): ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile, Encoding, ParameterWriter

### Community 188 - "IntCollectionPropertyEditorViewModel"
Cohesion: 0.60
Nodes (3): IEnumerable, string, IntCollectionPropertyEditorViewModel

### Community 190 - "Underscore-prefixed custom attributes"
Cohesion: 0.67
Nodes (3): Identity, order, collision, and merge behavior, Supported import shapes, Underscore-prefixed custom attributes

### Community 191 - "CountingByteEnumerable"
Cohesion: 0.40
Nodes (4): int, CountingByteEnumerable, IEnumerable, IEnumerator

### Community 192 - "migration-gltf-canonical-creation.md"
Cohesion: 0.33
Nodes (5): CLI reports, Creation results, Export results, Package authoring, Plans and limits

### Community 195 - "ParsedGlb"
Cohesion: 0.09
Nodes (21): IReadOnlyDictionary, IReadOnlyList, string, GltfDiagnosticCodes, GltfLightHandle, GltfMaterialHandle, GltfMeshResourceLimits, GltfNewModelFootprint (+13 more)

### Community 196 - "glTF API"
Cohesion: 0.18
Nodes (11): Canonical authoring envelopes, Canonical creation, Dynamic effect-preview contract, Dynamic `ScalableObject` mesh key, Embedded and explicit TEX and MSH bindings, Export packages, glTF API, Import plans and reports (+3 more)

### Community 197 - "IUndoRedoService"
Cohesion: 0.11
Nodes (11): Action, IEnumerable, IUndoRedoService, int, string, IntPropertyEditorViewModel, bool, int (+3 more)

### Community 198 - ".Create"
Cohesion: 0.28
Nodes (6): AnimationLengths, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 201 - ".Create"
Cohesion: 0.21
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

### Community 206 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 210 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 216 - "CommonMeshBaseHeader"
Cohesion: 0.21
Nodes (7): byte, int, IReadOnlyList, ReadOnlySpan, CommonMeshBaseHeader, int, DynamicMeshDecoder

## Knowledge Gaps
- **343 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+338 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **9 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `EarthTool.WD.Models`, `CommandTypeRegistrar`, `IValueConverter`, `ParameterReader`, `EarthTool.MSH.Assets`, `EarthTool.CLI.Commands.WD`, `IEarthInfo`, `EarthTool.PAR.Models`, `ConvertCommand`, `.CreateMockHeader`, `EarthTool.TEX`?**
  _High betweenness centrality (0.195) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `MeshAssetAuthoring.cs`, `ParsedGlb`, `OperationResult`, `CanonicalAuthoringMetadata`, `DynamicSemanticFailure`, `GltfPlanAndReport.cs`, `ProjectedAnimationFrame`, `DynamicEffectType`, `CanonicalBaseHeaderEncoder`, `StaticMeshAsset`?**
  _High betweenness centrality (0.122) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `Task` to `EarthTool.WD.Models`, `DynamicEffectType`?**
  _High betweenness centrality (0.104) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _343 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `EntityGroupNodeViewModel` be split into smaller, more focused modules?**
  _Cohesion score 0.07056451612903226 - nodes in this community are weakly interconnected._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10741971207087486 - nodes in this community are weakly interconnected._