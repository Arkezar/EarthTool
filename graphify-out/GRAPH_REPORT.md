# Graph Report - EarthTool  (2026-08-03)

## Corpus Check
- 360 files · ~262,792 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4374 nodes · 12549 edges · 188 communities (180 shown, 8 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 785 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `ad605974`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- MeshAsset
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- GltfPlanAndReportTests
- Task
- .ToByteArray
- EarthTool.Common.GUI.Enums
- IValueConverter
- MainWindowViewModel
- OperationResult
- GltfImportPlanSerializer
- .CreateStaticLightGuards
- MshOperationProfile
- .OpenArchive
- CanonicalDynamicObject
- Dynamic MESH Binary Layout
- Equipment
- DynamicMeshAsset
- release-qualification.mjs
- MainWindowViewModel
- EarthTool.PAR.GUI.ViewModels
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
- EarthTool.Common
- DynamicGltfDocument
- StaticMeshEditSession
- StaticMeshAssetTests
- .ImportCore
- ArchiverService
- ResearchReferenceCollectionEditorViewModel
- EarthTool.PAR.Enums
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
- OfficialCorpusQualification
- RenderVertex
- GltfInterchange
- Entity
- EarthTool.MSH.Tests
- IUndoRedoService
- DialogService
- EarthTool.PAR
- IReadOnlyList
- InteractableEntity
- DestructibleEntity
- EarthTool.Common.GUI.ViewModels
- MeshAssetAuthoring.cs
- EarthTool.PAR.GUI
- .DetectStaleGuards
- EditableEntity
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- official-corpus-qualification.mjs
- GltfWalkingSkeletonTests
- .RunAsync
- IExtractor
- AuthoringValidation
- StaticSourceObject
- EarthTool.sln
- EarthTool.Common.Interfaces
- ArchiveInfoViewModel
- BinaryExtensions
- .Create
- StaticMeshAsset
- Blender 4.5 glTF round-trip research
- MshCanonicalSerializer
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- TexFile
- MetadataEnvelope
- FlagsPropertyEditorViewModel
- EarthTool WD Archive Manager
- UndoRedoService
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- GltfCommandSettings.cs
- Runner
- ViewLocator
- OperationDiagnostic
- EarthTool Suite
- WD Central Directory
- VerticalTransporter
- ArchiverServiceTests
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- WorkerContext
- Reader
- StaticMeshSequenceFixture
- TexPreviewLoader
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- EnumPropertyEditorViewModel
- CommonCommand
- .WriteReportAsync
- EarthTool.TEX
- MeshAssetLineageId
- .RewriteJson
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- ParFile
- Archive
- Base Header
- ArchiveItem
- UnitTest1.cs
- EarthTool Installation Guide
- .RoundTripAsync
- PublicCutoverAcceptanceTests
- ConvertCommand
- Dependabot Dependency Automation
- ConvertCommand
- Setup .NET Environment
- Mesh Attachments 1..49
- StaticMeshAsset.cs
- Code Quality Analysis Job
- Dynamic Color
- ITransactionalFileSystem
- QualificationProfiler
- .EditImportSamplesCubicSplineWithoutPreservingTangents
- package.json
- CommandTypeRegistrar
- ParsedGlb
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- App
- ParameterReader
- .ToByteArray
- EarthTool.CLI.Commands.WD
- GltfContracts.cs
- validate-glb.mjs
- .Write_And_Read_AreSymmetric
- InMemoryArchiveDataSourceTests
- Official MSH Qualification Performance
- OneTriangleMshFixture
- IDialogService
- .ValidateManifestInventory
- EarthTool.CLI.Tests
- Research
- ParsedGltfAnimationChannel
- Migrate From COLLADA To glTF
- TreeItemViewModel
- .CreateStatic
- EarthTool.CLI.Commands.MSH
- .Reconcile
- ItemCommand
- MainWindow
- GltfInterchange.cs
- GltfPlanAndReport.cs
- ParameterReaderTests
- EquipableEntity
- ITexFile
- EarthTool.GLTF/HostExtensions.cs
- MshDiagnosticCodes
- Missile
- .Create

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 179 edges
2. `GltfInterchange` - 173 edges
3. `GlbDocument` - 138 edges
4. `DynamicGltfDocument` - 90 edges
5. `EarthTool.PAR.Enums` - 90 edges
6. `OperationDiagnostic` - 86 edges
7. `OperationResult` - 79 edges
8. `MetadataGraphValidationTests` - 77 edges
9. `StaticMeshAsset` - 75 edges
10. `EarthTool.PAR.Models` - 64 edges

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

## Communities (188 total, 8 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - "MeshAsset"
Cohesion: 0.19
Nodes (12): byte, MeshAsset, MeshAssetKind, CancellationToken, Exception, IEnumerable, ILogger, Stream (+4 more)

### Community 2 - "AssetResult"
Cohesion: 0.26
Nodes (6): AssetResult, DiagnosticKey, AssetResult, OperationCounts, ProfileScope, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.11
Nodes (15): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+7 more)

### Community 5 - "GltfPlanAndReportTests"
Cohesion: 0.13
Nodes (14): BufferPath, ConflictKey, Directory, Fact, Guid, InlineData, JsonNode, Task (+6 more)

### Community 6 - "Task"
Cohesion: 0.10
Nodes (6): BlenderOutputEvidence, Fact, IEnumerable, Task, Trait, Action

### Community 7 - ".ToByteArray"
Cohesion: 0.07
Nodes (23): Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding, IEnumerable (+15 more)

### Community 8 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.10
Nodes (15): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.Views, NotificationType, Exception, NotificationEventArgs, IServiceCollection (+7 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (8): bool, ILogger, ObservableCollection, ReactiveCommand, string, Task, Unit, MainWindowViewModel

### Community 11 - "OperationResult"
Cohesion: 0.13
Nodes (16): IReadOnlyList, OperationResult, GltfEditImportOptions, GltfEditImportResult, GltfMeshEditImportResult, GltfMetadataConflictResolution, GltfNewModelImportResult, GltfOperationProfile (+8 more)

### Community 12 - "GltfImportPlanSerializer"
Cohesion: 0.13
Nodes (11): CancellationToken, IReadOnlyDictionary, JsonElement, SeparateGltfPackage, Stream, Task, GltfImportPlan, GltfImportPlanKind (+3 more)

### Community 13 - ".CreateStaticLightGuards"
Cohesion: 0.18
Nodes (7): Action, BinaryWriter, Quaternion, Vector3, ProjectedAttachment, ProjectedCannonRenderPosition, ProjectedStaticLight

### Community 14 - "MshOperationProfile"
Cohesion: 0.10
Nodes (22): DecodedStaticRecord, MeshAssetOrigin, CancellationToken, Guid, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+14 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "CanonicalDynamicObject"
Cohesion: 0.06
Nodes (40): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, ReadOnlySpan, DynamicAlphaTiming (+32 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "Equipment"
Cohesion: 0.09
Nodes (19): ConnectorType, LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable (+11 more)

### Community 19 - "DynamicMeshAsset"
Cohesion: 0.22
Nodes (7): ICollection, IEnumerable, DynamicObjectScope, Action, Func, DynamicMeshAsset, DynamicObject

### Community 20 - "release-qualification.mjs"
Cohesion: 0.15
Nodes (29): buildEvidence(), collectReceivedFiles(), countDiscoveredTests(), exists(), expectedArtifacts, expectedTestCounts, fail(), forbiddenReleasePaths (+21 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.13
Nodes (9): bool, ILogger, object, ObservableCollection, ReactiveCommand, string, Task, Unit (+1 more)

### Community 22 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.04
Nodes (35): EarthTool.PAR.GUI, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, EntityClassType, Faction, ResearchType (+27 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.12
Nodes (13): Bitmap, INotificationService, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap (+5 more)

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
Cohesion: 0.20
Nodes (5): IEarthInfoFactory, Fact, ArchiveTests, Guid, TestDataGenerator

### Community 29 - "EarthTool.MSH.Assets"
Cohesion: 0.21
Nodes (12): EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.Consumer.Tests, EarthTool.MSH.Tests, EarthTool.GLTF.Internal (+4 more)

### Community 30 - "GlbDocument"
Cohesion: 0.09
Nodes (10): IDictionary, JsonDocument, JsonElement, Matrix4x4, ReadOnlySpan, uint, GlbDocument, GltfImportIntent (+2 more)

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
Cohesion: 0.13
Nodes (15): DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid, DateTime, Guid (+7 more)

### Community 35 - "EarthTool.Common"
Cohesion: 0.07
Nodes (17): EarthTool.TEX, EarthTool.PAR, EarthTool.Common, EarthTool.CLI, EarthTool.Common.Factories, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, EarthTool.CLI.Commands (+9 more)

### Community 36 - "DynamicGltfDocument"
Cohesion: 0.10
Nodes (19): DynamicEditedPreview, BinaryWriter, float, int, ReadOnlySpan, Stream, string, uint (+11 more)

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.13
Nodes (11): StaticRenderObjectId, bool, Dictionary, ICollection, IEnumerable, int, Matrix4x4, Vector3 (+3 more)

### Community 38 - "StaticMeshAssetTests"
Cohesion: 0.20
Nodes (6): Fact, IEnumerable, InlineData, Task, Theory, StaticMeshAssetTests

### Community 39 - ".ImportCore"
Cohesion: 0.15
Nodes (8): CancellationToken, GltfOperationProfile, ISet, JsonDocument, JsonElement, ReadOnlyMemory, DynamicGltfImport, DynamicMetadataGraphException

### Community 40 - "ArchiverService"
Cohesion: 0.12
Nodes (11): IArchiveFactory, ReadOnlySpan, Stream, IDecompressor, PathValidator, Encoding, ILogger, ArchiverService (+3 more)

### Community 41 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.20
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 42 - "EarthTool.PAR.Enums"
Cohesion: 0.09
Nodes (10): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.Extensions, EarthTool.PAR.Tests.TestData, EarthTool.PAR.Tests.Services, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Tests.Factories, EarthTool.PAR.Tests.Models (+2 more)

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - ".GenerateSampleData"
Cohesion: 0.13
Nodes (6): Fact, ArchiveItemTests, Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.14
Nodes (15): AnimationObjectLayout, BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionFingerprint (+7 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.14
Nodes (11): Action, bool, IEnumerable, ILogger, ObservableCollection, ReactiveCommand, string, Type (+3 more)

### Community 48 - "IArchiveItem"
Cohesion: 0.10
Nodes (11): Type, CommandTypeResolver, ReadOnlyMemory, IArchiveItem, ITextFlagService, HashSet, TextFlagService, IComparable (+3 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (20): FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo (+12 more)

### Community 51 - "CanonicalMeshAuthoringTests"
Cohesion: 0.20
Nodes (8): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, IEnumerable, IEnumerator

### Community 52 - "PropertyEditorViewModel"
Cohesion: 0.14
Nodes (16): Action, IEnumerable, IPropertyEditorFactory, Action, HashSet, IEnumerable, ILogger, Type (+8 more)

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.17
Nodes (10): ContentFingerprint, BinaryWriter, IEnumerable, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification (+2 more)

### Community 54 - "RenderVertex"
Cohesion: 0.13
Nodes (15): AnimationLayout, PartitionMatch, MemoryStream, NativeProjectionFingerprint, ByteArrayComparer, GeometryPartition, PartitionLayout, ProjectedPartition (+7 more)

### Community 55 - "GltfInterchange"
Cohesion: 0.07
Nodes (29): AnimationReplacement, Discarded, Action, BinaryWriter, IDictionary, IReadOnlyCollection, IReadOnlyDictionary, IReadOnlyList (+21 more)

### Community 56 - "Entity"
Cohesion: 0.07
Nodes (25): EarthTool.PAR.Models.Serialization, Encoding, IBinarySerializable, EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List (+17 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "IUndoRedoService"
Cohesion: 0.11
Nodes (13): Action, IUndoRedoService, IEnumerable, string, IntCollectionPropertyEditorViewModel, int, string, IntPropertyEditorViewModel (+5 more)

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - "IReadOnlyList"
Cohesion: 0.19
Nodes (9): DynamicEffectPreview, DynamicImageLayout, DynamicMeshLayout, DynamicObjectScope, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList, NativeProjectionFingerprint (+1 more)

### Community 62 - "InteractableEntity"
Cohesion: 0.08
Nodes (19): BarrelBetaType, ShadowType, TargetType, WeaponFireType, Encoding, IEnumerable, InteractableEntity, Encoding (+11 more)

### Community 63 - "DestructibleEntity"
Cohesion: 0.05
Nodes (30): ArtifactType, ExplosionFlags, PassiveMask, StandType, StoreableFlags, WasteSize, Encoding, IEnumerable (+22 more)

### Community 64 - "EarthTool.Common.GUI.ViewModels"
Cohesion: 0.08
Nodes (16): EarthTool.TEX.GUI, EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, EarthTool.Common.GUI, ReactiveCommand, Unit, AboutViewModel, ViewModelBase (+8 more)

### Community 65 - "MeshAssetAuthoring.cs"
Cohesion: 0.22
Nodes (7): IReadOnlyList, CanonicalStaticRenderObject, CanonicalStaticVertex, CanonicalTriangle, MshEditResult, StaticLightRecordKind, StaticRenderObjectAddition

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - ".DetectStaleGuards"
Cohesion: 0.15
Nodes (18): MetadataConflictException, bool, GltfOperationProfile, IEnumerable, int, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList (+10 more)

### Community 68 - "EditableEntity"
Cohesion: 0.12
Nodes (12): bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, bool, FlagValueViewModel (+4 more)

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "official-corpus-qualification.mjs"
Cohesion: 0.14
Nodes (27): assertPrivacySafe(), binaryStages, buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames(), currentPlatform(), fail() (+19 more)

### Community 72 - "GltfWalkingSkeletonTests"
Cohesion: 0.10
Nodes (8): Guid, InlineData, JsonDocument, JsonElement, Theory, BlenderOutputEvidence, GltfWalkingSkeletonTests, JsonArray

### Community 73 - ".RunAsync"
Cohesion: 0.07
Nodes (27): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+19 more)

### Community 74 - "IExtractor"
Cohesion: 0.33
Nodes (3): Task, IExtractor, IWDExtractor

### Community 75 - "AuthoringValidation"
Cohesion: 0.13
Nodes (9): HashSet, List, Vector2, AuthoringValidation, CanonicalHorizontalExtents, CanonicalStaticFootprint, CanonicalStaticSourceObject, StaticMeshBuilder (+1 more)

### Community 76 - "StaticSourceObject"
Cohesion: 0.14
Nodes (7): SourceObjectId, StaticSourceObject, IDictionary, RewrittenStaticRecord, StaticSourceBuilder, NewModelAnimationSet, NewModelSourceDraft

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.06
Nodes (21): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Factories, EarthTool.PAR.Services, EarthTool.WD.Tests.Services, EarthTool.Common.Models (+13 more)

### Community 79 - "ArchiveInfoViewModel"
Cohesion: 0.18
Nodes (7): DateTime, int, long, string, ArchiveInfoViewModel, ArchiveItemViewModel, ViewModelBase

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - ".Create"
Cohesion: 0.21
Nodes (6): AnimationLengths, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 82 - "StaticMeshAsset"
Cohesion: 0.15
Nodes (10): IEnumerable, InterchangeBaseline, IReadOnlyDictionary, Utf8JsonWriter, GltfPackage, StaticMeshAsset, PartitionLayout, ProjectedAttachment (+2 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.08
Nodes (24): Animations, Blender 4.5 glTF round-trip research, Conclusion, Decision consequences for later tickets, Diagnostic asset, EarthTool metadata requirements, Evidence model, Extras and custom properties (+16 more)

### Community 84 - "MshCanonicalSerializer"
Cohesion: 0.11
Nodes (14): StaticAnimationReplacement, Encoding, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList, ISet, Matrix4x4 (+6 more)

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "TexFile"
Cohesion: 0.22
Nodes (8): BinaryReader, IEnumerable, TexFile, TexHeader, BinaryReader, IEnumerable, SKBitmap, TexImage

### Community 88 - "MetadataEnvelope"
Cohesion: 0.10
Nodes (24): ImportPlanException, DynamicMetadataIdentityException, DynamicPreviewException, Guid, IReadOnlyList, Projection, Version, MalformedMetadataException (+16 more)

### Community 89 - "FlagsPropertyEditorViewModel"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "UndoRedoService"
Cohesion: 0.12
Nodes (10): Action, DateTime, UndoAction, IEnumerable, Action, IEnumerable, ILogger, int (+2 more)

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "GltfCommandSettings.cs"
Cohesion: 0.22
Nodes (13): AsyncCommand, CancellationToken, CommandContext, Task, ExportGltfCommand, ImportEditGltfCommand, ImportNewGltfCommand, Guid (+5 more)

### Community 95 - "Runner"
Cohesion: 0.21
Nodes (6): ChannelReader, ChannelWriter, Guid, Task, Runner, ValidatorAggregate

### Community 96 - "ViewLocator"
Cohesion: 0.12
Nodes (9): EarthTool.WD.GUI, Control, ViewLocator, AppBuilder, STAThread, Program, Control, ViewLocator (+1 more)

### Community 97 - "OperationDiagnostic"
Cohesion: 0.15
Nodes (5): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, IEnumerable, ParseScopeResolution

### Community 98 - "EarthTool Suite"
Cohesion: 0.20
Nodes (11): EarthTool.DAE, EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management (+3 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "VerticalTransporter"
Cohesion: 0.12
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 101 - "ArchiverServiceTests"
Cohesion: 0.17
Nodes (8): CancellationToken, CommandContext, DateTime, Guid, IArchiver, Fact, string, ArchiverServiceTests

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

### Community 106 - "WorkerContext"
Cohesion: 0.17
Nodes (10): List, KhronosValidatorServer, ValidatorResult, WorkerContext, IAsyncDisposable, KhronosValidatorServer, Process, ValidatorCode (+2 more)

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - "StaticMeshSequenceFixture"
Cohesion: 0.21
Nodes (7): int, IReadOnlyList, Matrix4x4, Vector3, Record, StaticMeshSequenceFixture, Record

### Community 109 - "TexPreviewLoader"
Cohesion: 0.07
Nodes (30): Ambiguous, BinaryReader, byte, CancellationToken, Exception, GltfExportOptions, GltfOperationProfile, ICollection (+22 more)

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

### Community 118 - ".WriteReportAsync"
Cohesion: 0.17
Nodes (4): Stream, CliReportFileSystem, ICliReportFileSystem, Exception

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "MeshAssetLineageId"
Cohesion: 0.23
Nodes (8): Guid, IEnumerable, WalkingSkeletonConsumer, Guid, MeshAssetLineageId, MshBuildResult, IEnumerable, MshExpert

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
Cohesion: 0.20
Nodes (9): CliProcessResult, CliReportOperation, IReadOnlyList, Task, CliDiagnostic, CliOracleResult, CliProcessResult, CliReportOperation (+1 more)

### Community 127 - "ParFile"
Cohesion: 0.15
Nodes (10): Task, IParFileService, ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile (+2 more)

### Community 128 - "Archive"
Cohesion: 0.17
Nodes (9): Stream, ICompressor, bool, DateTime, Encoding, IReadOnlyCollection, MemoryMappedFile, Archive (+1 more)

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "ArchiveItem"
Cohesion: 0.11
Nodes (15): ReadOnlyMemory, IArchiveDataSource, bool, ReadOnlyMemory, ArchiveItem, ReadOnlyMemory, InMemoryArchiveDataSource, int (+7 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - ".RoundTripAsync"
Cohesion: 0.23
Nodes (9): CancellationToken, Stream, Task, CancellationToken, Stream, Task, IMshReader, IMshValidator (+1 more)

### Community 134 - "PublicCutoverAcceptanceTests"
Cohesion: 0.22
Nodes (7): CliResult, Fact, Task, CliResult, PublicCutoverAcceptanceTests, GeneratedRegex, Regex

### Community 135 - "ConvertCommand"
Cohesion: 0.20
Nodes (10): CommonCommand, CommonSettings, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+2 more)

### Community 136 - "Dependabot Dependency Automation"
Cohesion: 0.50
Nodes (4): Dependabot Dependency Automation, Weekly GitHub Actions Updates, Weekly NuGet Updates, Security Check Job

### Community 137 - "ConvertCommand"
Cohesion: 0.27
Nodes (7): IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, IReader, Settings

### Community 138 - "Setup .NET Environment"
Cohesion: 0.67
Nodes (3): .NET SDK Setup, NuGet Package Cache, Setup .NET Environment

### Community 139 - "Mesh Attachments 1..49"
Cohesion: 0.67
Nodes (3): Trailing Hierarchy Unwind Count, Mesh Attachments 1..49, Mesh Extents

### Community 140 - "StaticMeshAsset.cs"
Cohesion: 0.25
Nodes (10): IReadOnlyList, Matrix4x4, Vector3, AnimationClassBytes, CommonMeshBaseHeader, StaticAnimationClass, StaticAnimationTracks, StaticRenderObject (+2 more)

### Community 143 - "ITransactionalFileSystem"
Cohesion: 0.19
Nodes (3): Stream, ITransactionalFileSystem, TransactionalFileSystem

### Community 148 - "QualificationProfiler"
Cohesion: 0.20
Nodes (11): Dictionary, int, long, object, string, ProfileScope, QualificationProfiler, TimingAggregate (+3 more)

### Community 149 - ".EditImportSamplesCubicSplineWithoutPreservingTangents"
Cohesion: 0.24
Nodes (4): Action, IReadOnlyList, JsonObject, List

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "CommandTypeRegistrar"
Cohesion: 0.24
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 152 - "ParsedGlb"
Cohesion: 0.08
Nodes (13): GltfAnimationHandle, GltfLightHandle, GltfMaterialHandle, GltfNewModelImportOptions, GltfNodeHandle, ICollection, JsonObject, Path (+5 more)

### Community 153 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 154 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 155 - "ParameterReader"
Cohesion: 0.26
Nodes (6): Reader, FileType, BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 156 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 158 - "EarthTool.CLI.Commands.WD"
Cohesion: 0.06
Nodes (36): Command, CommandSettings, EarthTool.CLI.Commands.PAR, EarthTool.CLI.Commands.WD, ItemSettings, AddCommand, CancellationToken, CommandContext (+28 more)

### Community 159 - "GltfContracts.cs"
Cohesion: 0.06
Nodes (29): Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfDiagnosticCodes, GltfDynamicEditImportResult, GltfExportOptions, GltfExportReceipt (+21 more)

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - ".Write_And_Read_AreSymmetric"
Cohesion: 0.29
Nodes (5): Writer, Fact, ParameterWriterTests, Encoding, ParTestData

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Measured Result, Official MSH Qualification Performance, Stage Profiling, Aggregate release qualification, Blender matrix, Official MSH corpus

### Community 167 - "IDialogService"
Cohesion: 0.24
Nodes (3): IEnumerable, Task, IDialogService

### Community 168 - ".ValidateManifestInventory"
Cohesion: 0.25
Nodes (5): CarrierKind, ICollection, Path, Value, Envelope

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 170 - "Research"
Cohesion: 0.31
Nodes (6): IDictionary, IEnumerable, ParameterEntry, IEnumerable, Research, TreeNode

### Community 171 - "ParsedGltfAnimationChannel"
Cohesion: 0.29
Nodes (5): float, int, string, ParsedAnimationBuilder, ParsedGltfAnimationChannel

### Community 172 - "Migrate From COLLADA To glTF"
Cohesion: 0.22
Nodes (7): Dynamic sprite-effect contract, glTF API, API migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 173 - "TreeItemViewModel"
Cohesion: 0.24
Nodes (5): HashSet, bool, Guid, ObservableCollection, TreeItemViewModel

### Community 174 - ".CreateStatic"
Cohesion: 0.29
Nodes (4): CanonicalStaticRecord, MeshArchiveFraming, Guid, List

### Community 175 - "EarthTool.CLI.Commands.MSH"
Cohesion: 0.25
Nodes (5): EarthTool.CLI.Commands.MSH, EarthTool.MSH, EarthTool.CLI.Tests, IServiceCollection, HostExtensions

### Community 176 - ".Reconcile"
Cohesion: 0.31
Nodes (3): DynamicRecordSlice, IDictionary, NativeObjectGraph

### Community 177 - "ItemCommand"
Cohesion: 0.38
Nodes (4): CancellationToken, CommandContext, IEnumerable, ItemCommand

### Community 178 - "MainWindow"
Cohesion: 0.15
Nodes (9): EarthTool.TEX.GUI.Views, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs, RoutedEventArgs (+1 more)

### Community 179 - "GltfInterchange.cs"
Cohesion: 0.48
Nodes (6): Exception, AmbiguousPartitionCorrespondenceException, MetadataIdentityException, ResourceLimitException, StaleNativeProjectionException, StaticLightMetadataException

### Community 180 - "GltfPlanAndReport.cs"
Cohesion: 0.48
Nodes (6): int, IReadOnlyList, string, GltfCliReport, GltfCliReportFormat, GltfImportPlanFormat

### Community 182 - "EquipableEntity"
Cohesion: 0.07
Nodes (18): BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags, SpaceStationType (+10 more)

### Community 183 - "ITexFile"
Cohesion: 0.33
Nodes (5): IEnumerable, TexHeader, TexImage, ITexFile, TexReader

### Community 187 - "Missile"
Cohesion: 0.13
Nodes (9): DamageFlags, HitType, MissileType, RocketType, Encoding, IEnumerable, Missile, Fact (+1 more)

### Community 189 - ".Create"
Cohesion: 0.17
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

## Knowledge Gaps
- **320 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+315 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `EarthTool.Common`, `IValueConverter`, `EarthTool.CLI.Commands.MSH`, `IEarthInfo`, `ParameterReader`?**
  _High betweenness centrality (0.174) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `.RunAsync` to `IArchiveItem`, `GltfImportPlanSerializer`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **Why does `MainWindowViewModel` connect `MainWindowViewModel` to `EditableEntity`, `IDialogService`, `EntityDetailsViewModel`, `IArchiveItem`, `EarthTool.PAR.GUI.ViewModels`, `MainWindowViewModel`, `Entity`, `IUndoRedoService`, `ParFile`?**
  _High betweenness centrality (0.111) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _320 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10741971207087486 - nodes in this community are weakly interconnected._
- **Should `GltfPlanAndReportTests` be split into smaller, more focused modules?**
  _Cohesion score 0.13076923076923078 - nodes in this community are weakly interconnected._