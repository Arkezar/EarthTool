# Graph Report - EarthTool  (2026-08-06)

## Corpus Check
- 374 files · ~310,469 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4870 nodes · 14941 edges · 198 communities (186 shown, 12 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 1148 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `18a66bc8`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- MeshAsset
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- IArchiveItem
- MshOperationProfile
- .ToByteArray
- .Load
- IValueConverter
- MainWindowViewModel
- OperationResult
- DynamicGltfInterchangeTests
- Vector3
- EarthTool.MSH.Assets
- .OpenArchive
- GltfInterchange
- Dynamic MESH Binary Layout
- GlbDocument.cs
- GlbDocument
- release-qualification.mjs
- DynamicEffectBehavior
- EarthTool.PAR.Enums
- MainWindowViewModel
- ITransactionalFileSystem
- Vehicle
- Common MSH Base Header
- MetadataGraphValidationTests
- ArchiveTests
- EarthTool.TEX.GUI/App.axaml.cs
- MetadataEnvelope
- EarthTool.CLI
- DynamicMeshAssetTests
- .Create
- OperationDiagnostic
- GltfOperationProfile
- DynamicEffectBehaviorTests
- StaticMeshAssembler
- InlineData
- DynamicGltfDocument
- IArchive
- UndoRedoService
- IDialogService
- Static Mesh Header
- StaticObject Record
- PropertyEditorFactory
- StaticAnimationProjection
- EntityDetailsViewModel
- ParFile
- PublicApiApproval
- IEarthInfo
- .Create
- Runner
- OfficialCorpusQualification
- IReadOnlyList
- TexPreviewLoader
- Entity
- EarthTool.MSH.Tests
- AnimationClassBytes
- DialogService
- EarthTool.PAR
- ArchiverServiceTests
- ArchiveInfoViewModel
- IArchiver
- MshDecodeContext
- StaticMeshSequenceFixture
- EarthTool.PAR.GUI
- MetadataConflictCollector
- .WriteReportAsync
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- .Commit
- InteractableEntity
- Task
- EquipableEntity
- GltfImportPlanSerializer
- PublicCutoverAcceptanceTests
- EarthTool.sln
- EarthTool.Common.Interfaces
- WdSettings.cs
- BinaryExtensions
- .ImportEditGlbAsync
- StaticMeshAsset
- Blender 4.5 glTF round-trip research
- ConvertCommand
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- .Write_And_Read_AreSymmetric
- EnumPropertyEditorViewModel
- 0003-create-immutable-msh-assets-from-gltf.md
- EarthTool WD Archive Manager
- ITransactionalFileSystem
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- glTF API
- .RoundTripAsync
- EarthTool.Common.GUI.Enums
- MappedArchiveDataSource
- EarthTool Suite
- WD Central Directory
- InterchangeBaseline
- ExportGltfSettings
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- .LoadPreview
- Reader
- .Decode
- EarthTool.PAR.GUI.ViewModels
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- Research
- TexFile
- NotificationService
- .GenerateSampleData
- EarthTool.TEX
- Task
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- Modify An Existing Mesh
- FileType
- Base Header
- MainWindowViewModel
- UnitTest1.cs
- EarthTool Installation Guide
- CommandTypeRegistrar
- GltfMeshCreationFallbackTests
- TreeItemViewModel
- Dependabot Dependency Automation
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- Setup .NET Environment
- Mesh Attachments 1..49
- DynamicEffectExtension
- Code Quality Analysis Job
- Dynamic Color
- ItemCommand
- ConvertCommand
- Mesh Artist Quick Start And Cheat Sheet
- package.json
- EarthTool.CLI.Commands.MSH
- Migrate From COLLADA To glTF
- FlagsPropertyEditorViewModel
- App
- CommonCommand
- MshCanonicalSerializer
- .OpenArchive
- validate-glb.mjs
- ViewLocator
- Decision consequences for later tickets
- Official MSH Qualification Performance
- .Resolve
- Tested build and fixture
- Extras and custom properties
- EarthTool.CLI.Tests
- ExtractCommand
- .ResolveAndLoad
- Underscore-prefixed custom attributes
- KhronosValidatorServer
- EarthTool.WD.GUI
- .Match
- OneTriangleMshFixture
- GltfInterchange.cs
- ResolutionBudget
- GltfWalkingSkeletonTests
- IReadOnlyList
- DynamicFrameSelection
- DestructibleEntity
- QualificationProfiler
- ParameterReader
- IDisposable
- GltfPlanAndReport.cs
- ParameterReaderTests
- ListCommand
- .Create
- IExtractor
- .ReplacePivot
- .ToByteArray
- .ApplyNewModelAnimations
- EarthTool.GLTF/HostExtensions.cs
- .GetMarkerAttachmentFlag
- .Execute
- OfficialCorpusCliOracle.cs

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 262 edges
2. `GltfInterchange` - 214 edges
3. `GlbDocument` - 152 edges
4. `DynamicGltfDocument` - 123 edges
5. `DynamicGltfInterchangeTests` - 97 edges
6. `OperationDiagnostic` - 93 edges
7. `EarthTool.PAR.Enums` - 90 edges
8. `OperationResult` - 87 edges
9. `StaticMeshAsset` - 82 edges
10. `MetadataGraphValidationTests` - 80 edges

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

## Communities (198 total, 12 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - "MeshAsset"
Cohesion: 0.21
Nodes (12): byte, MeshAsset, CancellationToken, Exception, IEnumerable, ILogger, Stream, Task (+4 more)

### Community 2 - "AssetResult"
Cohesion: 0.22
Nodes (7): AssetResult, DiagnosticKey, AssetResult, KhronosValidatorServer, OperationCounts, ProfileScope, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.10
Nodes (16): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+8 more)

### Community 5 - "IArchiveItem"
Cohesion: 0.14
Nodes (6): ReadOnlyMemory, IArchiveItem, ITextFlagService, HashSet, TextFlagService, IComparable

### Community 6 - "MshOperationProfile"
Cohesion: 0.17
Nodes (5): AuthoringValidation, MshBuildResult, IEnumerable, MshExpert, MshOperationProfile

### Community 7 - ".ToByteArray"
Cohesion: 0.07
Nodes (23): Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding, IEnumerable (+15 more)

### Community 8 - ".Load"
Cohesion: 0.20
Nodes (10): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IReadOnlyDictionary, IReadOnlyList, Vector3, MshPreviewLoader (+2 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (8): bool, ILogger, ObservableCollection, ReactiveCommand, string, Task, Unit, MainWindowViewModel

### Community 11 - "OperationResult"
Cohesion: 0.13
Nodes (11): IReadOnlyList, OperationResult, GltfMeshCreationResult, GltfOperationProfile, CancellationToken, SeparateGltfPackage, Stream, Task (+3 more)

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.06
Nodes (34): DynamicAlphaTiming, DynamicEffectType, DynamicLightType, DynamicMeshAsset, StaticAnimationClass, IEnumerable, Vector3, CanonicalDynamicAlpha (+26 more)

### Community 13 - "Vector3"
Cohesion: 0.10
Nodes (12): Action, BinaryWriter, float, Matrix4x4, Quaternion, Translation, Vector3, AttachmentHeadingProjection (+4 more)

### Community 14 - "EarthTool.MSH.Assets"
Cohesion: 0.16
Nodes (14): EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.Consumer.Tests, EarthTool.MSH.Tests, EarthTool.GLTF.Internal (+6 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.15
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "GltfInterchange"
Cohesion: 0.05
Nodes (25): AnimationEditPlan, AnimationReplacement, Func, ICollection, IEnumerable, IReadOnlyList, JsonNode, JsonObject (+17 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "GlbDocument.cs"
Cohesion: 0.11
Nodes (19): ImportPlanException, DynamicMetadataIdentityException, int, string, MalformedMetadataException, MetadataAnimationClass, MetadataAnimationProjection, MetadataPartition (+11 more)

### Community 19 - "GlbDocument"
Cohesion: 0.07
Nodes (16): CarrierKind, GltfOperationProfile, Guid, ICollection, IDictionary, JsonDocument, JsonElement, Path (+8 more)

### Community 20 - "release-qualification.mjs"
Cohesion: 0.07
Nodes (62): corpusBinaryStages, corpusInterchangeStages, recognizedDynamicEffectTypes, assertPrivacySafe(), buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames() (+54 more)

### Community 21 - "DynamicEffectBehavior"
Cohesion: 0.11
Nodes (15): DynamicSemanticFailure, EffectRectangle, IReadOnlyDictionary, IReadOnlyList, Vector3, DynamicAuthoringDefaults, DynamicAuthoringRequirement, DynamicBehaviorField (+7 more)

### Community 22 - "EarthTool.PAR.Enums"
Cohesion: 0.08
Nodes (11): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.Extensions, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.PAR.Tests.Services, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Tests.Factories (+3 more)

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
Cohesion: 0.12
Nodes (13): Baseline, Action, Bytes, Fact, Func, Guid, ICollection, InlineData (+5 more)

### Community 28 - "ArchiveTests"
Cohesion: 0.14
Nodes (9): bool, DateTime, Encoding, IReadOnlyCollection, MemoryMappedFile, Archive, Fact, ArchiveTests (+1 more)

### Community 29 - "EarthTool.TEX.GUI/App.axaml.cs"
Cohesion: 0.13
Nodes (10): EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs (+2 more)

### Community 30 - "MetadataEnvelope"
Cohesion: 0.08
Nodes (25): GltfNewModelStaticLightOptions, Action, BinaryWriter, IDictionary, IReadOnlyCollection, IReadOnlyDictionary, ISet, List (+17 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Asset, byte, Bytes, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - ".Create"
Cohesion: 0.12
Nodes (10): JsonNode, int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture (+2 more)

### Community 34 - "OperationDiagnostic"
Cohesion: 0.08
Nodes (10): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, GltfLightHandle, GltfMaterialHandle, GltfNewModelImportOptions, GltfNodeHandle, ParseScopeResolution (+2 more)

### Community 35 - "GltfOperationProfile"
Cohesion: 0.10
Nodes (14): DynamicSceneLayout, CancellationToken, GltfOperationProfile, ICollection, InterchangeBaseline, ISet, JsonDocument, JsonElement (+6 more)

### Community 36 - "DynamicEffectBehaviorTests"
Cohesion: 0.23
Nodes (6): Action, Fact, DynamicEffectBehaviorTests, RepresentationTestCase, IReadOnlySet, RepresentationTestCase

### Community 37 - "StaticMeshAssembler"
Cohesion: 0.07
Nodes (25): StaticRenderObjectFlagMasks, StaticRenderObjectFlags, bool, Dictionary, Guid, HashSet, IEnumerable, IReadOnlyDictionary (+17 more)

### Community 38 - "InlineData"
Cohesion: 0.15
Nodes (5): BlenderOutputEvidence, IEnumerable, InlineData, Theory, Trait

### Community 39 - "DynamicGltfDocument"
Cohesion: 0.08
Nodes (24): DynamicEditedPreview, DynamicRecordSlice, float, int, PreservationChange, ReadOnlySpan, Stream, string (+16 more)

### Community 40 - "IArchive"
Cohesion: 0.06
Nodes (31): DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid, IArchiveFactory, Stream (+23 more)

### Community 41 - "UndoRedoService"
Cohesion: 0.11
Nodes (10): Action, DateTime, UndoAction, IEnumerable, Action, IEnumerable, ILogger, int (+2 more)

### Community 42 - "IDialogService"
Cohesion: 0.25
Nodes (3): IEnumerable, Task, IDialogService

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - "PropertyEditorFactory"
Cohesion: 0.24
Nodes (7): Action, HashSet, IEnumerable, ILogger, Type, PropertyEditorFactory, PropertyInfo

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.14
Nodes (15): AnimationObjectLayout, BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionFingerprint (+7 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.05
Nodes (38): bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, Action, IEnumerable (+30 more)

### Community 48 - "ParFile"
Cohesion: 0.15
Nodes (10): Task, IParFileService, ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile (+2 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (21): FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo (+13 more)

### Community 51 - ".Create"
Cohesion: 0.18
Nodes (8): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, IEnumerable, IEnumerator

### Community 52 - "Runner"
Cohesion: 0.16
Nodes (8): ChannelReader, ChannelWriter, DynamicCoverage, CliOracleResult, Guid, Task, Runner, ValidatorAggregate

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.10
Nodes (17): ContentFingerprint, BinaryWriter, Dictionary, IDictionary, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+9 more)

### Community 54 - "IReadOnlyList"
Cohesion: 0.09
Nodes (18): AnimationLayout, PartitionMatch, IReadOnlyList, MemoryStream, NativeProjectionFingerprint, ByteArrayComparer, GeometryPartition, ParsedGltfMesh (+10 more)

### Community 55 - "TexPreviewLoader"
Cohesion: 0.13
Nodes (14): byte, CancellationToken, Exception, GltfExportOptions, GltfOperationProfile, ICollection, IReadOnlyDictionary, IReadOnlyList (+6 more)

### Community 56 - "Entity"
Cohesion: 0.06
Nodes (29): EarthTool.PAR.Models.Serialization, Encoding, IBinarySerializable, EntityClassType, EntityGroupType, BinaryReader, IEnumerable, EntityFactory (+21 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "AnimationClassBytes"
Cohesion: 0.18
Nodes (7): byte, int, IReadOnlyList, ReadOnlySpan, Span, CommonMeshBaseHeader, AnimationClassBytes

### Community 59 - "DialogService"
Cohesion: 0.18
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - "ArchiverServiceTests"
Cohesion: 0.21
Nodes (5): DateTime, Guid, Fact, string, ArchiverServiceTests

### Community 62 - "ArchiveInfoViewModel"
Cohesion: 0.18
Nodes (7): DateTime, int, long, string, ArchiveInfoViewModel, ArchiveItemViewModel, ViewModelBase

### Community 63 - "IArchiver"
Cohesion: 0.16
Nodes (8): Command, EarthTool.CLI.Commands.WD, AddCommand, CreateCommand, InfoCommand, RemoveCommand, WdCommandBase, IArchiver

### Community 64 - "MshDecodeContext"
Cohesion: 0.10
Nodes (28): DecodedStaticRecord, Guid, MeshArchiveFraming, MeshAssetOrigin, int, DynamicMeshDecoder, CancellationToken, IReadOnlyDictionary (+20 more)

### Community 65 - "StaticMeshSequenceFixture"
Cohesion: 0.20
Nodes (7): int, IReadOnlyList, Matrix4x4, Vector3, Record, StaticMeshSequenceFixture, Record

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "MetadataConflictCollector"
Cohesion: 0.15
Nodes (19): MetadataConflictException, bool, GltfOperationProfile, IEnumerable, int, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList (+11 more)

### Community 68 - ".WriteReportAsync"
Cohesion: 0.17
Nodes (4): Stream, CliReportFileSystem, ICliReportFileSystem, Exception

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - ".Commit"
Cohesion: 0.19
Nodes (5): Fact, InlineData, Task, Theory, StaticMeshAssetTests

### Community 72 - "InteractableEntity"
Cohesion: 0.04
Nodes (38): BarrelBetaType, ConnectorType, LookRoundTypeFlags, RepairerCapabilityFlags, ShadowType, TargetType, WeaponFireType, Encoding (+30 more)

### Community 73 - "Task"
Cohesion: 0.06
Nodes (30): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+22 more)

### Community 74 - "EquipableEntity"
Cohesion: 0.05
Nodes (32): BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags, ResourceVehicleType (+24 more)

### Community 75 - "GltfImportPlanSerializer"
Cohesion: 0.06
Nodes (27): BufferPath, ConflictKey, Directory, CancellationToken, Guid, IEnumerable, JsonElement, SeparateGltfPackage (+19 more)

### Community 76 - "PublicCutoverAcceptanceTests"
Cohesion: 0.21
Nodes (7): CliResult, Fact, Task, CliResult, PublicCutoverAcceptanceTests, GeneratedRegex, Regex

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.04
Nodes (34): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Factories, EarthTool.TEX, EarthTool.PAR, EarthTool.WD.Tests.Services (+26 more)

### Community 79 - "WdSettings.cs"
Cohesion: 0.19
Nodes (12): CommandSettings, CommonSettings, CancellationToken, CommandContext, DebugCommand, AddSettings, CreateSettings, InfoSettings (+4 more)

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - ".ImportEditGlbAsync"
Cohesion: 0.08
Nodes (9): AnimationLengths, JsonDocument, JsonElement, Vector3, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths (+1 more)

### Community 82 - "StaticMeshAsset"
Cohesion: 0.11
Nodes (22): Discarded, GltfArtistObjectLocalIds, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, Utf8JsonWriter, GltfPackage, IEnumerable (+14 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.20
Nodes (10): Animations, Blender 4.5 glTF round-trip research, Conclusion, Evidence model, Meshes, primitives, and topology, Nodes, hierarchy, scenes, and transforms, Primary sources, Punctual lights (+2 more)

### Community 84 - "ConvertCommand"
Cohesion: 0.27
Nodes (7): IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, IReader, Settings

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - ".Write_And_Read_AreSymmetric"
Cohesion: 0.29
Nodes (5): Writer, Fact, ParameterWriterTests, Encoding, ParTestData

### Community 88 - "EnumPropertyEditorViewModel"
Cohesion: 0.29
Nodes (5): object, ObservableCollection, Type, EnumPropertyEditorViewModel, EnumValueViewModel

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "ITransactionalFileSystem"
Cohesion: 0.19
Nodes (3): Stream, ITransactionalFileSystem, TransactionalFileSystem

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "glTF API"
Cohesion: 0.40
Nodes (5): Dynamic effect-preview contract, glTF API, Reports and compatibility, Static authoring authority and inference matrix, Static-light authoring contract

### Community 95 - ".RoundTripAsync"
Cohesion: 0.23
Nodes (9): CancellationToken, Stream, Task, CancellationToken, Stream, Task, IMshReader, IMshValidator (+1 more)

### Community 96 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.09
Nodes (17): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, EarthTool.Common.GUI.Views, IServiceCollection, ServiceCollectionExtensions (+9 more)

### Community 97 - "MappedArchiveDataSource"
Cohesion: 0.09
Nodes (15): EarthTool.WD.Interfaces, ReadOnlyMemory, IArchiveDataSource, ReadOnlyMemory, InMemoryArchiveDataSource, int, MemoryMappedFile, ReadOnlyMemory (+7 more)

### Community 98 - "EarthTool Suite"
Cohesion: 0.20
Nodes (11): EarthTool.DAE, EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management (+3 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "InterchangeBaseline"
Cohesion: 0.07
Nodes (34): GltfStaticIdentityMap, Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfArtistObjectLocalIds, GltfDiagnosticCodes, GltfDynamicEditImportResult (+26 more)

### Community 101 - "ExportGltfSettings"
Cohesion: 0.27
Nodes (9): AsyncCommand, CancellationToken, CommandContext, Task, ExportGltfCommand, ImportGltfCommand, ExportGltfSettings, GltfCommandSettings (+1 more)

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

### Community 106 - ".LoadPreview"
Cohesion: 0.33
Nodes (4): PreviewResolution, PreviewResolution, PreviewResolutionKind, TexResolutionBudget

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - ".Decode"
Cohesion: 0.18
Nodes (7): BinaryReader, IEnumerable, int, long, TexResolutionBudget, HasVariants, Preview

### Community 109 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.04
Nodes (44): EarthTool.PAR.GUI, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, Faction, ResearchType, AppBuilder (+36 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.15
Nodes (12): CancellationToken, Func, IEnumerable, IReadOnlyList, Task, TextWriter, GltfCommandExecutor, OperationStatus (+4 more)

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

### Community 116 - "Research"
Cohesion: 0.31
Nodes (6): IDictionary, IEnumerable, ParameterEntry, IEnumerable, Research, TreeNode

### Community 117 - "TexFile"
Cohesion: 0.24
Nodes (8): BinaryReader, IEnumerable, TexFile, TexHeader, BinaryReader, IEnumerable, SKBitmap, TexImage

### Community 118 - "NotificationService"
Cohesion: 0.19
Nodes (7): NotificationType, Exception, NotificationEventArgs, Exception, ILogger, NotificationService, EventArgs

### Community 119 - ".GenerateSampleData"
Cohesion: 0.14
Nodes (7): Fact, ArchiveItemTests, Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests, Guid

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 122 - "Task"
Cohesion: 0.09
Nodes (3): Fact, Task, Action

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
Cohesion: 0.18
Nodes (10): CliProcessResult, CliReportOperation, IReadOnlyList, JsonElement, string, Task, CliBatchOracleResult, CliProcessResult (+2 more)

### Community 127 - "Modify An Existing Mesh"
Cohesion: 0.29
Nodes (7): 1. Extract and export, 2. Import into Blender, 3. Edit or add geometry, 4. Preview all animation classes, 5. Export from Blender, 6. Import the edit and install it, Modify An Existing Mesh

### Community 128 - "FileType"
Cohesion: 0.22
Nodes (7): Reader, FileType, IEnumerable, TexHeader, TexImage, ITexFile, TexReader

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "MainWindowViewModel"
Cohesion: 0.13
Nodes (10): INotificationService, bool, ILogger, object, ObservableCollection, ReactiveCommand, string, Task (+2 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - "CommandTypeRegistrar"
Cohesion: 0.22
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 134 - "GltfMeshCreationFallbackTests"
Cohesion: 0.25
Nodes (9): Action, Fact, IEnumerable, InlineData, JsonNode, JsonObject, Task, Theory (+1 more)

### Community 135 - "TreeItemViewModel"
Cohesion: 0.24
Nodes (5): HashSet, bool, Guid, ObservableCollection, TreeItemViewModel

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
Cohesion: 0.15
Nodes (9): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, ReadOnlySpan, Vector3, DynamicEffectExtension, Fact, Guid (+1 more)

### Community 143 - "ItemCommand"
Cohesion: 0.27
Nodes (6): EarthTool.CLI.Commands.PAR, CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings

### Community 148 - "ConvertCommand"
Cohesion: 0.20
Nodes (10): CommonCommand, CommonSettings, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+2 more)

### Community 149 - "Mesh Artist Quick Start And Cheat Sheet"
Cohesion: 0.29
Nodes (7): Attachment Identifier Cheat Sheet, Choose The Correct Workflow, Create A Standalone MSH, Directional Empty Presentation In Blender, Fast Checks Before Import, Mesh Artist Quick Start And Cheat Sheet, Read the import report

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "EarthTool.CLI.Commands.MSH"
Cohesion: 0.24
Nodes (5): EarthTool.CLI.Commands.MSH, EarthTool.MSH, EarthTool.CLI.Tests, IServiceCollection, HostExtensions

### Community 152 - "Migrate From COLLADA To glTF"
Cohesion: 0.33
Nodes (6): API migration, Attachment helper name migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 154 - "FlagsPropertyEditorViewModel"
Cohesion: 0.23
Nodes (6): bool, object, ObservableCollection, Type, FlagsPropertyEditorViewModel, FlagValueViewModel

### Community 155 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 156 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 158 - "MshCanonicalSerializer"
Cohesion: 0.09
Nodes (25): CanonicalStaticRecord, Matrix4x4, StaticAnimationTracks, CanonicalStaticVertex, CanonicalTriangle, StaticAnimationReplacement, StaticRenderObjectAddition, Encoding (+17 more)

### Community 159 - ".OpenArchive"
Cohesion: 0.20
Nodes (6): CancellationToken, CommandContext, CancellationToken, CommandContext, CancellationToken, CommandContext

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

### Community 166 - ".Resolve"
Cohesion: 0.29
Nodes (5): Func, IEnumerable, IReadOnlyList, SafeResourceLookup, SafeResourceMatch

### Community 167 - "Tested build and fixture"
Cohesion: 0.67
Nodes (3): Diagnostic asset, Stock options, Tested build and fixture

### Community 168 - "Extras and custom properties"
Cohesion: 0.67
Nodes (3): Extras and custom properties, JSON value behavior, Scope survival matrix

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 170 - "ExtractCommand"
Cohesion: 0.29
Nodes (7): CancellationToken, CommandContext, List, ExtractCommand, ExtractSettings, extracted, failed

### Community 172 - "Underscore-prefixed custom attributes"
Cohesion: 0.67
Nodes (3): Identity, order, collision, and merge behavior, Supported import shapes, Underscore-prefixed custom attributes

### Community 173 - "KhronosValidatorServer"
Cohesion: 0.20
Nodes (8): List, KhronosValidatorServer, ValidatorResult, IAsyncDisposable, Process, ValidatorCode, ValidatorResult, ValueTask

### Community 174 - "EarthTool.WD.GUI"
Cohesion: 0.29
Nodes (4): EarthTool.WD.GUI, AppBuilder, STAThread, Program

### Community 175 - ".Match"
Cohesion: 0.32
Nodes (4): WalkingSkeletonConsumer, Action, Func, MeshAssetKind

### Community 177 - "GltfInterchange.cs"
Cohesion: 0.43
Nodes (7): Exception, AmbiguousPartitionCorrespondenceException, MetadataIdentityException, RequiredTextureResourceBindingException, ResourceLimitException, StaleNativeProjectionException, StaticLightMetadataException

### Community 178 - "ResolutionBudget"
Cohesion: 0.25
Nodes (4): IEnumerable, int, long, ResolutionBudget

### Community 179 - "GltfWalkingSkeletonTests"
Cohesion: 0.07
Nodes (8): Action, Guid, IReadOnlyList, JsonObject, List, BlenderOutputEvidence, GltfWalkingSkeletonTests, JsonArray

### Community 180 - "IReadOnlyList"
Cohesion: 0.09
Nodes (21): DynamicAnimationLayout, DynamicAnimationTrack, DynamicEffectPreview, DynamicImageLayout, DynamicMeshLayout, DynamicObjectScope, BinaryWriter, IDictionary (+13 more)

### Community 181 - "DynamicFrameSelection"
Cohesion: 0.43
Nodes (3): DynamicFrameSelection, DynamicTextureRegion, IEquatable

### Community 182 - "DestructibleEntity"
Cohesion: 0.04
Nodes (39): ArtifactType, DamageFlags, ExplosionFlags, HitType, MissileType, PassiveMask, RocketType, StandType (+31 more)

### Community 183 - "QualificationProfiler"
Cohesion: 0.27
Nodes (8): long, object, string, ProfileScope, QualificationProfiler, TimingAggregate, QualificationProfiler, TimeSpan

### Community 184 - "ParameterReader"
Cohesion: 0.46
Nodes (4): BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 185 - "IDisposable"
Cohesion: 0.29
Nodes (5): Type, CommandTypeResolver, IDisposable, IHost, ITypeResolver

### Community 186 - "GltfPlanAndReport.cs"
Cohesion: 0.48
Nodes (6): int, IReadOnlyList, string, GltfCliReport, GltfCliReportFormat, GltfImportPlanFormat

### Community 188 - "ListCommand"
Cohesion: 0.47
Nodes (4): CancellationToken, CommandContext, ListCommand, ListSettings

### Community 189 - ".Create"
Cohesion: 0.11
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

### Community 190 - "IExtractor"
Cohesion: 0.33
Nodes (3): Task, IExtractor, IWDExtractor

### Community 192 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

## Knowledge Gaps
- **336 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+331 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **12 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `FileType`, `IValueConverter`, `IEarthInfo`, `EarthTool.CLI.Commands.MSH`?**
  _High betweenness centrality (0.157) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `MetadataConflictCollector`, `InterchangeBaseline`, `AnimationClassBytes`, `StaticMeshAssembler`, `DynamicGltfInterchangeTests`, `StaticAnimationProjection`, `GltfInterchange.cs`, `GlbDocument.cs`, `DynamicFrameSelection`, `DynamicEffectBehavior`, `EarthTool.CLI.Commands.MSH`, `GltfPlanAndReport.cs`?**
  _High betweenness centrality (0.110) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `Task` to `InterchangeBaseline`, `IDisposable`, `DynamicGltfInterchangeTests`?**
  _High betweenness centrality (0.097) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _336 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10465116279069768 - nodes in this community are weakly interconnected._
- **Should `IArchiveItem` be split into smaller, more focused modules?**
  _Cohesion score 0.14210526315789473 - nodes in this community are weakly interconnected._