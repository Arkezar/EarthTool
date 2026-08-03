# Graph Report - EarthTool  (2026-08-03)

## Corpus Check
- 360 files · ~269,958 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4442 nodes · 12966 edges · 184 communities (175 shown, 9 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 827 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c3a7fdf4`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- .WriteFileAsync
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- GltfPlanAndReportTests
- Task
- .ToByteArray
- EarthTool.Common.GUI.ViewModels
- IValueConverter
- MainWindowViewModel
- OperationResult
- DynamicGltfInterchangeTests
- .CreateStaticLightGuards
- MshOperationProfile
- .OpenArchive
- DynamicEffectExtension
- Dynamic MESH Binary Layout
- DynamicGltfDocument
- MeshAsset
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
- GltfImportPlanSerializer
- EarthTool.Common
- IReadOnlyList
- StaticMeshEditSession
- Equipment
- JsonElement
- IArchive
- .GenerateSampleData
- EarthTool.PAR.Enums
- Static Mesh Header
- StaticObject Record
- .DeserializeAsync
- StaticAnimationProjection
- EntityDetailsViewModel
- IArchiveItem
- PublicApiApproval
- IEarthInfo
- CanonicalMeshAuthoringTests
- PropertyEditorFactory
- OfficialCorpusQualification
- IReadOnlyList
- IReadOnlyList
- Entity
- EarthTool.MSH.Tests
- IUndoRedoService
- DialogService
- EarthTool.PAR
- .CreateJson
- InteractableEntity
- DestructibleEntity
- INotificationService
- GltfCliReportSerializer
- EarthTool.PAR.GUI
- MetadataEnvelope
- EditableEntity
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- official-corpus-qualification.mjs
- GltfWalkingSkeletonTests
- .RunAsync
- GltfCommandSettings.cs
- MeshAssetAuthoring.cs
- .WriteReportAsync
- EarthTool.sln
- EarthTool.Common.Interfaces
- TreeItemViewModel
- BinaryExtensions
- .Create
- StaticMeshAsset
- Blender 4.5 glTF round-trip research
- MshCanonicalSerializer
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- EarthTool.TEX
- Program
- ResearchReferenceCollectionEditorViewModel
- EarthTool WD Archive Manager
- UndoRedoService
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- TexPreview
- Runner
- ViewLocator
- GltfInterchange
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
- TexPreviewLoader
- .Decode
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- EnumPropertyEditorViewModel
- PublicCutoverAcceptanceTests
- .ReadRibbonPreview
- PassiveEntity
- EarthTool.TEX
- MeshAssetLineageId
- .ExportGlbAsync
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- ParFile
- PropertyEditorViewModel
- Base Header
- MappedArchiveDataSource
- UnitTest1.cs
- EarthTool Installation Guide
- TexFile
- .Write_And_Read_AreSymmetric
- ConvertCommand
- Dependabot Dependency Automation
- ConvertCommand
- Setup .NET Environment
- Mesh Attachments 1..49
- StaticMeshAsset.cs
- Code Quality Analysis Job
- Dynamic Color
- FlagsPropertyEditorViewModel
- EarthTool.CLI.Commands.MSH
- Fact
- package.json
- CommandTypeRegistrar
- ParsedGlb
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- App
- ParameterReader
- CommonCommand
- EarthTool.CLI.Commands.WD
- GltfExportOptions
- validate-glb.mjs
- .CreateCurrentStaticLightGuards
- TransactionalFileSystem
- Official MSH Qualification Performance
- .Resolve
- IDialogService
- GltfInterchange.cs
- EarthTool.CLI.Tests
- ParameterReaderTests
- ParsedGltfAnimationChannel
- Migrate From COLLADA To glTF
- ITextFlagService
- GltfPlanAndReport.cs
- .ToByteArray
- TexPreviewLoadResult
- EntityGroup
- EarthTool.TEX.GUI/App.axaml.cs
- IntCollectionPropertyEditorViewModel
- EarthTool.GLTF/HostExtensions.cs
- StaticHierarchy
- EquipableEntity
- .Create

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 179 edges
2. `GltfInterchange` - 173 edges
3. `GlbDocument` - 138 edges
4. `DynamicGltfDocument` - 108 edges
5. `EarthTool.PAR.Enums` - 90 edges
6. `OperationDiagnostic` - 86 edges
7. `OperationResult` - 79 edges
8. `MetadataGraphValidationTests` - 77 edges
9. `DynamicGltfInterchangeTests` - 76 edges
10. `StaticMeshAsset` - 75 edges

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

## Communities (184 total, 9 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - ".WriteFileAsync"
Cohesion: 0.16
Nodes (10): ITransactionalFileSystem, CancellationToken, Exception, IEnumerable, ILogger, Stream, Task, MshReader (+2 more)

### Community 2 - "AssetResult"
Cohesion: 0.24
Nodes (8): AssetResult, DiagnosticKey, Task, AssetResult, KhronosValidatorServer, OperationCounts, ProfileScope, WorkerContext

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
Nodes (4): BlenderOutputEvidence, IEnumerable, Task, Trait

### Community 7 - ".ToByteArray"
Cohesion: 0.07
Nodes (23): Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding, IEnumerable (+15 more)

### Community 8 - "EarthTool.Common.GUI.ViewModels"
Cohesion: 0.17
Nodes (9): EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, ReactiveCommand, Unit, AboutViewModel, ViewModelBase, ParAboutViewModel, TexAboutViewModel (+1 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.08
Nodes (13): Task, IParFileService, bool, ILogger, ObservableCollection, ReactiveCommand, string, Task (+5 more)

### Community 11 - "OperationResult"
Cohesion: 0.13
Nodes (21): IReadOnlyList, OperationResult, GltfDynamicEditImportResult, GltfEditImportOptions, GltfEditImportResult, GltfExportReceipt, GltfMeshEditImportResult, GltfMetadataConflictResolution (+13 more)

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.07
Nodes (34): DynamicAlphaTiming, DynamicEffectType, DynamicMeshAsset, EffectRectangle, IEnumerable, Vector3, CanonicalDynamicAlpha, CanonicalDynamicEffectShape (+26 more)

### Community 13 - ".CreateStaticLightGuards"
Cohesion: 0.15
Nodes (8): Action, BinaryWriter, MemoryStream, Quaternion, Vector3, ProjectedAttachment, ProjectedCannonRenderPosition, ProjectedStaticLight

### Community 14 - "MshOperationProfile"
Cohesion: 0.11
Nodes (20): DecodedStaticRecord, MeshAssetOrigin, CancellationToken, Guid, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+12 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "DynamicEffectExtension"
Cohesion: 0.13
Nodes (12): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, ReadOnlySpan, DynamicEffectExtension (+4 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "DynamicGltfDocument"
Cohesion: 0.10
Nodes (19): BinaryWriter, float, IEnumerable, int, Stream, string, uint, Vector2 (+11 more)

### Community 19 - "MeshAsset"
Cohesion: 0.13
Nodes (16): CancellationToken, Stream, Task, Action, byte, Func, MeshAsset, MeshAssetKind (+8 more)

### Community 20 - "release-qualification.mjs"
Cohesion: 0.15
Nodes (29): buildEvidence(), collectReceivedFiles(), countDiscoveredTests(), exists(), expectedArtifacts, expectedTestCounts, fail(), forbiddenReleasePaths (+21 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (11): IEnumerable, Task, bool, ILogger, object, ObservableCollection, ReactiveCommand, string (+3 more)

### Community 22 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.05
Nodes (29): EarthTool.PAR.GUI, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, EntityClassType, Faction, ResearchType (+21 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.14
Nodes (11): Bitmap, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap, string (+3 more)

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
Cohesion: 0.22
Nodes (12): EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.Consumer.Tests, EarthTool.MSH.Tests, EarthTool.GLTF.Internal (+4 more)

### Community 30 - "GlbDocument"
Cohesion: 0.06
Nodes (17): CarrierKind, GltfOperationProfile, Guid, ICollection, IDictionary, JsonDocument, JsonElement, Matrix4x4 (+9 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Asset, byte, Bytes, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - ".Create"
Cohesion: 0.16
Nodes (9): int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture, OmniRecord (+1 more)

### Community 34 - "GltfImportPlanSerializer"
Cohesion: 0.27
Nodes (4): JsonElement, GltfImportPlanSerializer, ImportPlanException, JsonValueKind

### Community 35 - "EarthTool.Common"
Cohesion: 0.10
Nodes (13): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.Common, EarthTool.CLI, EarthTool.PAR.Tests.Services, EarthTool.PAR.Tests.Models (+5 more)

### Community 36 - "IReadOnlyList"
Cohesion: 0.14
Nodes (12): DynamicObjectScope, DynamicRecordSlice, CancellationToken, GltfOperationProfile, IDictionary, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList (+4 more)

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.11
Nodes (16): SourceObjectId, StaticRenderObjectId, StaticSourceObject, bool, Dictionary, ICollection, IEnumerable, int (+8 more)

### Community 38 - "Equipment"
Cohesion: 0.09
Nodes (18): LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable, Equipment (+10 more)

### Community 39 - "JsonElement"
Cohesion: 0.15
Nodes (7): ICollection, ISet, JsonDocument, JsonElement, DynamicMetadataGraphException, End, Start

### Community 40 - "IArchive"
Cohesion: 0.06
Nodes (29): DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid, IArchiveFactory, Stream (+21 more)

### Community 41 - ".GenerateSampleData"
Cohesion: 0.12
Nodes (9): bool, ReadOnlyMemory, ArchiveItem, Fact, ArchiveItemTests, Fact, MemoryMappedFile, string (+1 more)

### Community 42 - "EarthTool.PAR.Enums"
Cohesion: 0.10
Nodes (6): EarthTool.PAR.Extensions, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Tests.Factories, EarthTool.PAR.Factories, EarthTool.PAR.Models

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - ".DeserializeAsync"
Cohesion: 0.22
Nodes (5): CancellationToken, IReadOnlyDictionary, SeparateGltfPackage, Stream, Task

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.15
Nodes (13): BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionFingerprint, AnimationProjectionSet (+5 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.13
Nodes (12): Action, bool, IEnumerable, ILogger, ObservableCollection, ReactiveCommand, string, Type (+4 more)

### Community 48 - "IArchiveItem"
Cohesion: 0.09
Nodes (12): EarthTool.CLI.Commands, Type, CommandTypeResolver, ReadOnlyMemory, IArchiveItem, HashSet, TextFlagService, ArchiveItemViewModel (+4 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (17): FileFlags, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo, Encoding (+9 more)

### Community 51 - "CanonicalMeshAuthoringTests"
Cohesion: 0.07
Nodes (21): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, Fact, IEnumerable (+13 more)

### Community 52 - "PropertyEditorFactory"
Cohesion: 0.27
Nodes (7): Action, HashSet, IEnumerable, ILogger, Type, PropertyEditorFactory, PropertyInfo

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.20
Nodes (8): BinaryWriter, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification, OperationCounts, ValidatorCode

### Community 54 - "IReadOnlyList"
Cohesion: 0.13
Nodes (15): AnimationObjectLayout, PartitionMatch, IReadOnlyList, AnimationLayout, ByteArrayComparer, GeometryPartition, ParsedGltfPrimitive, PartitionLayout (+7 more)

### Community 55 - "IReadOnlyList"
Cohesion: 0.10
Nodes (21): Discarded, IDictionary, IReadOnlyCollection, IReadOnlyDictionary, IReadOnlyList, ISet, List, Matrix4x4 (+13 more)

### Community 56 - "Entity"
Cohesion: 0.08
Nodes (23): EarthTool.PAR.Models.Serialization, EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List, ValidationError, ValidationResult (+15 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "IUndoRedoService"
Cohesion: 0.11
Nodes (11): Action, IEnumerable, IUndoRedoService, int, string, IntPropertyEditorViewModel, bool, int (+3 more)

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - ".CreateJson"
Cohesion: 0.20
Nodes (3): DynamicImageLayout, DynamicMeshLayout, Utf8JsonWriter

### Community 62 - "InteractableEntity"
Cohesion: 0.08
Nodes (19): BarrelBetaType, ShadowType, TargetType, WeaponFireType, Encoding, IEnumerable, InteractableEntity, Encoding (+11 more)

### Community 63 - "DestructibleEntity"
Cohesion: 0.05
Nodes (28): DamageFlags, ExplosionFlags, HitType, MissileType, RocketType, StandType, StoreableFlags, WasteSize (+20 more)

### Community 64 - "INotificationService"
Cohesion: 0.09
Nodes (16): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.Views, NotificationType, Exception, INotificationService, NotificationEventArgs (+8 more)

### Community 65 - "GltfCliReportSerializer"
Cohesion: 0.20
Nodes (5): GltfNewModelHelperKind, GltfStaticObjectRoles, IEnumerable, Utf8JsonWriter, GltfCliReportSerializer

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "MetadataEnvelope"
Cohesion: 0.09
Nodes (35): ImportPlanException, DynamicMetadataIdentityException, Projection, Version, MalformedMetadataException, MetadataAnimationClass, MetadataAnimationProjection, MetadataConflictException (+27 more)

### Community 68 - "EditableEntity"
Cohesion: 0.14
Nodes (11): bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, bool, FlagValueViewModel (+3 more)

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
Cohesion: 0.09
Nodes (10): Action, Guid, InlineData, IReadOnlyList, JsonObject, List, Theory, BlenderOutputEvidence (+2 more)

### Community 73 - ".RunAsync"
Cohesion: 0.07
Nodes (27): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+19 more)

### Community 74 - "GltfCommandSettings.cs"
Cohesion: 0.24
Nodes (11): AsyncCommand, CancellationToken, CommandContext, Task, ExportGltfCommand, ImportEditGltfCommand, ImportNewGltfCommand, ExportGltfSettings (+3 more)

### Community 75 - "MeshAssetAuthoring.cs"
Cohesion: 0.09
Nodes (18): StaticRenderObjectFlags, HashSet, IReadOnlyList, List, Vector2, Vector3, AuthoringValidation, CanonicalHorizontalExtents (+10 more)

### Community 76 - ".WriteReportAsync"
Cohesion: 0.17
Nodes (4): Stream, CliReportFileSystem, ICliReportFileSystem, Exception

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.05
Nodes (27): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Services, EarthTool.WD.GUI, EarthTool.Common.Models (+19 more)

### Community 79 - "TreeItemViewModel"
Cohesion: 0.12
Nodes (11): DateTime, int, long, string, ArchiveInfoViewModel, HashSet, bool, Guid (+3 more)

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - ".Create"
Cohesion: 0.21
Nodes (6): AnimationLengths, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 82 - "StaticMeshAsset"
Cohesion: 0.15
Nodes (13): AnimationLayout, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, NativeProjectionFingerprint, Utf8JsonWriter, GltfPackage, StaticMeshAsset (+5 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.08
Nodes (24): Animations, Blender 4.5 glTF round-trip research, Conclusion, Decision consequences for later tickets, Diagnostic asset, EarthTool metadata requirements, Evidence model, Extras and custom properties (+16 more)

### Community 84 - "MshCanonicalSerializer"
Cohesion: 0.08
Nodes (24): CanonicalStaticRecord, Matrix4x4, Vector3, MeshArchiveFraming, StaticAnimationTracks, StaticRenderObject, CanonicalStaticFootprint, StaticAnimationReplacement (+16 more)

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.25
Nodes (6): ContentFingerprint, IEnumerable, Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "EarthTool.TEX"
Cohesion: 0.13
Nodes (10): EarthTool.TEX, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, IServiceCollection, HostExtensions, IEnumerable, TexHeader, TexImage (+2 more)

### Community 88 - "Program"
Cohesion: 0.50
Nodes (3): AppBuilder, STAThread, Program

### Community 89 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.23
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "UndoRedoService"
Cohesion: 0.14
Nodes (9): Action, DateTime, UndoAction, Action, IEnumerable, ILogger, int, UndoRedoService (+1 more)

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "TexPreview"
Cohesion: 0.27
Nodes (6): Exception, PreviewResolution, TexPreview, PreviewResolution, PreviewResolutionKind, TexResolutionBudget

### Community 95 - "Runner"
Cohesion: 0.11
Nodes (15): ChannelReader, ChannelWriter, Dictionary, Guid, int, long, object, string (+7 more)

### Community 96 - "ViewLocator"
Cohesion: 0.10
Nodes (11): EarthTool.TEX.GUI, Control, ViewLocator, AppBuilder, STAThread, Program, Control, ViewLocator (+3 more)

### Community 97 - "GltfInterchange"
Cohesion: 0.06
Nodes (14): AnimationReplacement, IReadOnlyDictionary, OperationDiagnostic, IEnumerable, JsonNode, JsonObject, ReadOnlySpan, AnimationReplacement (+6 more)

### Community 98 - "EarthTool Suite"
Cohesion: 0.22
Nodes (10): EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management, MSH Model Export Workflow (+2 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "VerticalTransporter"
Cohesion: 0.12
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 101 - "ArchiverServiceTests"
Cohesion: 0.15
Nodes (10): CancellationToken, CommandContext, CancellationToken, CommandContext, DateTime, Guid, IArchiver, Fact (+2 more)

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
Cohesion: 0.18
Nodes (10): List, KhronosValidatorServer, ValidatorResult, WorkerContext, IAsyncDisposable, Process, QualificationProfiler, ValidatorCode (+2 more)

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - "TexPreviewLoader"
Cohesion: 0.27
Nodes (7): byte, CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, PreviewResolutionKind, TexPreviewLoader

### Community 109 - ".Decode"
Cohesion: 0.12
Nodes (9): BinaryReader, IEnumerable, int, long, TexHeader, TexResolutionBudget, TexFlags, HasVariants (+1 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.11
Nodes (19): CancellationToken, Func, IEnumerable, IReadOnlyList, Task, TextWriter, GltfCommandExecutor, Guid (+11 more)

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

### Community 117 - "PublicCutoverAcceptanceTests"
Cohesion: 0.22
Nodes (7): CliResult, Fact, Task, CliResult, PublicCutoverAcceptanceTests, GeneratedRegex, Regex

### Community 118 - ".ReadRibbonPreview"
Cohesion: 0.36
Nodes (4): DynamicEditedPreview, DynamicEffectPreview, ReadOnlySpan, DynamicPreviewException

### Community 119 - "PassiveEntity"
Cohesion: 0.17
Nodes (9): ArtifactType, PassiveMask, Encoding, IEnumerable, PassiveEntity, Encoding, IEnumerable, Artifact (+1 more)

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "MeshAssetLineageId"
Cohesion: 0.23
Nodes (8): Guid, IEnumerable, WalkingSkeletonConsumer, Guid, MeshAssetLineageId, MshBuildResult, IEnumerable, MshExpert

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

### Community 126 - "OfficialCorpusCliOracle"
Cohesion: 0.16
Nodes (10): CliProcessResult, CliReportOperation, DiagnosticSeverity, IReadOnlyList, Task, CliDiagnostic, CliOracleResult, CliProcessResult (+2 more)

### Community 127 - "ParFile"
Cohesion: 0.20
Nodes (8): ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile, Encoding, ParameterWriter

### Community 128 - "PropertyEditorViewModel"
Cohesion: 0.21
Nodes (9): Action, IEnumerable, IPropertyEditorFactory, bool, ReactiveCommand, string, Type, Unit (+1 more)

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

### Community 133 - "TexFile"
Cohesion: 0.24
Nodes (8): BinaryReader, IEnumerable, TexFile, TexHeader, BinaryReader, IEnumerable, SKBitmap, TexImage

### Community 134 - ".Write_And_Read_AreSymmetric"
Cohesion: 0.29
Nodes (5): Writer, Fact, ParameterWriterTests, Encoding, ParTestData

### Community 135 - "ConvertCommand"
Cohesion: 0.15
Nodes (13): IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+5 more)

### Community 136 - "Dependabot Dependency Automation"
Cohesion: 0.50
Nodes (4): Dependabot Dependency Automation, Weekly GitHub Actions Updates, Weekly NuGet Updates, Security Check Job

### Community 137 - "ConvertCommand"
Cohesion: 0.21
Nodes (10): CommonCommand, CommonSettings, IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, Settings (+2 more)

### Community 138 - "Setup .NET Environment"
Cohesion: 0.67
Nodes (3): .NET SDK Setup, NuGet Package Cache, Setup .NET Environment

### Community 139 - "Mesh Attachments 1..49"
Cohesion: 0.67
Nodes (3): Trailing Hierarchy Unwind Count, Mesh Attachments 1..49, Mesh Extents

### Community 140 - "StaticMeshAsset.cs"
Cohesion: 0.14
Nodes (5): IReadOnlyList, AnimationClassBytes, CommonMeshBaseHeader, DynamicObject, StaticAnimationClass

### Community 143 - "FlagsPropertyEditorViewModel"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 148 - "EarthTool.CLI.Commands.MSH"
Cohesion: 0.25
Nodes (5): EarthTool.CLI.Commands.MSH, EarthTool.MSH, EarthTool.CLI.Tests, IServiceCollection, HostExtensions

### Community 149 - "Fact"
Cohesion: 0.11
Nodes (3): Fact, JsonDocument, JsonElement

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "CommandTypeRegistrar"
Cohesion: 0.22
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 152 - "ParsedGlb"
Cohesion: 0.08
Nodes (20): string, GltfAnimationHandle, GltfDiagnosticCodes, GltfLightHandle, GltfMaterialHandle, GltfMetadataConflictActions, GltfNewModelAnimationClass, GltfNewModelFootprint (+12 more)

### Community 153 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 154 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 155 - "ParameterReader"
Cohesion: 0.26
Nodes (6): Reader, FileType, BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 156 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 158 - "EarthTool.CLI.Commands.WD"
Cohesion: 0.06
Nodes (34): Command, CommandSettings, EarthTool.CLI.Commands.WD, CommonSettings, AddCommand, CreateCommand, CancellationToken, CommandContext (+26 more)

### Community 159 - "GltfExportOptions"
Cohesion: 0.29
Nodes (6): Guid, IReadOnlyDictionary, IReadOnlyList, GltfExportOptions, GltfMetadataConflictCatalog, GltfMetadataIdentity

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Measured Result, Official MSH Qualification Performance, Stage Profiling, Aggregate release qualification, Blender matrix, Official MSH corpus

### Community 166 - ".Resolve"
Cohesion: 0.33
Nodes (4): Ambiguous, Path, Root, Shadowed

### Community 168 - "GltfInterchange.cs"
Cohesion: 0.48
Nodes (6): Exception, AmbiguousPartitionCorrespondenceException, MetadataIdentityException, ResourceLimitException, StaleNativeProjectionException, StaticLightMetadataException

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 171 - "ParsedGltfAnimationChannel"
Cohesion: 0.27
Nodes (7): float, int, string, ParsedAnimationBuilder, ParsedGltfAnimation, ParsedGltfAnimationChannel, ParsedGltfAnimationObject

### Community 172 - "Migrate From COLLADA To glTF"
Cohesion: 0.22
Nodes (7): Dynamic effect-preview contract, glTF API, API migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 174 - "GltfPlanAndReport.cs"
Cohesion: 0.53
Nodes (5): int, IReadOnlyList, string, GltfCliReportFormat, GltfImportPlanFormat

### Community 175 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 176 - "TexPreviewLoadResult"
Cohesion: 0.50
Nodes (4): IReadOnlyDictionary, IReadOnlyList, DynamicTexPreviewLoadResult, TexPreviewLoadResult

### Community 177 - "EntityGroup"
Cohesion: 0.12
Nodes (11): EarthTool.CLI.Commands.PAR, CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings, Encoding, IBinarySerializable (+3 more)

### Community 178 - "EarthTool.TEX.GUI/App.axaml.cs"
Cohesion: 0.12
Nodes (10): EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs (+2 more)

### Community 179 - "IntCollectionPropertyEditorViewModel"
Cohesion: 0.60
Nodes (3): IEnumerable, string, IntCollectionPropertyEditorViewModel

### Community 182 - "EquipableEntity"
Cohesion: 0.07
Nodes (19): BuildingExType, BuildingTabType, BuildingType, ConnectorType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags (+11 more)

### Community 189 - ".Create"
Cohesion: 0.16
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

## Knowledge Gaps
- **320 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+315 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **9 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `EarthTool.Common`, `ConvertCommand`, `IValueConverter`, `EarthTool.PAR.Enums`, `IArchiveItem`, `IEarthInfo`, `EarthTool.CLI.Commands.MSH`, `EarthTool.TEX`, `ParameterReader`?**
  _High betweenness centrality (0.180) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `.RunAsync` to `IArchiveItem`, `DynamicGltfInterchangeTests`, `.DeserializeAsync`?**
  _High betweenness centrality (0.127) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `MetadataEnvelope`, `GltfInterchange.cs`, `MeshAssetAuthoring.cs`, `StaticMeshAsset.cs`, `DynamicGltfInterchangeTests`, `StaticAnimationProjection`, `GltfPlanAndReport.cs`, `DynamicEffectExtension`, `MeshAsset`, `EarthTool.CLI.Commands.MSH`, `ParsedGlb`?**
  _High betweenness centrality (0.109) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _320 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10741971207087486 - nodes in this community are weakly interconnected._
- **Should `GltfPlanAndReportTests` be split into smaller, more focused modules?**
  _Cohesion score 0.13076923076923078 - nodes in this community are weakly interconnected._