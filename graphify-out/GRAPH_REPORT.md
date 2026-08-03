# Graph Report - EarthTool  (2026-08-03)

## Corpus Check
- 358 files · ~252,496 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4185 nodes · 11744 edges · 172 communities (168 shown, 4 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 750 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `1630685e`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- .WriteFileAsync
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- ParsedGlb
- Task
- EarthTool.PAR.Models.Abstracts
- NotificationService
- IValueConverter
- MainWindowViewModel
- OperationResult
- GltfImportPlanSerializer
- .CreateStaticLightGuards
- MshV1Decoder
- .OpenArchive
- CanonicalDynamicObject
- Dynamic MESH Binary Layout
- Equipment
- MeshAsset
- release-qualification.mjs
- MainWindowViewModel
- EarthTool.PAR.Models
- MainWindowViewModel
- ITransactionalFileSystem
- Vehicle
- Common MSH Base Header
- MetadataGraphValidationTests
- .CreateMockHeader
- EarthTool.MSH.Assets
- GlbDocument
- EarthTool.CLI
- DynamicMeshAssetTests
- .Create
- IArchive
- OfficialCorpusQualification
- EntityGroup
- StaticMeshEditSession
- OfficialCorpusCliOracle
- TreeNodeViewModelBase
- GltfWalkingSkeletonTests
- ResearchReferenceCollectionEditorViewModel
- PassiveEntity
- Static Mesh Header
- StaticObject Record
- .GenerateSampleData
- StaticAnimationProjection
- EntityDetailsViewModel
- IArchiveItem
- PublicApiApproval
- IEarthInfo
- CanonicalMeshAuthoringTests
- PropertyEditorViewModel
- .Create
- IReadOnlyList
- IReadOnlyList
- Entity
- EarthTool.MSH.Tests
- .RewriteStatic
- DialogService
- EarthTool.PAR
- GltfCliReportOperation
- InteractableEntity
- DestructibleEntity
- EarthTool.Common.GUI.Enums
- MeshAssetAuthoring.cs
- EarthTool.PAR.GUI
- MetadataEnvelope
- EditableEntity
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- official-corpus-qualification.mjs
- Fact
- .RunAsync
- Runner
- AuthoringValidation
- IReadOnlyList
- EarthTool.sln
- EarthTool.Common.Interfaces
- TreeItemViewModel
- BinaryExtensions
- EarthTool.TEX
- StaticMeshAsset
- Blender 4.5 glTF round-trip research
- MshCanonicalSerializer
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- ConvertCommand
- ParsedGltfAnimationChannel
- FlagsPropertyEditorViewModel
- EarthTool WD Archive Manager
- IUndoRedoService
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- GltfInterchange
- MshOperationProfile
- ViewLocator
- KhronosValidatorServer
- EarthTool Suite
- WD Central Directory
- VerticalTransporter
- EquipableEntity
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- EntityConverter
- Reader
- IDialogService
- TexPreviewLoader
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- EnumPropertyEditorViewModel
- CommonCommand
- EntityFactoryTests.cs
- EarthTool.TEX
- .CollectNewModelAnimationPaths
- .ExportGlbAsync
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- GltfCommandSettings.cs
- ParFile
- ViewLocator
- Base Header
- MappedArchiveDataSource
- UnitTest1.cs
- EarthTool Installation Guide
- StaticRenderObject
- .RoundTripAsync
- ConvertCommand
- Dependabot Dependency Automation
- ITransactionalFileSystem
- Setup .NET Environment
- Mesh Attachments 1..49
- .GetCanonicalStaticSerializedLength
- Code Quality Analysis Job
- Dynamic Color
- .Write_And_Read_AreSymmetric
- package.json
- IDisposable
- GltfContracts.cs
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- App
- .ToByteArray
- ArchiverServiceTests
- validate-glb.mjs
- ItemCommand
- Official MSH Qualification Performance
- .WriteReportAsync
- EarthTool.CLI.Tests
- EarthTool.PAR.GUI
- Migrate From COLLADA To glTF
- PublicCutoverAcceptanceTests
- MainWindow
- GltfInterchange.cs
- EarthTool.PAR.Enums
- EarthTool.GLTF/HostExtensions.cs
- EarthTool.Common
- .Create

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 179 edges
2. `GltfInterchange` - 166 edges
3. `GlbDocument` - 138 edges
4. `EarthTool.PAR.Enums` - 90 edges
5. `OperationDiagnostic` - 82 edges
6. `MetadataGraphValidationTests` - 77 edges
7. `StaticMeshAsset` - 75 edges
8. `OperationResult` - 72 edges
9. `EarthTool.PAR.Models` - 64 edges
10. `ParsedGlb` - 59 edges

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

## Communities (172 total, 4 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - ".WriteFileAsync"
Cohesion: 0.21
Nodes (10): CancellationToken, Exception, IEnumerable, ILogger, Stream, Task, MshReader, MshValidator (+2 more)

### Community 2 - "AssetResult"
Cohesion: 0.22
Nodes (8): AssetResult, DiagnosticKey, GltfExportOptions, Task, AssetResult, KhronosValidatorServer, ProfileScope, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.11
Nodes (15): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+7 more)

### Community 5 - "ParsedGlb"
Cohesion: 0.14
Nodes (4): InterchangeBaseline, JsonObject, ISet, ParsedGlb

### Community 6 - "Task"
Cohesion: 0.10
Nodes (4): BlenderOutputEvidence, IEnumerable, Task, Trait

### Community 7 - "EarthTool.PAR.Models.Abstracts"
Cohesion: 0.07
Nodes (23): EarthTool.PAR.Extensions, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Factories, Encoding, IEnumerable, TypelessEntity, IEnumerable, Parameter (+15 more)

### Community 8 - "NotificationService"
Cohesion: 0.19
Nodes (7): NotificationType, Exception, NotificationEventArgs, Exception, ILogger, NotificationService, EventArgs

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (10): Task, IParFileService, bool, ILogger, ObservableCollection, ReactiveCommand, string, Task (+2 more)

### Community 11 - "OperationResult"
Cohesion: 0.15
Nodes (12): IReadOnlyList, OperationResult, GltfEditImportResult, GltfNewModelImportResult, GltfOperationProfile, CancellationToken, SeparateGltfPackage, Stream (+4 more)

### Community 12 - "GltfImportPlanSerializer"
Cohesion: 0.07
Nodes (23): BufferPath, ConflictKey, Directory, CancellationToken, IEnumerable, JsonElement, SeparateGltfPackage, Stream (+15 more)

### Community 13 - ".CreateStaticLightGuards"
Cohesion: 0.14
Nodes (8): Action, BinaryWriter, MemoryStream, Quaternion, Vector3, ProjectedAttachment, ProjectedCannonRenderPosition, ProjectedStaticLight

### Community 14 - "MshV1Decoder"
Cohesion: 0.13
Nodes (14): CancellationToken, Guid, IEnumerable, int, IReadOnlyDictionary, List, Matrix4x4, ReadOnlySpan (+6 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "CanonicalDynamicObject"
Cohesion: 0.09
Nodes (29): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, ReadOnlySpan, DynamicAlphaTiming (+21 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "Equipment"
Cohesion: 0.07
Nodes (19): ConnectorType, LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable (+11 more)

### Community 19 - "MeshAsset"
Cohesion: 0.17
Nodes (14): CancellationToken, FailingMshWriter, Action, byte, Func, IReadOnlyList, CommonMeshBaseHeader, DynamicMeshAsset (+6 more)

### Community 20 - "release-qualification.mjs"
Cohesion: 0.15
Nodes (29): buildEvidence(), collectReceivedFiles(), countDiscoveredTests(), exists(), expectedArtifacts, expectedTestCounts, fail(), forbiddenReleasePaths (+21 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (11): INotificationService, ITextFlagService, bool, ILogger, object, ObservableCollection, ReactiveCommand, string (+3 more)

### Community 22 - "EarthTool.PAR.Models"
Cohesion: 0.12
Nodes (8): EarthTool.CLI.Commands.PAR, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, EarthTool.PAR.Models, ValidationError, ValidationSeverity

### Community 23 - "MainWindowViewModel"
Cohesion: 0.13
Nodes (12): Bitmap, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap, string (+4 more)

### Community 24 - "ITransactionalFileSystem"
Cohesion: 0.06
Nodes (11): EarthTool.GLTF.Internal, Stream, ITransactionalFileSystem, TransactionalFileSystem, CancellationTokenSource, Stream, CancellingAfterSidecarTransactionalFileSystem, CorruptingSidecarTransactionalFileSystem (+3 more)

### Community 25 - "Vehicle"
Cohesion: 0.07
Nodes (20): VehicleObjectType, Encoding, Encoding, IEnumerable, Builder, Encoding, Encoding, IEnumerable (+12 more)

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
Cohesion: 0.17
Nodes (16): EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.CLI.Commands.MSH, EarthTool.MSH, EarthTool.Consumer.Tests (+8 more)

### Community 30 - "GlbDocument"
Cohesion: 0.07
Nodes (16): CarrierKind, GltfOperationProfile, ICollection, IDictionary, JsonDocument, JsonElement, Matrix4x4, Path (+8 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Asset, byte, Bytes, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - ".Create"
Cohesion: 0.16
Nodes (9): int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture, OmniRecord (+1 more)

### Community 34 - "IArchive"
Cohesion: 0.06
Nodes (29): DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid, IArchiveFactory, Stream (+21 more)

### Community 35 - "OfficialCorpusQualification"
Cohesion: 0.17
Nodes (10): ContentFingerprint, BinaryWriter, IEnumerable, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification (+2 more)

### Community 36 - "EntityGroup"
Cohesion: 0.12
Nodes (13): Encoding, IBinarySerializable, Faction, ResearchType, ObservableCollection, EntityGroupNodeViewModel, ObservableCollection, ResearchTypeNodeViewModel (+5 more)

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.13
Nodes (13): SourceObjectId, StaticRenderObjectId, StaticSourceObject, bool, Dictionary, IEnumerable, int, Matrix4x4 (+5 more)

### Community 38 - "OfficialCorpusCliOracle"
Cohesion: 0.20
Nodes (9): CliProcessResult, CliReportOperation, IReadOnlyList, Task, CliDiagnostic, CliOracleResult, CliProcessResult, CliReportOperation (+1 more)

### Community 39 - "TreeNodeViewModelBase"
Cohesion: 0.12
Nodes (13): ObservableCollection, EntityGroupsRootNodeViewModel, ObservableCollection, FactionNodeViewModel, ObservableCollection, FactionResearchNodeViewModel, ObservableCollection, GroupTypeNodeViewModel (+5 more)

### Community 40 - "GltfWalkingSkeletonTests"
Cohesion: 0.09
Nodes (10): Action, Guid, InlineData, IReadOnlyList, JsonObject, List, Theory, BlenderOutputEvidence (+2 more)

### Community 41 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.20
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 42 - "PassiveEntity"
Cohesion: 0.13
Nodes (9): ArtifactType, PassiveMask, Encoding, IEnumerable, PassiveEntity, Encoding, IEnumerable, Artifact (+1 more)

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - ".GenerateSampleData"
Cohesion: 0.12
Nodes (9): bool, ReadOnlyMemory, ArchiveItem, Fact, ArchiveItemTests, Fact, MemoryMappedFile, string (+1 more)

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.11
Nodes (18): AnimationObjectLayout, AnimationReplacement, AnimationReplacement, NewModelAnimationTrack, BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4 (+10 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.09
Nodes (18): bool, Dictionary, EditableResearch, Action, bool, IEnumerable, ILogger, ObservableCollection (+10 more)

### Community 48 - "IArchiveItem"
Cohesion: 0.16
Nodes (6): ReadOnlyMemory, IArchiveItem, HashSet, TextFlagService, ArchiveItemViewModel, IComparable

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (17): FileFlags, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo, Encoding (+9 more)

### Community 51 - "CanonicalMeshAuthoringTests"
Cohesion: 0.07
Nodes (21): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, Fact, IEnumerable (+13 more)

### Community 52 - "PropertyEditorViewModel"
Cohesion: 0.14
Nodes (16): Action, IEnumerable, IPropertyEditorFactory, Action, HashSet, IEnumerable, ILogger, Type (+8 more)

### Community 53 - ".Create"
Cohesion: 0.21
Nodes (6): AnimationLengths, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 54 - "IReadOnlyList"
Cohesion: 0.09
Nodes (30): Discarded, PartitionMatch, ImportPlanException, IReadOnlyList, NativeProjectionFingerprint, ByteArrayComparer, GeometryPartition, MalformedMetadataException (+22 more)

### Community 55 - "IReadOnlyList"
Cohesion: 0.09
Nodes (21): Action, BinaryWriter, IDictionary, IReadOnlyCollection, IReadOnlyDictionary, IReadOnlyList, ISet, List (+13 more)

### Community 56 - "Entity"
Cohesion: 0.14
Nodes (13): EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List, ValidationResult, IEnumerable, ILogger (+5 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - ".RewriteStatic"
Cohesion: 0.20
Nodes (8): CanonicalStaticVertex, CanonicalTriangle, StaticRenderObjectAddition, IEnumerable, IReadOnlyDictionary, IReadOnlyList, ISet, RewrittenStaticRecord

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - "GltfCliReportOperation"
Cohesion: 0.13
Nodes (15): GltfExportReceipt, GltfMetadataLineageDisposition, NativeProjectionFingerprint, Guid, int, IReadOnlyList, string, Utf8JsonWriter (+7 more)

### Community 62 - "InteractableEntity"
Cohesion: 0.09
Nodes (14): ShadowType, Encoding, IEnumerable, InteractableEntity, Encoding, IEnumerable, TypedEntity, Encoding (+6 more)

### Community 63 - "DestructibleEntity"
Cohesion: 0.06
Nodes (21): ExplosionFlags, StandType, StoreableFlags, WasteSize, Encoding, IEnumerable, DestructibleEntity, BuilderLine (+13 more)

### Community 64 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.08
Nodes (19): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, EarthTool.Common.GUI.Views (+11 more)

### Community 65 - "MeshAssetAuthoring.cs"
Cohesion: 0.21
Nodes (7): StaticRenderObjectFlags, ICollection, CanonicalStaticObjectRole, MshEditResult, PreservationChange, PreservationDisposition, PreservationReport

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "MetadataEnvelope"
Cohesion: 0.12
Nodes (23): Guid, Projection, Version, MetadataConflictException, MetadataEnvelope, bool, GltfOperationProfile, IEnumerable (+15 more)

### Community 68 - "EditableEntity"
Cohesion: 0.19
Nodes (8): EntityClassType, bool, Dictionary, EditableEntity, ObservableCollection, EntityListItemViewModel, NewValue, OldValue

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "official-corpus-qualification.mjs"
Cohesion: 0.14
Nodes (27): assertPrivacySafe(), binaryStages, buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames(), currentPlatform(), fail() (+19 more)

### Community 72 - "Fact"
Cohesion: 0.11
Nodes (3): Fact, JsonDocument, JsonElement

### Community 73 - ".RunAsync"
Cohesion: 0.08
Nodes (25): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+17 more)

### Community 74 - "Runner"
Cohesion: 0.10
Nodes (17): ChannelReader, ChannelWriter, Dictionary, Guid, int, long, object, string (+9 more)

### Community 75 - "AuthoringValidation"
Cohesion: 0.10
Nodes (15): Guid, HashSet, IReadOnlyList, List, Vector2, Vector3, AuthoringValidation, CanonicalHorizontalExtents (+7 more)

### Community 76 - "IReadOnlyList"
Cohesion: 0.60
Nodes (4): IReadOnlyList, MshDecodeResult, StaticHierarchy, StaticSourceBuilder

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.05
Nodes (26): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Services, EarthTool.Common.Models, EarthTool.WD.Services (+18 more)

### Community 79 - "TreeItemViewModel"
Cohesion: 0.12
Nodes (11): DateTime, int, long, string, ArchiveInfoViewModel, HashSet, bool, Guid (+3 more)

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - "EarthTool.TEX"
Cohesion: 0.10
Nodes (18): EarthTool.TEX, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, IServiceCollection, HostExtensions, IEnumerable, TexHeader, TexImage (+10 more)

### Community 82 - "StaticMeshAsset"
Cohesion: 0.13
Nodes (13): AnimationLayout, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, Utf8JsonWriter, GltfPackage, StaticSourceObjectTraversal, StaticMeshAsset (+5 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.08
Nodes (24): Animations, Blender 4.5 glTF round-trip research, Conclusion, Decision consequences for later tickets, Diagnostic asset, EarthTool metadata requirements, Evidence model, Extras and custom properties (+16 more)

### Community 84 - "MshCanonicalSerializer"
Cohesion: 0.15
Nodes (8): CanonicalStaticRecord, AnimationClassBytes, Encoding, Guid, int, List, Matrix4x4, MshCanonicalSerializer

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "ConvertCommand"
Cohesion: 0.23
Nodes (9): CommonCommand, CommonSettings, IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, Settings (+1 more)

### Community 88 - "ParsedGltfAnimationChannel"
Cohesion: 0.29
Nodes (5): int, string, ParsedAnimationBuilder, ParsedGltfAnimationChannel, float

### Community 89 - "FlagsPropertyEditorViewModel"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "IUndoRedoService"
Cohesion: 0.06
Nodes (23): Action, DateTime, UndoAction, Action, IEnumerable, IUndoRedoService, Action, IEnumerable (+15 more)

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "GltfInterchange"
Cohesion: 0.07
Nodes (14): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, GltfEditImportOptions, GltfMetadataConflictResolution, IEnumerable, JsonNode, ReadOnlySpan (+6 more)

### Community 95 - "MshOperationProfile"
Cohesion: 0.16
Nodes (10): DecodedStaticRecord, Guid, IEnumerable, WalkingSkeletonConsumer, Guid, MeshAssetLineageId, IEnumerable, MshExpert (+2 more)

### Community 96 - "ViewLocator"
Cohesion: 0.12
Nodes (9): EarthTool.TEX.GUI, Control, ViewLocator, AppBuilder, STAThread, Program, Control, ViewLocator (+1 more)

### Community 97 - "KhronosValidatorServer"
Cohesion: 0.20
Nodes (8): List, KhronosValidatorServer, ValidatorResult, IAsyncDisposable, Process, ValidatorCode, ValidatorResult, ValueTask

### Community 98 - "EarthTool Suite"
Cohesion: 0.22
Nodes (10): EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management, MSH Model Export Workflow (+2 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "VerticalTransporter"
Cohesion: 0.09
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 101 - "EquipableEntity"
Cohesion: 0.14
Nodes (8): MaxShieldUpgradeType, PositionType, IEnumerable, EquipableEntity, Platoon, Encoding, IEnumerable, StartingPosition

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

### Community 106 - "EntityConverter"
Cohesion: 0.20
Nodes (8): EarthTool.PAR.Models.Serialization, JsonSerializerOptions, Type, Utf8JsonWriter, EntityConverter, TypeReader, JsonConverter, Utf8JsonReader

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - "IDialogService"
Cohesion: 0.24
Nodes (3): IEnumerable, Task, IDialogService

### Community 109 - "TexPreviewLoader"
Cohesion: 0.07
Nodes (29): Ambiguous, BinaryReader, byte, CancellationToken, Exception, GltfExportOptions, GltfOperationProfile, ICollection (+21 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.16
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

### Community 116 - "EnumPropertyEditorViewModel"
Cohesion: 0.29
Nodes (5): object, ObservableCollection, Type, EnumPropertyEditorViewModel, EnumValueViewModel

### Community 117 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 118 - "EntityFactoryTests.cs"
Cohesion: 0.29
Nodes (4): EarthTool.PAR.Tests.Factories, Encoding, Fact, EntityFactoryTests

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - ".CollectNewModelAnimationPaths"
Cohesion: 0.33
Nodes (4): ICollection, Path, NewModelAnimationSet, NodeIndex

### Community 122 - ".ExportGlbAsync"
Cohesion: 0.15
Nodes (4): JsonNode, Action, Guid, OneTriangleMshFixture

### Community 123 - "EarthTool.TEX.Tests"
Cohesion: 0.29
Nodes (7): EarthTool.TEX.Tests, net8.0, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 124 - "EarthTool"
Cohesion: 0.29
Nodes (7): EarthTool, EarthTool.CLI, EarthTool.DAE, EarthTool.MSH, EarthTool.PAR, EarthTool.PAR.GUI, EarthTool.TEX

### Community 125 - "Static Light"
Cohesion: 0.33
Nodes (6): Active Static Light, Light Attachment, Light Parameters, Spot Heading, Spot Shape Values, Static Light

### Community 126 - "GltfCommandSettings.cs"
Cohesion: 0.22
Nodes (13): AsyncCommand, CancellationToken, CommandContext, Task, ExportGltfCommand, ImportEditGltfCommand, ImportNewGltfCommand, Guid (+5 more)

### Community 127 - "ParFile"
Cohesion: 0.12
Nodes (15): Reader, FileType, IReader, ILogger, Task, ParFileService, Encoding, IEnumerable (+7 more)

### Community 128 - "ViewLocator"
Cohesion: 0.18
Nodes (6): EarthTool.WD.GUI, AppBuilder, STAThread, Program, Control, ViewLocator

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "MappedArchiveDataSource"
Cohesion: 0.10
Nodes (14): ReadOnlyMemory, IArchiveDataSource, ReadOnlyMemory, InMemoryArchiveDataSource, int, MemoryMappedFile, ReadOnlyMemory, MappedArchiveDataSource (+6 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - "StaticRenderObject"
Cohesion: 0.25
Nodes (7): Matrix4x4, Vector3, StaticAnimationTracks, StaticRenderObject, StaticAnimationReplacement, ReadOnlySpan, Vector3

### Community 134 - ".RoundTripAsync"
Cohesion: 0.19
Nodes (11): CancellationToken, Stream, Task, CancellationToken, Stream, string, Task, IMshReader (+3 more)

### Community 135 - "ConvertCommand"
Cohesion: 0.14
Nodes (13): IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+5 more)

### Community 136 - "Dependabot Dependency Automation"
Cohesion: 0.50
Nodes (4): Dependabot Dependency Automation, Weekly GitHub Actions Updates, Weekly NuGet Updates, Security Check Job

### Community 137 - "ITransactionalFileSystem"
Cohesion: 0.19
Nodes (3): Stream, ITransactionalFileSystem, TransactionalFileSystem

### Community 138 - "Setup .NET Environment"
Cohesion: 0.67
Nodes (3): .NET SDK Setup, NuGet Package Cache, Setup .NET Environment

### Community 139 - "Mesh Attachments 1..49"
Cohesion: 0.67
Nodes (3): Trailing Hierarchy Unwind Count, Mesh Attachments 1..49, Mesh Extents

### Community 148 - ".Write_And_Read_AreSymmetric"
Cohesion: 0.19
Nodes (7): Writer, Fact, ParameterReaderTests, Fact, ParameterWriterTests, Encoding, ParTestData

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "IDisposable"
Cohesion: 0.09
Nodes (15): CommandSettings, EarthTool.CLI.Commands, Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, Type (+7 more)

### Community 152 - "GltfContracts.cs"
Cohesion: 0.10
Nodes (22): Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfAnimationHandle, GltfDiagnosticCodes, GltfLightHandle, GltfMaterialHandle (+14 more)

### Community 153 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 154 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 156 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 158 - "ArchiverServiceTests"
Cohesion: 0.05
Nodes (41): Command, EarthTool.CLI.Commands.WD, CancellationToken, CommandContext, AddCommand, CancellationToken, CommandContext, CreateCommand (+33 more)

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - "ItemCommand"
Cohesion: 0.31
Nodes (5): CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Measured Result, Official MSH Qualification Performance, Stage Profiling, Aggregate release qualification, Blender matrix, Official MSH corpus

### Community 167 - ".WriteReportAsync"
Cohesion: 0.17
Nodes (4): Stream, CliReportFileSystem, ICliReportFileSystem, Exception

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 171 - "EarthTool.PAR.GUI"
Cohesion: 0.29
Nodes (4): EarthTool.PAR.GUI, AppBuilder, STAThread, Program

### Community 172 - "Migrate From COLLADA To glTF"
Cohesion: 0.25
Nodes (6): glTF API, API migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 173 - "PublicCutoverAcceptanceTests"
Cohesion: 0.22
Nodes (7): CliResult, Fact, Task, CliResult, PublicCutoverAcceptanceTests, GeneratedRegex, Regex

### Community 178 - "MainWindow"
Cohesion: 0.16
Nodes (8): Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs, RoutedEventArgs, Window

### Community 179 - "GltfInterchange.cs"
Cohesion: 0.48
Nodes (6): Exception, AmbiguousPartitionCorrespondenceException, MetadataIdentityException, ResourceLimitException, StaleNativeProjectionException, StaticLightMetadataException

### Community 182 - "EarthTool.PAR.Enums"
Cohesion: 0.07
Nodes (20): EarthTool.PAR.Enums, BarrelBetaType, BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, DamageFlags, HitType (+12 more)

### Community 187 - "EarthTool.Common"
Cohesion: 0.09
Nodes (14): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.Common, EarthTool.CLI, EarthTool.PAR.Tests.Services, EarthTool.PAR.Tests.Models (+6 more)

### Community 189 - ".Create"
Cohesion: 0.16
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

## Knowledge Gaps
- **317 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+312 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `ConvertCommand`, `IValueConverter`, `IEarthInfo`, `EarthTool.PAR.Models`, `IDisposable`, `EarthTool.Common`, `EarthTool.MSH.Assets`, `ParFile`?**
  _High betweenness centrality (0.169) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `.RunAsync` to `IDisposable`?**
  _High betweenness centrality (0.113) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `MeshAssetAuthoring.cs`, `MetadataEnvelope`, `.RoundTripAsync`, `TexPreviewLoader`, `StaticAnimationProjection`, `CanonicalDynamicObject`, `GltfInterchange.cs`, `MeshAsset`, `IReadOnlyList`, `GltfContracts.cs`, `GltfCliReportOperation`?**
  _High betweenness centrality (0.107) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _317 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.11149825783972125 - nodes in this community are weakly interconnected._
- **Should `ParsedGlb` be split into smaller, more focused modules?**
  _Cohesion score 0.14126984126984127 - nodes in this community are weakly interconnected._