# Graph Report - EarthTool  (2026-08-04)

## Corpus Check
- 364 files · ~296,880 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4715 nodes · 14320 edges · 188 communities (181 shown, 7 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 977 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d5f56e67`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- .WriteFileAsync
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- DynamicGltfDocument
- ArchiveItem
- .ToByteArray
- .Load
- IValueConverter
- MainWindowViewModel
- OperationResult
- DynamicGltfInterchangeTests
- Vector3
- MshOperationProfile
- .OpenArchive
- DynamicEffectExtension
- Dynamic MESH Binary Layout
- GltfPlanAndReportTests
- MeshAsset
- release-qualification.mjs
- MainWindowViewModel
- GltfImportPlanSerializer
- MainWindowViewModel
- ITransactionalFileSystem
- .ToByteArray
- Common MSH Base Header
- MetadataGraphValidationTests
- ArchiveTests
- EarthTool.MSH.Assets
- ArchiverServiceTests
- EarthTool.CLI
- DynamicMeshAssetTests
- .Create
- EarthTool.Common
- .CreateMockHeader
- InterchangeBaseline
- StaticMeshEditSession
- EarthTool.CLI.Commands.WD
- ArchiveInfoViewModel
- EarthTool.WD.Models
- TexPreviewLoader
- GltfInterchange
- Static Mesh Header
- StaticObject Record
- ParsedGlb
- StaticAnimationProjection
- EntityDetailsViewModel
- ParFile
- PublicApiApproval
- IEarthInfo
- .Create
- GlbDocument
- OfficialCorpusQualification
- IReadOnlyList
- IReadOnlyList
- Entity
- EarthTool.MSH.Tests
- Fact
- DialogService
- EarthTool.PAR
- EarthTool.PAR.Enums
- .ToByteArray
- EarthTool.PAR.GUI.ViewModels
- ConvertCommand
- EarthTool.PAR.GUI
- MetadataEnvelope
- VerticalTransporter
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- GltfCommandSettings.cs
- Equipment
- Task
- Weapon
- OperationDiagnostic
- EarthTool.Common.GUI.ViewModels
- EarthTool.sln
- EarthTool.Common.Interfaces
- ResearchReferenceCollectionEditorViewModel
- BinaryExtensions
- .ReadGlbJson
- StaticMeshAsset
- Blender 4.5 glTF round-trip research
- StaticAnimationMshFixture
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- .Write_And_Read_AreSymmetric
- CommonCommand
- OneTriangleMshFixture
- EarthTool WD Archive Manager
- .WriteReportAsync
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- GltfCliReportSerializer
- NotificationService
- EarthTool.Common.GUI.Enums
- StaticMeshAsset.cs
- EarthTool Suite
- WD Central Directory
- ItemCommand
- IArchiveItem
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- KhronosValidatorServer
- Reader
- EarthTool.PAR.GUI.Services
- JsonElement
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- ParameterReader
- .LoadPreview
- EarthTool.TEX
- .GenerateSampleData
- EarthTool.TEX
- gltf.md
- GltfWalkingSkeletonTests
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- Modify An Existing Mesh
- PropertyEditorFactory
- Base Header
- IArchive
- UnitTest1.cs
- EarthTool Installation Guide
- .CreateBinary
- InMemoryArchiveDataSourceTests
- ConvertCommand
- Dependabot Dependency Automation
- .DeserializeAsync
- Setup .NET Environment
- Mesh Attachments 1..49
- MeshAssetLineageId
- Code Quality Analysis Job
- Dynamic Color
- ResolutionBudget
- App
- Mesh Artist Quick Start And Cheat Sheet
- package.json
- CommandTypeRegistrar
- Migrate From COLLADA To glTF
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- App
- MainWindow
- .ImportEditGlbAsync
- MshCanonicalSerializer
- Runner
- validate-glb.mjs
- EarthTool.TEX.GUI/App.axaml.cs
- Decision consequences for later tickets
- Official MSH Qualification Performance
- .Resolve
- Tested build and fixture
- Extras and custom properties
- EarthTool.CLI.Tests
- Task
- Program
- Underscore-prefixed custom attributes
- .ValidateOwnedAccessor
- .Decode
- .ToByteArray
- Program
- .ResolveAndLoad
- .EditImportSamplesCubicSplineWithoutPreservingTangents
- IReadOnlyList
- TreeItemViewModel
- EquipableEntity
- ParsedGltfAnimationChannel
- IExtractor
- ViewLocator
- .CollectNewModelAnimationPaths
- ParameterReaderTests
- .Create

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 253 edges
2. `GltfInterchange` - 202 edges
3. `GlbDocument` - 152 edges
4. `DynamicGltfDocument` - 121 edges
5. `DynamicGltfInterchangeTests` - 96 edges
6. `OperationDiagnostic` - 94 edges
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

## Communities (188 total, 7 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - ".WriteFileAsync"
Cohesion: 0.12
Nodes (13): Stream, ITransactionalFileSystem, TransactionalFileSystem, CancellationToken, Exception, IEnumerable, ILogger, Stream (+5 more)

### Community 2 - "AssetResult"
Cohesion: 0.23
Nodes (8): AssetResult, DiagnosticKey, Task, AssetResult, KhronosValidatorServer, OperationCounts, ProfileScope, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.11
Nodes (16): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+8 more)

### Community 5 - "DynamicGltfDocument"
Cohesion: 0.09
Nodes (22): DynamicEditedPreview, float, int, ReadOnlySpan, Stream, string, Translation, uint (+14 more)

### Community 6 - "ArchiveItem"
Cohesion: 0.24
Nodes (6): Stream, ICompressor, Encoding, bool, ReadOnlyMemory, ArchiveItem

### Community 7 - ".ToByteArray"
Cohesion: 0.09
Nodes (12): Encoding, Encoding, IEnumerable, TypedEntity, Encoding, Encoding, Encoding, Encoding (+4 more)

### Community 8 - ".Load"
Cohesion: 0.20
Nodes (10): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IReadOnlyDictionary, IReadOnlyList, Vector3, MshPreviewLoader (+2 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.10
Nodes (8): bool, ILogger, ObservableCollection, ReactiveCommand, string, Task, Unit, MainWindowViewModel

### Community 11 - "OperationResult"
Cohesion: 0.17
Nodes (9): IReadOnlyList, OperationResult, GltfOperationProfile, CancellationToken, SeparateGltfPackage, Stream, Task, GltfImportPlan (+1 more)

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.06
Nodes (35): DynamicAlphaTiming, DynamicEffectType, DynamicMeshAsset, EffectRectangle, IEnumerable, Vector3, CanonicalDynamicAlpha, CanonicalDynamicEffectShape (+27 more)

### Community 13 - "Vector3"
Cohesion: 0.11
Nodes (12): Action, BinaryWriter, float, Quaternion, Translation, Vector3, AttachmentHeadingProjection, ProjectedAttachment (+4 more)

### Community 14 - "MshOperationProfile"
Cohesion: 0.11
Nodes (20): DecodedStaticRecord, MeshAssetOrigin, CancellationToken, Guid, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+12 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "DynamicEffectExtension"
Cohesion: 0.12
Nodes (12): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, ReadOnlySpan, DynamicEffectExtension (+4 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "GltfPlanAndReportTests"
Cohesion: 0.13
Nodes (14): BufferPath, ConflictKey, Directory, Fact, Guid, InlineData, JsonNode, Task (+6 more)

### Community 19 - "MeshAsset"
Cohesion: 0.14
Nodes (14): CancellationToken, Stream, Task, Action, byte, Func, MeshAsset, MeshAssetKind (+6 more)

### Community 20 - "release-qualification.mjs"
Cohesion: 0.07
Nodes (62): corpusBinaryStages, corpusInterchangeStages, recognizedDynamicEffectTypes, assertPrivacySafe(), buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames() (+54 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.10
Nodes (14): IEnumerable, Task, IDialogService, INotificationService, Task, bool, ILogger, object (+6 more)

### Community 22 - "GltfImportPlanSerializer"
Cohesion: 0.25
Nodes (5): JsonElement, SeparateGltfPackage, GltfImportPlanSerializer, ImportPlanException, JsonValueKind

### Community 23 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (11): Bitmap, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap, string (+3 more)

### Community 24 - "ITransactionalFileSystem"
Cohesion: 0.05
Nodes (13): Stream, ITransactionalFileSystem, TransactionalFileSystem, int, Stream, ManifestFailingFileSystem, CancellationTokenSource, Stream (+5 more)

### Community 25 - ".ToByteArray"
Cohesion: 0.16
Nodes (7): Encoding, Encoding, Encoding, Encoding, Encoding, Fact, VehicleSerializationTests

### Community 26 - "Common MSH Base Header"
Cohesion: 0.10
Nodes (23): Model MSH Framing and Record Extensions Explicitly, Canonical Next Record Markers, MSH Footprint API, MSH Horizontal Extents API, IMeshBaseHeader, Legacy MSH Model Migration, MSH API, MSH Slots API (+15 more)

### Community 27 - "MetadataGraphValidationTests"
Cohesion: 0.13
Nodes (13): Baseline, Action, Bytes, Fact, Func, Guid, ICollection, InlineData (+5 more)

### Community 28 - "ArchiveTests"
Cohesion: 0.15
Nodes (8): bool, DateTime, IReadOnlyCollection, MemoryMappedFile, Archive, Fact, ArchiveTests, SortedSet

### Community 29 - "EarthTool.MSH.Assets"
Cohesion: 0.07
Nodes (40): CliResult, EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.CLI.Commands.MSH, EarthTool.MSH (+32 more)

### Community 30 - "ArchiverServiceTests"
Cohesion: 0.14
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

### Community 34 - "EarthTool.Common"
Cohesion: 0.07
Nodes (15): EarthTool.PAR, EarthTool.Common, EarthTool.CLI, EarthTool.CLI.Commands, Type, CommandTypeResolver, CommonSettings, IServiceCollection (+7 more)

### Community 35 - ".CreateMockHeader"
Cohesion: 0.26
Nodes (3): Fact, ArchiveItemTests, Guid

### Community 36 - "InterchangeBaseline"
Cohesion: 0.08
Nodes (33): Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfArtistObjectLocalIds, GltfDiagnosticCodes, GltfDynamicEditImportResult, GltfEditImportOptions (+25 more)

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.07
Nodes (23): UnchangedEmitterOwnership, AnimationClassBytes, SourceObjectId, StaticRenderObjectId, StaticSourceObject, bool, Dictionary, HashSet (+15 more)

### Community 38 - "EarthTool.CLI.Commands.WD"
Cohesion: 0.07
Nodes (32): Command, CommandSettings, EarthTool.CLI.Commands.WD, AddCommand, CreateCommand, CancellationToken, CommandContext, DebugCommand (+24 more)

### Community 39 - "ArchiveInfoViewModel"
Cohesion: 0.15
Nodes (7): DateTime, int, long, string, ArchiveInfoViewModel, ArchiveItemViewModel, ViewModelBase

### Community 40 - "EarthTool.WD.Models"
Cohesion: 0.09
Nodes (16): EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Models, EarthTool.WD.Interfaces, EarthTool.WD.Models, ReadOnlyMemory, IArchiveDataSource, ReadOnlyMemory, InMemoryArchiveDataSource (+8 more)

### Community 41 - "TexPreviewLoader"
Cohesion: 0.13
Nodes (14): byte, CancellationToken, Exception, GltfExportOptions, GltfOperationProfile, ICollection, IReadOnlyDictionary, IReadOnlyList (+6 more)

### Community 42 - "GltfInterchange"
Cohesion: 0.05
Nodes (10): AnimationEditPlan, AnimationReplacement, Func, IEnumerable, JsonNode, JsonObject, AnimationEditPlan, GltfInterchange (+2 more)

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - "ParsedGlb"
Cohesion: 0.12
Nodes (9): GltfAnimationHandle, GltfLightHandle, GltfMaterialHandle, GltfNewModelImportOptions, GltfNodeHandle, ISet, ParsedGlb, EmitterOwnershipPlan (+1 more)

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.13
Nodes (17): AnimationObjectLayout, AnimationReplacement, NewModelAnimationTrack, BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion (+9 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.05
Nodes (33): EntityClassType, bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, Action (+25 more)

### Community 48 - "ParFile"
Cohesion: 0.12
Nodes (12): Task, IParFileService, ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile (+4 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (21): FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo (+13 more)

### Community 51 - ".Create"
Cohesion: 0.07
Nodes (21): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, Fact, IEnumerable (+13 more)

### Community 52 - "GlbDocument"
Cohesion: 0.07
Nodes (15): CarrierKind, GltfOperationProfile, ICollection, IDictionary, JsonDocument, JsonElement, Path, ReadOnlySpan (+7 more)

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.17
Nodes (10): ContentFingerprint, BinaryWriter, IEnumerable, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification (+2 more)

### Community 54 - "IReadOnlyList"
Cohesion: 0.12
Nodes (24): PartitionMatch, ImportPlanException, DynamicMetadataIdentityException, IReadOnlyList, ByteArrayComparer, GeometryPartition, MalformedMetadataException, MetadataAnimationClass (+16 more)

### Community 55 - "IReadOnlyList"
Cohesion: 0.07
Nodes (29): Discarded, GltfNewModelStaticLightOptions, Action, BinaryWriter, IDictionary, IReadOnlyCollection, IReadOnlyDictionary, IReadOnlyList (+21 more)

### Community 56 - "Entity"
Cohesion: 0.09
Nodes (22): EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List, ValidationError, ValidationResult, ValidationSeverity (+14 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "Fact"
Cohesion: 0.11
Nodes (3): Fact, JsonObject, JsonArray

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 62 - "EarthTool.PAR.Enums"
Cohesion: 0.03
Nodes (75): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.Extensions, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.PAR.Tests.Services, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Tests.Factories (+67 more)

### Community 63 - ".ToByteArray"
Cohesion: 0.11
Nodes (10): Encoding, Encoding, Encoding, Encoding, Encoding, Encoding, Encoding, Encoding (+2 more)

### Community 64 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.06
Nodes (31): EarthTool.PAR.GUI, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Views, Encoding, IBinarySerializable, Faction, ResearchType, ObservableCollection (+23 more)

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
Cohesion: 0.09
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "GltfCommandSettings.cs"
Cohesion: 0.22
Nodes (13): AsyncCommand, CancellationToken, CommandContext, Task, ExportGltfCommand, ImportEditGltfCommand, ImportNewGltfCommand, Guid (+5 more)

### Community 72 - "Equipment"
Cohesion: 0.08
Nodes (17): LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable, Equipment (+9 more)

### Community 73 - "Task"
Cohesion: 0.07
Nodes (27): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+19 more)

### Community 74 - "Weapon"
Cohesion: 0.11
Nodes (10): BarrelBetaType, TargetType, WeaponFireType, Encoding, Encoding, Encoding, IEnumerable, Weapon (+2 more)

### Community 75 - "OperationDiagnostic"
Cohesion: 0.11
Nodes (9): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, ParseScopeResolution, IReadOnlyList, AuthoringValidation, CanonicalStaticSourceObject, MshEditResult (+1 more)

### Community 76 - "EarthTool.Common.GUI.ViewModels"
Cohesion: 0.17
Nodes (9): EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, ReactiveCommand, Unit, AboutViewModel, ViewModelBase, ParAboutViewModel, TexAboutViewModel (+1 more)

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.07
Nodes (17): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Services, EarthTool.Common.Models, EarthTool.WD.Services, EarthTool.WD (+9 more)

### Community 79 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.10
Nodes (14): bool, object, ObservableCollection, Type, FlagsPropertyEditorViewModel, FlagValueViewModel, Action, bool (+6 more)

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - ".ReadGlbJson"
Cohesion: 0.10
Nodes (3): JsonDocument, JsonElement, Vector3

### Community 82 - "StaticMeshAsset"
Cohesion: 0.11
Nodes (13): GltfArtistObjectLocalIds, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, NativeProjectionFingerprint, Utf8JsonWriter, GltfPackage, StaticSourceObjectTraversal (+5 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.20
Nodes (10): Animations, Blender 4.5 glTF round-trip research, Conclusion, Evidence model, Meshes, primitives, and topology, Nodes, hierarchy, scenes, and transforms, Primary sources, Punctual lights (+2 more)

### Community 84 - "StaticAnimationMshFixture"
Cohesion: 0.19
Nodes (6): AnimationLengths, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - ".Write_And_Read_AreSymmetric"
Cohesion: 0.38
Nodes (3): Writer, Fact, ParameterWriterTests

### Community 88 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - ".WriteReportAsync"
Cohesion: 0.15
Nodes (5): Stream, CliReportFileSystem, ICliReportFileSystem, Exception, GltfCliReport

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "GltfCliReportSerializer"
Cohesion: 0.22
Nodes (4): GltfStaticObjectRoles, IEnumerable, Utf8JsonWriter, GltfCliReportSerializer

### Community 95 - "NotificationService"
Cohesion: 0.23
Nodes (6): Exception, NotificationEventArgs, Exception, ILogger, NotificationService, EventArgs

### Community 96 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.16
Nodes (9): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.Views, NotificationType, IServiceCollection, ServiceCollectionExtensions, AboutView (+1 more)

### Community 97 - "StaticMeshAsset.cs"
Cohesion: 0.20
Nodes (12): IReadOnlyList, Matrix4x4, Vector3, CommonMeshBaseHeader, StaticAnimationClass, StaticAnimationTracks, StaticRenderObject, StaticRenderObjectFlagMasks (+4 more)

### Community 98 - "EarthTool Suite"
Cohesion: 0.22
Nodes (10): EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management, MSH Model Export Workflow (+2 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "ItemCommand"
Cohesion: 0.25
Nodes (6): EarthTool.CLI.Commands.PAR, CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings

### Community 101 - "IArchiveItem"
Cohesion: 0.16
Nodes (6): ReadOnlyMemory, IArchiveItem, ITextFlagService, HashSet, TextFlagService, IComparable

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

### Community 106 - "KhronosValidatorServer"
Cohesion: 0.20
Nodes (8): List, KhronosValidatorServer, ValidatorResult, IAsyncDisposable, Process, ValidatorCode, ValidatorResult, ValueTask

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - "EarthTool.PAR.GUI.Services"
Cohesion: 0.05
Nodes (30): EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.Models, Action, DateTime, UndoAction, Action, IEnumerable, IUndoRedoService (+22 more)

### Community 109 - "JsonElement"
Cohesion: 0.13
Nodes (9): DynamicSceneLayout, CancellationToken, GltfOperationProfile, InterchangeBaseline, JsonDocument, JsonElement, ReadOnlyMemory, DynamicGltfImport (+1 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.12
Nodes (16): CancellationToken, Func, IEnumerable, IReadOnlyList, Task, TextWriter, GltfCommandExecutor, OperationStatus (+8 more)

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

### Community 116 - "ParameterReader"
Cohesion: 0.20
Nodes (7): Reader, FileType, IReader, BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 117 - ".LoadPreview"
Cohesion: 0.33
Nodes (4): PreviewResolution, PreviewResolution, PreviewResolutionKind, TexResolutionBudget

### Community 118 - "EarthTool.TEX"
Cohesion: 0.09
Nodes (18): EarthTool.TEX, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, IServiceCollection, HostExtensions, IEnumerable, TexHeader, TexImage (+10 more)

### Community 119 - ".GenerateSampleData"
Cohesion: 0.24
Nodes (4): Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "gltf.md"
Cohesion: 0.33
Nodes (3): Dynamic effect-preview contract, glTF API, Static-light authoring contract

### Community 122 - "GltfWalkingSkeletonTests"
Cohesion: 0.13
Nodes (3): Guid, BlenderOutputEvidence, GltfWalkingSkeletonTests

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

### Community 128 - "PropertyEditorFactory"
Cohesion: 0.24
Nodes (7): Action, HashSet, IEnumerable, ILogger, Type, PropertyEditorFactory, PropertyInfo

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "IArchive"
Cohesion: 0.07
Nodes (27): DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid, IArchiveFactory, ReadOnlySpan (+19 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - ".CreateBinary"
Cohesion: 0.20
Nodes (7): AnimationLayout, Matrix4x4, MemoryStream, PartitionLayout, PartitionLayout, PreviewLayout, ProjectedPartition

### Community 135 - "ConvertCommand"
Cohesion: 0.16
Nodes (10): IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+2 more)

### Community 136 - "Dependabot Dependency Automation"
Cohesion: 0.50
Nodes (4): Dependabot Dependency Automation, Weekly GitHub Actions Updates, Weekly NuGet Updates, Security Check Job

### Community 137 - ".DeserializeAsync"
Cohesion: 0.40
Nodes (4): CancellationToken, IReadOnlyDictionary, Stream, Task

### Community 138 - "Setup .NET Environment"
Cohesion: 0.67
Nodes (3): .NET SDK Setup, NuGet Package Cache, Setup .NET Environment

### Community 139 - "Mesh Attachments 1..49"
Cohesion: 0.67
Nodes (3): Trailing Hierarchy Unwind Count, Mesh Attachments 1..49, Mesh Extents

### Community 140 - "MeshAssetLineageId"
Cohesion: 0.13
Nodes (8): Guid, IEnumerable, WalkingSkeletonConsumer, Guid, MeshAssetLineageId, MshBuildResult, IEnumerable, MshExpert

### Community 143 - "ResolutionBudget"
Cohesion: 0.25
Nodes (4): IEnumerable, int, long, ResolutionBudget

### Community 148 - "App"
Cohesion: 0.33
Nodes (3): IServiceCollection, App, IServiceProvider

### Community 149 - "Mesh Artist Quick Start And Cheat Sheet"
Cohesion: 0.33
Nodes (6): Attachment Identifier Cheat Sheet, Choose The Correct Workflow, Create A Standalone MSH, Directional Empty Presentation In Blender, Fast Checks Before Import, Mesh Artist Quick Start And Cheat Sheet

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "CommandTypeRegistrar"
Cohesion: 0.24
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 152 - "Migrate From COLLADA To glTF"
Cohesion: 0.33
Nodes (6): API migration, Attachment helper name migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 153 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 154 - "App"
Cohesion: 0.22
Nodes (5): Application, IServiceCollection, App, IServiceCollection, App

### Community 155 - "MainWindow"
Cohesion: 0.16
Nodes (8): Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs, RoutedEventArgs, Window

### Community 158 - "MshCanonicalSerializer"
Cohesion: 0.09
Nodes (25): CanonicalStaticRecord, MeshArchiveFraming, CanonicalHorizontalExtents, CanonicalStaticFootprint, CanonicalStaticRenderObject, CanonicalStaticVertex, CanonicalTriangle, StaticAnimationReplacement (+17 more)

### Community 159 - "Runner"
Cohesion: 0.08
Nodes (21): ChannelReader, ChannelWriter, DynamicCoverage, Dictionary, Guid, IDictionary, int, IReadOnlyDictionary (+13 more)

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - "EarthTool.TEX.GUI/App.axaml.cs"
Cohesion: 0.14
Nodes (8): EarthTool.TEX.GUI, EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, Control, ViewLocator, Control, ViewLocator, IDataTemplate

### Community 164 - "Decision consequences for later tickets"
Cohesion: 0.40
Nodes (5): Decision consequences for later tickets, EarthTool metadata requirements, Native glTF candidates, Required fingerprints and invalidation, What stock Blender cannot promise

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Historical Measured Result, Official MSH Qualification Performance, Stage Profiling, Aggregate release qualification, Blender matrix, Official MSH corpus

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

### Community 170 - "Task"
Cohesion: 0.09
Nodes (5): BlenderOutputEvidence, IEnumerable, Task, Trait, Action

### Community 171 - "Program"
Cohesion: 0.40
Nodes (3): AppBuilder, STAThread, Program

### Community 172 - "Underscore-prefixed custom attributes"
Cohesion: 0.67
Nodes (3): Identity, order, collision, and merge behavior, Supported import shapes, Underscore-prefixed custom attributes

### Community 173 - ".ValidateOwnedAccessor"
Cohesion: 0.28
Nodes (5): ICollection, ISet, End, NativeObjectGraph, Start

### Community 174 - ".Decode"
Cohesion: 0.18
Nodes (7): BinaryReader, IEnumerable, int, long, TexResolutionBudget, HasVariants, Preview

### Community 175 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 176 - "Program"
Cohesion: 0.40
Nodes (3): AppBuilder, STAThread, Program

### Community 179 - ".EditImportSamplesCubicSplineWithoutPreservingTangents"
Cohesion: 0.25
Nodes (3): Action, IReadOnlyList, List

### Community 180 - "IReadOnlyList"
Cohesion: 0.10
Nodes (21): DynamicAnimationLayout, DynamicAnimationTrack, DynamicEffectPreview, DynamicImageLayout, DynamicMeshLayout, DynamicObjectScope, DynamicRecordSlice, BinaryWriter (+13 more)

### Community 181 - "TreeItemViewModel"
Cohesion: 0.24
Nodes (5): HashSet, bool, Guid, ObservableCollection, TreeItemViewModel

### Community 182 - "EquipableEntity"
Cohesion: 0.06
Nodes (18): BuildingExType, BuildingTabType, BuildingType, ConnectorType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags (+10 more)

### Community 183 - "ParsedGltfAnimationChannel"
Cohesion: 0.23
Nodes (6): int, string, ParsedAnimationBuilder, ParsedGltfAnimation, ParsedGltfAnimationChannel, ParsedGltfAnimationObject

### Community 184 - "IExtractor"
Cohesion: 0.33
Nodes (3): Task, IExtractor, IWDExtractor

### Community 185 - "ViewLocator"
Cohesion: 0.18
Nodes (6): EarthTool.WD.GUI, AppBuilder, STAThread, Program, Control, ViewLocator

### Community 186 - ".CollectNewModelAnimationPaths"
Cohesion: 0.33
Nodes (4): ICollection, Path, ReadOnlySpan, NodeIndex

### Community 189 - ".Create"
Cohesion: 0.09
Nodes (7): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord, JsonNode

## Knowledge Gaps
- **333 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+328 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `EarthTool.Common`, `ConvertCommand`, `EarthTool.WD.Models`, `IValueConverter`, `ParFile`, `IEarthInfo`, `ParameterReader`, `EarthTool.TEX`, `EarthTool.MSH.Assets`?**
  _High betweenness centrality (0.152) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `Task` to `IArchive`, `DynamicGltfInterchangeTests`?**
  _High betweenness centrality (0.120) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `StaticMeshAsset.cs`, `MetadataEnvelope`, `InterchangeBaseline`, `DynamicGltfInterchangeTests`, `StaticAnimationProjection`, `DynamicEffectExtension`, `IReadOnlyList`, `MshCanonicalSerializer`?**
  _High betweenness centrality (0.101) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _333 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.WriteFileAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.11605937921727395 - nodes in this community are weakly interconnected._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10852713178294573 - nodes in this community are weakly interconnected._