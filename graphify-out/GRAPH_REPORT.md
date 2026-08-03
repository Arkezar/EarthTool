# Graph Report - EarthTool  (2026-08-03)

## Corpus Check
- 363 files · ~280,310 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4555 nodes · 13383 edges · 181 communities (169 shown, 12 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 852 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `12be5090`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- .WriteFileAsync
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- GltfPlanAndReportTests
- .ReadAssetAsync
- .ToByteArray
- .ResolveAndLoad
- IValueConverter
- MainWindowViewModel
- OperationResult
- DynamicGltfInterchangeTests
- .CreateStaticLightGuards
- MshOperationProfile
- .OpenArchive
- DynamicEffectSemantics
- Dynamic MESH Binary Layout
- EarthTool.PAR.GUI.ViewModels
- GltfImportPlanSerializer
- release-qualification.mjs
- MainWindowViewModel
- TreeNodeViewModelBase
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
- StaticMeshAsset.cs
- EarthTool.PAR.Factories
- IReadOnlyList
- StaticMeshEditSession
- InteractableEntity
- GltfInterchange
- ArchiverService
- .GenerateSampleData
- GltfWalkingSkeletonTests
- Static Mesh Header
- StaticObject Record
- Equipment
- StaticAnimationProjection
- EntityDetailsViewModel
- ParFile
- PublicApiApproval
- IEarthInfo
- CanonicalMeshAuthoringTests
- GltfCliReportSerializer
- OfficialCorpusQualification
- IReadOnlyList
- MetadataEnvelope
- Entity
- EarthTool.MSH.Tests
- GltfCommandSettings.cs
- DialogService
- EarthTool.PAR
- DynamicGltfDocument
- EarthTool.PAR.Enums
- DestructibleEntity
- TreeItemViewModel
- IArchiver
- EarthTool.PAR.GUI
- StaticMeshAsset
- IArchiveItem
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- Runner
- TexPreviewLoader
- Task
- MeshAssetLineageId
- AuthoringValidation
- JsonElement
- EarthTool.sln
- EarthTool.Common.Interfaces
- EarthTool.TEX
- BinaryExtensions
- .Create
- .CreateJson
- Blender 4.5 glTF round-trip research
- MshCanonicalSerializer
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- IUndoRedoService
- DynamicEffectExtension
- ResearchReferenceCollectionEditorViewModel
- EarthTool WD Archive Manager
- PublicCutoverAcceptanceTests
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- EarthTool.Common.GUI.Enums
- QualificationProfiler
- ViewLocator
- GltfCliReportOperation
- EarthTool Suite
- WD Central Directory
- VerticalTransporter
- IArchive
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- WorkerContext
- Reader
- GltfOperationProfile
- .Decode
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- EnumPropertyEditorViewModel
- .WriteReportAsync
- NotificationService
- .ValidateMetadataGraph
- EarthTool.TEX
- PassiveEntity
- .ExportGlbAsync
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- PropertyEditorViewModel
- TexPreview
- Base Header
- ArchiveItem
- UnitTest1.cs
- EarthTool Installation Guide
- EarthTool.CLI.Commands.MSH
- GltfInterchange.cs
- ConvertCommand
- Dependabot Dependency Automation
- ConvertCommand
- Setup .NET Environment
- Mesh Attachments 1..49
- OperationDiagnostic
- Code Quality Analysis Job
- Dynamic Color
- FlagsPropertyEditorViewModel
- MeshAsset
- Task
- package.json
- CommandTypeRegistrar
- InterchangeBaseline
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- App
- MainWindow
- CommonCommand
- .Load
- validate-glb.mjs
- EarthTool.GLTF/HostExtensions.cs
- TexFile
- Official MSH Qualification Performance
- IDialogService
- InMemoryArchiveDataSourceTests
- .ValidateOwnedAccessor
- EarthTool.CLI.Tests
- OneTriangleMshFixture
- GlbDocument.cs
- .CreateCurrentStaticLightGuards
- IDisposable
- IExtractor
- .ToByteArray
- ParameterReaderTests
- GltfAnimationHandle
- MshDiagnosticCodes
- EquipableEntity
- .Create

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 181 edges
2. `GltfInterchange` - 175 edges
3. `GlbDocument` - 141 edges
4. `DynamicGltfDocument` - 117 edges
5. `DynamicGltfInterchangeTests` - 94 edges
6. `OperationDiagnostic` - 90 edges
7. `EarthTool.PAR.Enums` - 90 edges
8. `OperationResult` - 79 edges
9. `MetadataGraphValidationTests` - 77 edges
10. `StaticMeshAsset` - 73 edges

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

## Communities (181 total, 12 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - ".WriteFileAsync"
Cohesion: 0.12
Nodes (12): Stream, ITransactionalFileSystem, TransactionalFileSystem, CancellationToken, Exception, IEnumerable, ILogger, Stream (+4 more)

### Community 2 - "AssetResult"
Cohesion: 0.22
Nodes (7): AssetResult, DiagnosticKey, AssetResult, KhronosValidatorServer, OperationCounts, ProfileScope, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.09
Nodes (18): EarthTool.WD.Tests.Services, EarthTool.WD.Services, ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream (+10 more)

### Community 5 - "GltfPlanAndReportTests"
Cohesion: 0.13
Nodes (14): BufferPath, ConflictKey, Directory, Fact, Guid, InlineData, JsonNode, Task (+6 more)

### Community 7 - ".ToByteArray"
Cohesion: 0.07
Nodes (23): Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding, IEnumerable (+15 more)

### Community 8 - ".ResolveAndLoad"
Cohesion: 0.08
Nodes (21): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+13 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (8): bool, ILogger, ObservableCollection, ReactiveCommand, string, Task, Unit, MainWindowViewModel

### Community 11 - "OperationResult"
Cohesion: 0.17
Nodes (10): IReadOnlyList, OperationResult, GltfNewModelImportResult, GltfOperationProfile, CancellationToken, SeparateGltfPackage, Stream, Task (+2 more)

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.06
Nodes (35): DynamicAlphaTiming, DynamicEffectType, DynamicMeshAsset, EffectRectangle, IEnumerable, Vector3, CanonicalDynamicAlpha, CanonicalDynamicEffectShape (+27 more)

### Community 13 - ".CreateStaticLightGuards"
Cohesion: 0.17
Nodes (7): BinaryWriter, MemoryStream, Quaternion, Vector3, ProjectedAttachment, ProjectedCannonRenderPosition, ProjectedStaticLight

### Community 14 - "MshOperationProfile"
Cohesion: 0.12
Nodes (18): MeshAssetOrigin, CancellationToken, Guid, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList, List (+10 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (9): BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory, Fact (+1 more)

### Community 16 - "DynamicEffectSemantics"
Cohesion: 0.17
Nodes (9): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, Fact, Guid (+1 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.08
Nodes (10): EarthTool.PAR.GUI, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, ValidationError, ValidationSeverity, AppBuilder (+2 more)

### Community 19 - "GltfImportPlanSerializer"
Cohesion: 0.12
Nodes (10): CancellationToken, IEnumerable, IReadOnlyDictionary, JsonElement, SeparateGltfPackage, Stream, Task, GltfImportPlanSerializer (+2 more)

### Community 20 - "release-qualification.mjs"
Cohesion: 0.07
Nodes (62): corpusBinaryStages, corpusInterchangeStages, recognizedDynamicEffectTypes, assertPrivacySafe(), buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames() (+54 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (11): INotificationService, ITextFlagService, bool, ILogger, object, ObservableCollection, ReactiveCommand, string (+3 more)

### Community 22 - "TreeNodeViewModelBase"
Cohesion: 0.06
Nodes (31): EntityClassType, EntityGroupType, Faction, ResearchType, BinaryReader, IEnumerable, EntityFactory, ObservableCollection (+23 more)

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
Cohesion: 0.16
Nodes (9): bool, DateTime, IReadOnlyCollection, MemoryMappedFile, Archive, Fact, ArchiveTests, Guid (+1 more)

### Community 29 - "EarthTool.MSH.Assets"
Cohesion: 0.17
Nodes (15): EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.Consumer.Tests, EarthTool.MSH.Tests, EarthTool.GLTF.Internal (+7 more)

### Community 30 - "GlbDocument"
Cohesion: 0.08
Nodes (11): GltfOperationProfile, IDictionary, JsonDocument, JsonElement, Matrix4x4, ReadOnlySpan, uint, GlbDocument (+3 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Asset, byte, Bytes, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - ".Create"
Cohesion: 0.16
Nodes (9): int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture, OmniRecord (+1 more)

### Community 34 - "StaticMeshAsset.cs"
Cohesion: 0.14
Nodes (13): Matrix4x4, Vector3, StaticAnimationClass, StaticAnimationTracks, StaticRenderObject, StaticRenderObjectFlags, CanonicalStaticObjectRole, StaticAnimationReplacement (+5 more)

### Community 35 - "EarthTool.PAR.Factories"
Cohesion: 0.18
Nodes (7): EarthTool.PAR.Tests.Factories, EarthTool.PAR.Tests.Models, EarthTool.PAR.Factories, Fact, MissileSerializationTests, Fact, WeaponSerializationTests

### Community 36 - "IReadOnlyList"
Cohesion: 0.11
Nodes (15): DynamicImageLayout, DynamicMeshLayout, DynamicObjectScope, DynamicRecordSlice, BinaryWriter, IDictionary, IReadOnlyDictionary, IReadOnlyList (+7 more)

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.10
Nodes (17): SourceObjectId, StaticRenderObjectId, StaticSourceObject, bool, Dictionary, ICollection, IEnumerable, int (+9 more)

### Community 38 - "InteractableEntity"
Cohesion: 0.09
Nodes (17): BarrelBetaType, ShadowType, TargetType, WeaponFireType, Encoding, IEnumerable, InteractableEntity, Encoding (+9 more)

### Community 39 - "GltfInterchange"
Cohesion: 0.10
Nodes (12): AnimationReplacement, GltfLightHandle, GltfMaterialHandle, GltfNewModelImportOptions, GltfNodeHandle, ICollection, JsonObject, Path (+4 more)

### Community 40 - "ArchiverService"
Cohesion: 0.07
Nodes (26): Encoding, DateTime, Guid, IArchiveFactory, Stream, ICompressor, ReadOnlySpan, Stream (+18 more)

### Community 41 - ".GenerateSampleData"
Cohesion: 0.13
Nodes (7): ArchiveTestsBase, Fact, ArchiveItemTests, Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 42 - "GltfWalkingSkeletonTests"
Cohesion: 0.10
Nodes (8): Action, Guid, IReadOnlyList, JsonObject, List, BlenderOutputEvidence, GltfWalkingSkeletonTests, JsonArray

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - "Equipment"
Cohesion: 0.09
Nodes (18): LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable, Equipment (+10 more)

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.15
Nodes (13): BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionFingerprint, AnimationProjectionSet (+5 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.07
Nodes (23): bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, Action, bool (+15 more)

### Community 48 - "ParFile"
Cohesion: 0.08
Nodes (21): Reader, Writer, FileType, Task, IParFileService, ILogger, Task, ParFileService (+13 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (21): FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo (+13 more)

### Community 51 - "CanonicalMeshAuthoringTests"
Cohesion: 0.07
Nodes (21): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, Fact, IEnumerable (+13 more)

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.17
Nodes (10): ContentFingerprint, BinaryWriter, IEnumerable, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification (+2 more)

### Community 54 - "IReadOnlyList"
Cohesion: 0.15
Nodes (15): AnimationObjectLayout, PartitionMatch, IReadOnlyList, AnimationLayout, ByteArrayComparer, GeometryPartition, ParsedGltfPrimitive, PartitionLayout (+7 more)

### Community 55 - "MetadataEnvelope"
Cohesion: 0.09
Nodes (28): Discarded, IDictionary, IReadOnlyCollection, IReadOnlyDictionary, IReadOnlyList, ISet, List, Matrix4x4 (+20 more)

### Community 56 - "Entity"
Cohesion: 0.09
Nodes (19): EarthTool.PAR.Models.Serialization, Encoding, IBinarySerializable, List, ValidationResult, IEnumerable, ILogger, EntityValidationService (+11 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "GltfCommandSettings.cs"
Cohesion: 0.22
Nodes (13): AsyncCommand, CancellationToken, CommandContext, Task, ExportGltfCommand, ImportEditGltfCommand, ImportNewGltfCommand, Guid (+5 more)

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - "DynamicGltfDocument"
Cohesion: 0.12
Nodes (16): float, int, Stream, string, uint, Vector2, Vector3, DynamicEditedPreview (+8 more)

### Community 62 - "EarthTool.PAR.Enums"
Cohesion: 0.09
Nodes (7): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.Extensions, EarthTool.PAR.Tests.TestData, EarthTool.PAR.Tests.Services, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Models

### Community 63 - "DestructibleEntity"
Cohesion: 0.05
Nodes (28): DamageFlags, ExplosionFlags, HitType, MissileType, RocketType, StandType, StoreableFlags, WasteSize (+20 more)

### Community 64 - "TreeItemViewModel"
Cohesion: 0.12
Nodes (11): DateTime, int, long, string, ArchiveInfoViewModel, HashSet, bool, Guid (+3 more)

### Community 65 - "IArchiver"
Cohesion: 0.06
Nodes (38): Command, CommandSettings, CommonSettings, CancellationToken, CommandContext, AddCommand, CancellationToken, CommandContext (+30 more)

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "StaticMeshAsset"
Cohesion: 0.13
Nodes (20): MetadataConflictException, bool, GltfOperationProfile, IEnumerable, int, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList (+12 more)

### Community 68 - "IArchiveItem"
Cohesion: 0.24
Nodes (5): ReadOnlyMemory, IArchiveItem, HashSet, TextFlagService, IComparable

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "Runner"
Cohesion: 0.16
Nodes (8): ChannelReader, ChannelWriter, DynamicCoverage, CliOracleResult, Guid, Task, Runner, ValidatorAggregate

### Community 72 - "TexPreviewLoader"
Cohesion: 0.21
Nodes (6): byte, Exception, PreviewResolutionKind, TexHeader, TexPreviewLoader, TexFlags

### Community 73 - "Task"
Cohesion: 0.07
Nodes (27): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+19 more)

### Community 74 - "MeshAssetLineageId"
Cohesion: 0.11
Nodes (11): DecodedStaticRecord, Guid, IEnumerable, WalkingSkeletonConsumer, Guid, MeshArchiveFraming, MeshAssetLineageId, MshBuildResult (+3 more)

### Community 75 - "AuthoringValidation"
Cohesion: 0.15
Nodes (5): HashSet, List, Vector2, Vector3, AuthoringValidation

### Community 76 - "JsonElement"
Cohesion: 0.12
Nodes (8): DynamicSceneLayout, CancellationToken, InterchangeBaseline, JsonDocument, JsonElement, ReadOnlyMemory, DynamicGltfImport, DynamicMetadataGraphException

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.04
Nodes (34): EarthTool.WD.GUI.ViewModels, EarthTool.CLI.Commands.PAR, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Factories, EarthTool.PAR, EarthTool.PAR.Services (+26 more)

### Community 79 - "EarthTool.TEX"
Cohesion: 0.13
Nodes (10): EarthTool.TEX, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, IServiceCollection, HostExtensions, IEnumerable, TexHeader, TexImage (+2 more)

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - ".Create"
Cohesion: 0.10
Nodes (8): AnimationLengths, JsonDocument, JsonElement, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 82 - ".CreateJson"
Cohesion: 0.12
Nodes (13): AnimationLayout, Action, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, NativeProjectionFingerprint, Utf8JsonWriter, GltfPackage (+5 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.06
Nodes (31): Dynamic effect-preview contract, glTF API, Animations, Blender 4.5 glTF round-trip research, Conclusion, Decision consequences for later tickets, Diagnostic asset, EarthTool metadata requirements (+23 more)

### Community 84 - "MshCanonicalSerializer"
Cohesion: 0.07
Nodes (26): CanonicalStaticRecord, AnimationClassBytes, IReadOnlyList, CanonicalHorizontalExtents, CanonicalStaticFootprint, CanonicalStaticRenderObject, CanonicalStaticSourceObject, CanonicalStaticVertex (+18 more)

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "IUndoRedoService"
Cohesion: 0.06
Nodes (23): Action, DateTime, UndoAction, Action, IEnumerable, IUndoRedoService, Action, IEnumerable (+15 more)

### Community 88 - "DynamicEffectExtension"
Cohesion: 0.17
Nodes (8): IEnumerable, DynamicObjectScope, IReadOnlyList, ReadOnlySpan, CommonMeshBaseHeader, DynamicEffectExtension, DynamicLightType, DynamicObject

### Community 89 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.20
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "PublicCutoverAcceptanceTests"
Cohesion: 0.22
Nodes (7): CliResult, Fact, Task, CliResult, PublicCutoverAcceptanceTests, GeneratedRegex, Regex

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.06
Nodes (24): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.TEX.GUI, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, EarthTool.Common.GUI, EarthTool.Common.GUI.Views (+16 more)

### Community 95 - "QualificationProfiler"
Cohesion: 0.13
Nodes (14): Dictionary, IDictionary, int, IReadOnlyDictionary, ISet, long, object, string (+6 more)

### Community 96 - "ViewLocator"
Cohesion: 0.12
Nodes (9): EarthTool.WD.GUI, Control, ViewLocator, AppBuilder, STAThread, Program, Control, ViewLocator (+1 more)

### Community 97 - "GltfCliReportOperation"
Cohesion: 0.24
Nodes (10): Guid, int, IReadOnlyList, string, GltfCliReport, GltfCliReportFormat, GltfCliReportOperation, GltfCliReportOperationKind (+2 more)

### Community 98 - "EarthTool Suite"
Cohesion: 0.20
Nodes (11): EarthTool.DAE, EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management (+3 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "VerticalTransporter"
Cohesion: 0.12
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 101 - "IArchive"
Cohesion: 0.16
Nodes (8): DateTime, IReadOnlyCollection, IArchive, DateTime, Guid, Fact, string, ArchiverServiceTests

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
Cohesion: 0.18
Nodes (10): List, KhronosValidatorServer, ValidatorResult, WorkerContext, IAsyncDisposable, Process, QualificationProfiler, ValidatorCode (+2 more)

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - "GltfOperationProfile"
Cohesion: 0.30
Nodes (5): DynamicEditedPreview, DynamicEffectPreview, GltfOperationProfile, ReadOnlySpan, DynamicPreviewException

### Community 109 - ".Decode"
Cohesion: 0.18
Nodes (7): BinaryReader, IEnumerable, int, long, TexResolutionBudget, HasVariants, Preview

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

### Community 117 - ".WriteReportAsync"
Cohesion: 0.17
Nodes (4): Stream, CliReportFileSystem, ICliReportFileSystem, Exception

### Community 118 - "NotificationService"
Cohesion: 0.19
Nodes (7): NotificationType, Exception, NotificationEventArgs, Exception, ILogger, NotificationService, EventArgs

### Community 119 - ".ValidateMetadataGraph"
Cohesion: 0.21
Nodes (5): CarrierKind, ICollection, Path, Value, Envelope

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "PassiveEntity"
Cohesion: 0.17
Nodes (9): ArtifactType, PassiveMask, Encoding, IEnumerable, PassiveEntity, Encoding, IEnumerable, Artifact (+1 more)

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

### Community 127 - "PropertyEditorViewModel"
Cohesion: 0.14
Nodes (16): Action, IEnumerable, IPropertyEditorFactory, Action, HashSet, IEnumerable, ILogger, Type (+8 more)

### Community 128 - "TexPreview"
Cohesion: 0.33
Nodes (5): PreviewResolution, TexPreview, PreviewResolution, PreviewResolutionKind, TexResolutionBudget

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "ArchiveItem"
Cohesion: 0.13
Nodes (13): ReadOnlyMemory, IArchiveDataSource, bool, ReadOnlyMemory, ArchiveItem, int, MemoryMappedFile, ReadOnlyMemory (+5 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - "EarthTool.CLI.Commands.MSH"
Cohesion: 0.25
Nodes (5): EarthTool.CLI.Commands.MSH, EarthTool.MSH, EarthTool.CLI.Tests, IServiceCollection, HostExtensions

### Community 134 - "GltfInterchange.cs"
Cohesion: 0.48
Nodes (6): Exception, AmbiguousPartitionCorrespondenceException, MetadataIdentityException, ResourceLimitException, StaleNativeProjectionException, StaticLightMetadataException

### Community 135 - "ConvertCommand"
Cohesion: 0.10
Nodes (19): IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand, CancellationToken, CommandContext (+11 more)

### Community 136 - "Dependabot Dependency Automation"
Cohesion: 0.50
Nodes (4): Dependabot Dependency Automation, Weekly GitHub Actions Updates, Weekly NuGet Updates, Security Check Job

### Community 137 - "ConvertCommand"
Cohesion: 0.23
Nodes (9): CommonCommand, CommonSettings, IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, Settings (+1 more)

### Community 138 - "Setup .NET Environment"
Cohesion: 0.67
Nodes (3): .NET SDK Setup, NuGet Package Cache, Setup .NET Environment

### Community 139 - "Mesh Attachments 1..49"
Cohesion: 0.67
Nodes (3): Trailing Hierarchy Unwind Count, Mesh Attachments 1..49, Mesh Extents

### Community 140 - "OperationDiagnostic"
Cohesion: 0.11
Nodes (6): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, IEnumerable, ReadOnlySpan, ParseScopeResolution

### Community 143 - "FlagsPropertyEditorViewModel"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 148 - "MeshAsset"
Cohesion: 0.15
Nodes (14): CancellationToken, Stream, Task, Action, byte, Func, MeshAsset, MeshAssetKind (+6 more)

### Community 149 - "Task"
Cohesion: 0.12
Nodes (6): BlenderOutputEvidence, IEnumerable, InlineData, Task, Theory, Trait

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "CommandTypeRegistrar"
Cohesion: 0.22
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 152 - "InterchangeBaseline"
Cohesion: 0.08
Nodes (32): Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfDiagnosticCodes, GltfDynamicEditImportResult, GltfEditImportOptions, GltfEditImportResult (+24 more)

### Community 153 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 154 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 155 - "MainWindow"
Cohesion: 0.14
Nodes (9): EarthTool.TEX.GUI.Views, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs, RoutedEventArgs (+1 more)

### Community 156 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 159 - ".Load"
Cohesion: 0.22
Nodes (8): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IReadOnlyDictionary, IReadOnlyList, DynamicTexPreviewLoadResult, TexPreviewLoadResult

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 164 - "TexFile"
Cohesion: 0.24
Nodes (8): BinaryReader, IEnumerable, TexFile, TexHeader, BinaryReader, IEnumerable, SKBitmap, TexImage

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Historical Measured Result, Official MSH Qualification Performance, Stage Profiling, Aggregate release qualification, Blender matrix, Official MSH corpus

### Community 166 - "IDialogService"
Cohesion: 0.24
Nodes (3): IEnumerable, Task, IDialogService

### Community 168 - ".ValidateOwnedAccessor"
Cohesion: 0.28
Nodes (5): ICollection, ISet, End, NativeObjectGraph, Start

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 171 - "GlbDocument.cs"
Cohesion: 0.11
Nodes (20): ImportPlanException, DynamicMetadataIdentityException, float, int, string, MalformedMetadataException, MetadataAnimationClass, MetadataAnimationProjection (+12 more)

### Community 173 - "IDisposable"
Cohesion: 0.29
Nodes (5): Type, CommandTypeResolver, IDisposable, IHost, ITypeResolver

### Community 174 - "IExtractor"
Cohesion: 0.33
Nodes (3): Task, IExtractor, IWDExtractor

### Community 175 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 182 - "EquipableEntity"
Cohesion: 0.07
Nodes (19): BuildingExType, BuildingTabType, BuildingType, ConnectorType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags (+11 more)

### Community 189 - ".Create"
Cohesion: 0.16
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

## Knowledge Gaps
- **320 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+315 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **12 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `EarthTool.CLI.Commands.MSH`, `ConvertCommand`, `IValueConverter`, `EarthTool.TEX`, `ParFile`, `IEarthInfo`?**
  _High betweenness centrality (0.159) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `Task` to `GltfImportPlanSerializer`, `DynamicGltfInterchangeTests`, `IDisposable`?**
  _High betweenness centrality (0.127) - this node is a cross-community bridge._
- **Why does `MainWindowViewModel` connect `MainWindowViewModel` to `IDialogService`, `IDisposable`, `EntityDetailsViewModel`, `ParFile`, `EarthTool.PAR.GUI.ViewModels`, `MainWindowViewModel`, `TreeNodeViewModelBase`, `IUndoRedoService`, `Entity`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _320 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.WriteFileAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.12312312312312312 - nodes in this community are weakly interconnected._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.08979591836734693 - nodes in this community are weakly interconnected._