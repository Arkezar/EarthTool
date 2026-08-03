# Graph Report - EarthTool  (2026-08-03)

## Corpus Check
- 360 files · ~266,700 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4410 nodes · 12742 edges · 170 communities (163 shown, 7 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 807 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c5d6f15d`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- MeshAsset
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- GltfImportPlanSerializer
- Task
- .ToByteArray
- EarthTool.Common.GUI.ViewModels
- IValueConverter
- MainWindowViewModel
- OperationResult
- DynamicGltfInterchangeTests
- GlbDocument
- MshOperationProfile
- .OpenArchive
- DynamicEffectExtension
- Dynamic MESH Binary Layout
- DynamicMeshAsset
- release-qualification.mjs
- MainWindowViewModel
- EarthTool.PAR.GUI.ViewModels
- MainWindowViewModel
- .ExportGlbFileAsync
- Vehicle
- Common MSH Base Header
- MetadataGraphValidationTests
- ArchiveTests
- EarthTool.MSH.Assets
- JsonElement
- EarthTool.CLI
- DynamicMeshAssetTests
- .Create
- WDExtractorTests
- EarthTool.Common
- DynamicGltfDocument
- StaticMeshEditSession
- CanonicalDynamicObject
- InterchangeBaseline
- ArchiverService
- .CreateMockHeader
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
- IReadOnlyList
- StaticMeshAsset
- Entity
- EarthTool.MSH.Tests
- .BlenderEditsPassOwnershipAwareOracle
- DialogService
- EarthTool.PAR
- EarthTool.CLI.Commands.WD
- InteractableEntity
- DestructibleEntity
- EarthTool.Common.GUI.Enums
- .ReadFloatAccessor
- EarthTool.PAR.GUI
- .DetectStaleGuards
- DynamicEffectRecipeTests
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- official-corpus-qualification.mjs
- .RunAsync
- WdSettings
- AuthoringValidation
- IArchiver
- EarthTool.sln
- EarthTool.Common.Interfaces
- TreeItemViewModel
- BinaryExtensions
- .Create
- .CreateJson
- Blender 4.5 glTF round-trip research
- MshCanonicalSerializer
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- EarthTool.TEX
- EarthTool.WD.GUI
- ResearchReferenceCollectionEditorViewModel
- EarthTool WD Archive Manager
- IUndoRedoService
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- IDisposable
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
- InfoCommand
- TexPreviewLoader
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- EnumPropertyEditorViewModel
- ManifestFailingFileSystem
- .Execute
- .Execute
- EarthTool.TEX
- MeshAssetLineageId
- .ImportNewModelGlbAsync
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- ParFile
- Program
- Base Header
- EarthTool.WD.Models
- UnitTest1.cs
- EarthTool Installation Guide
- ConvertCommand
- Dependabot Dependency Automation
- ConvertCommand
- Setup .NET Environment
- Mesh Attachments 1..49
- StaticSourceObject
- Code Quality Analysis Job
- Dynamic Color
- QualificationProfiler
- GltfWalkingSkeletonTests
- package.json
- CommandTypeRegistrar
- ParsedGlb
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- App
- ParameterReader
- WdSettings.cs
- GltfExportOptions
- validate-glb.mjs
- InMemoryArchiveDataSourceTests
- Official MSH Qualification Performance
- OneTriangleMshFixture
- IDialogService
- EarthTool.CLI.Tests
- ParsedGltfAnimationChannel
- Migrate From COLLADA To glTF
- ItemCommand
- MainWindow
- EquipableEntity
- Missile
- .Create

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 179 edges
2. `GltfInterchange` - 173 edges
3. `GlbDocument` - 138 edges
4. `DynamicGltfDocument` - 97 edges
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

## Communities (170 total, 7 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - "MeshAsset"
Cohesion: 0.08
Nodes (24): CancellationToken, Stream, Task, byte, MeshAsset, MeshAssetKind, Stream, ITransactionalFileSystem (+16 more)

### Community 2 - "AssetResult"
Cohesion: 0.26
Nodes (6): AssetResult, DiagnosticKey, AssetResult, OperationCounts, ProfileScope, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.11
Nodes (15): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+7 more)

### Community 5 - "GltfImportPlanSerializer"
Cohesion: 0.06
Nodes (27): BufferPath, ConflictKey, Directory, CancellationToken, Guid, IEnumerable, JsonElement, SeparateGltfPackage (+19 more)

### Community 6 - "Task"
Cohesion: 0.12
Nodes (3): Fact, Task, Action

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
Cohesion: 0.12
Nodes (8): bool, ILogger, ObservableCollection, ReactiveCommand, string, Task, Unit, MainWindowViewModel

### Community 11 - "OperationResult"
Cohesion: 0.11
Nodes (17): IReadOnlyList, OperationResult, GltfExportReceipt, GltfMeshEditImportResult, GltfMetadataLineageDisposition, GltfNewModelImportResult, GltfOperationProfile, NativeProjectionFingerprint (+9 more)

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.13
Nodes (13): Action, Fact, Guid, IEnumerable, InlineData, JsonDocument, JsonElement, JsonNode (+5 more)

### Community 13 - "GlbDocument"
Cohesion: 0.08
Nodes (16): Action, BinaryWriter, IDictionary, IEnumerable, Matrix4x4, MemoryStream, Quaternion, uint (+8 more)

### Community 14 - "MshOperationProfile"
Cohesion: 0.11
Nodes (21): DecodedStaticRecord, MeshAssetOrigin, CancellationToken, Guid, IEnumerable, int, IReadOnlyDictionary, IReadOnlyList (+13 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "DynamicEffectExtension"
Cohesion: 0.12
Nodes (13): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, ReadOnlySpan, DynamicEffectExtension (+5 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 19 - "DynamicMeshAsset"
Cohesion: 0.22
Nodes (6): Action, Func, DynamicMeshAsset, Guid, DynamicMeshBuilder, IReadOnlyList

### Community 20 - "release-qualification.mjs"
Cohesion: 0.15
Nodes (29): buildEvidence(), collectReceivedFiles(), countDiscoveredTests(), exists(), expectedArtifacts, expectedTestCounts, fail(), forbiddenReleasePaths (+21 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.10
Nodes (12): IEnumerable, INotificationService, ITextFlagService, bool, ILogger, object, ObservableCollection, ReactiveCommand (+4 more)

### Community 22 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.07
Nodes (27): EarthTool.PAR.GUI, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, Faction, ResearchType, ObservableCollection, EntityGroupNodeViewModel (+19 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.13
Nodes (12): Bitmap, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap, string (+4 more)

### Community 24 - ".ExportGlbFileAsync"
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
Cohesion: 0.14
Nodes (9): bool, DateTime, IReadOnlyCollection, MemoryMappedFile, Archive, Fact, ArchiveTests, TestDataGenerator (+1 more)

### Community 29 - "EarthTool.MSH.Assets"
Cohesion: 0.07
Nodes (40): CliResult, EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.CLI.Commands.MSH, EarthTool.MSH (+32 more)

### Community 30 - "JsonElement"
Cohesion: 0.08
Nodes (11): CarrierKind, GltfOperationProfile, Guid, ICollection, JsonDocument, JsonElement, Path, Value (+3 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Asset, byte, Bytes, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - ".Create"
Cohesion: 0.16
Nodes (9): int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture, OmniRecord (+1 more)

### Community 34 - "WDExtractorTests"
Cohesion: 0.25
Nodes (9): DateTime, Guid, Fact, string, Task, WDExtractorTests, ILogger, Task (+1 more)

### Community 35 - "EarthTool.Common"
Cohesion: 0.08
Nodes (16): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.Common, EarthTool.CLI, EarthTool.PAR.Tests.Services, EarthTool.Common.GUI (+8 more)

### Community 36 - "DynamicGltfDocument"
Cohesion: 0.05
Nodes (46): DynamicEditedPreview, DynamicEffectPreview, DynamicImageLayout, DynamicMeshLayout, DynamicObjectScope, DynamicRecordSlice, BinaryWriter, CancellationToken (+38 more)

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.11
Nodes (16): StaticRenderObjectId, bool, Dictionary, ICollection, IEnumerable, int, Matrix4x4, CanonicalTriangle (+8 more)

### Community 38 - "CanonicalDynamicObject"
Cohesion: 0.25
Nodes (12): DynamicAlphaTiming, DynamicEffectType, IEnumerable, Vector3, CanonicalDynamicAlpha, CanonicalDynamicEffectShape, CanonicalDynamicFrameSequence, CanonicalDynamicRecipe (+4 more)

### Community 39 - "InterchangeBaseline"
Cohesion: 0.18
Nodes (11): AnimationReplacement, GltfEditImportOptions, GltfEditImportResult, GltfMetadataConflictResolution, InterchangeBaseline, IReadOnlyList, AnimationReplacement, MetadataConflictResolutionResult (+3 more)

### Community 40 - "ArchiverService"
Cohesion: 0.11
Nodes (13): Stream, ICompressor, ReadOnlySpan, Stream, IDecompressor, PathValidator, Encoding, Encoding (+5 more)

### Community 41 - ".CreateMockHeader"
Cohesion: 0.21
Nodes (6): bool, ReadOnlyMemory, ArchiveItem, Fact, ArchiveItemTests, Guid

### Community 42 - "EarthTool.PAR.Enums"
Cohesion: 0.10
Nodes (7): EarthTool.PAR.Extensions, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Tests.Factories, EarthTool.PAR.Tests.Models, EarthTool.PAR.Factories, EarthTool.PAR.Models

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - ".GenerateSampleData"
Cohesion: 0.24
Nodes (4): Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.14
Nodes (15): AnimationObjectLayout, BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionFingerprint (+7 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.08
Nodes (21): bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, Action, bool (+13 more)

### Community 48 - "IArchiveItem"
Cohesion: 0.10
Nodes (12): DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid, IArchiveFactory, ReadOnlyMemory (+4 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (21): FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo (+13 more)

### Community 51 - "CanonicalMeshAuthoringTests"
Cohesion: 0.07
Nodes (20): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, Fact, InlineData (+12 more)

### Community 52 - "PropertyEditorViewModel"
Cohesion: 0.06
Nodes (28): EarthTool.PAR.GUI.Services, Action, IEnumerable, IPropertyEditorFactory, Action, HashSet, IEnumerable, ILogger (+20 more)

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.17
Nodes (10): ContentFingerprint, BinaryWriter, IEnumerable, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification (+2 more)

### Community 54 - "IReadOnlyList"
Cohesion: 0.18
Nodes (12): IReadOnlyList, ByteArrayComparer, GeometryPartition, ParsedGltfPrimitive, PartitionLayout, ProjectedPartition, StaticGeometryFingerprint, Vector2 (+4 more)

### Community 55 - "StaticMeshAsset"
Cohesion: 0.06
Nodes (38): Discarded, Action, BinaryWriter, IDictionary, IReadOnlyCollection, IReadOnlyDictionary, ISet, List (+30 more)

### Community 56 - "Entity"
Cohesion: 0.06
Nodes (29): EarthTool.PAR.Models.Serialization, Encoding, IBinarySerializable, EntityClassType, EntityGroupType, BinaryReader, IEnumerable, EntityFactory (+21 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - ".BlenderEditsPassOwnershipAwareOracle"
Cohesion: 0.19
Nodes (5): BlenderOutputEvidence, IEnumerable, InlineData, Theory, Trait

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - "EarthTool.CLI.Commands.WD"
Cohesion: 0.24
Nodes (7): Command, EarthTool.CLI.Commands.WD, CancellationToken, CommandContext, ListCommand, WdCommandBase, ListSettings

### Community 62 - "InteractableEntity"
Cohesion: 0.04
Nodes (38): BarrelBetaType, ConnectorType, LookRoundTypeFlags, RepairerCapabilityFlags, ShadowType, TargetType, WeaponFireType, Encoding (+30 more)

### Community 63 - "DestructibleEntity"
Cohesion: 0.05
Nodes (30): ArtifactType, ExplosionFlags, PassiveMask, StandType, StoreableFlags, WasteSize, Encoding, IEnumerable (+22 more)

### Community 64 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.10
Nodes (15): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.Views, NotificationType, Exception, NotificationEventArgs, IServiceCollection (+7 more)

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - ".DetectStaleGuards"
Cohesion: 0.15
Nodes (18): bool, GltfOperationProfile, IEnumerable, int, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList, List (+10 more)

### Community 68 - "DynamicEffectRecipeTests"
Cohesion: 0.42
Nodes (4): Fact, Guid, Task, DynamicEffectRecipeTests

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "official-corpus-qualification.mjs"
Cohesion: 0.14
Nodes (27): assertPrivacySafe(), binaryStages, buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames(), currentPlatform(), fail() (+19 more)

### Community 73 - ".RunAsync"
Cohesion: 0.07
Nodes (27): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+19 more)

### Community 74 - "WdSettings"
Cohesion: 0.25
Nodes (7): CommandSettings, CommonSettings, CancellationToken, CommandContext, DebugCommand, WdSettings, FlagValue

### Community 75 - "AuthoringValidation"
Cohesion: 0.11
Nodes (12): HashSet, IReadOnlyList, List, Vector2, Vector3, AuthoringValidation, CanonicalHorizontalExtents, CanonicalStaticFootprint (+4 more)

### Community 76 - "IArchiver"
Cohesion: 0.28
Nodes (5): CancellationToken, CommandContext, RemoveCommand, RemoveSettings, IArchiver

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.06
Nodes (22): EarthTool.WD.GUI.ViewModels, EarthTool.CLI.Commands.PAR, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Services, EarthTool.Common.Models, EarthTool.WD.Services (+14 more)

### Community 79 - "TreeItemViewModel"
Cohesion: 0.10
Nodes (12): DateTime, int, long, string, ArchiveInfoViewModel, ArchiveItemViewModel, HashSet, bool (+4 more)

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - ".Create"
Cohesion: 0.25
Nodes (6): AnimationLengths, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 82 - ".CreateJson"
Cohesion: 0.17
Nodes (10): AnimationLayout, InterchangeBaseline, IReadOnlyDictionary, NativeProjectionFingerprint, Utf8JsonWriter, GltfPackage, PartitionLayout, PreviewLayout (+2 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.08
Nodes (24): Animations, Blender 4.5 glTF round-trip research, Conclusion, Decision consequences for later tickets, Diagnostic asset, EarthTool metadata requirements, Evidence model, Extras and custom properties (+16 more)

### Community 84 - "MshCanonicalSerializer"
Cohesion: 0.08
Nodes (25): CanonicalStaticRecord, Guid, Matrix4x4, Vector3, MeshArchiveFraming, StaticAnimationTracks, StaticRenderObject, CanonicalStaticVertex (+17 more)

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "EarthTool.TEX"
Cohesion: 0.09
Nodes (18): EarthTool.TEX, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, IServiceCollection, HostExtensions, IEnumerable, TexHeader, TexImage (+10 more)

### Community 88 - "EarthTool.WD.GUI"
Cohesion: 0.29
Nodes (4): EarthTool.WD.GUI, AppBuilder, STAThread, Program

### Community 89 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.10
Nodes (14): bool, object, ObservableCollection, Type, FlagsPropertyEditorViewModel, FlagValueViewModel, Action, bool (+6 more)

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - "IUndoRedoService"
Cohesion: 0.09
Nodes (12): Action, DateTime, UndoAction, Action, IEnumerable, IUndoRedoService, Action, IEnumerable (+4 more)

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "IDisposable"
Cohesion: 0.25
Nodes (5): Type, CommandTypeResolver, IDisposable, IHost, ITypeResolver

### Community 95 - "Runner"
Cohesion: 0.21
Nodes (6): ChannelReader, ChannelWriter, Guid, Task, Runner, ValidatorAggregate

### Community 96 - "ViewLocator"
Cohesion: 0.10
Nodes (11): EarthTool.TEX.GUI, Control, ViewLocator, AppBuilder, STAThread, Program, Control, ViewLocator (+3 more)

### Community 97 - "GltfInterchange"
Cohesion: 0.07
Nodes (11): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, IEnumerable, JsonNode, JsonObject, ReadOnlySpan, GltfInterchange (+3 more)

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
Cohesion: 0.21
Nodes (5): DateTime, Guid, Fact, string, ArchiverServiceTests

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

### Community 108 - "InfoCommand"
Cohesion: 0.38
Nodes (4): CancellationToken, CommandContext, InfoCommand, InfoSettings

### Community 109 - "TexPreviewLoader"
Cohesion: 0.07
Nodes (29): Ambiguous, BinaryReader, byte, CancellationToken, Exception, GltfExportOptions, GltfOperationProfile, ICollection (+21 more)

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

### Community 116 - "EnumPropertyEditorViewModel"
Cohesion: 0.29
Nodes (5): object, ObservableCollection, Type, EnumPropertyEditorViewModel, EnumValueViewModel

### Community 117 - "ManifestFailingFileSystem"
Cohesion: 0.29
Nodes (3): int, Stream, ManifestFailingFileSystem

### Community 118 - ".Execute"
Cohesion: 0.40
Nodes (4): CancellationToken, CommandContext, AddCommand, AddSettings

### Community 119 - ".Execute"
Cohesion: 0.40
Nodes (4): CancellationToken, CommandContext, CreateCommand, CreateSettings

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "MeshAssetLineageId"
Cohesion: 0.24
Nodes (7): Guid, IEnumerable, WalkingSkeletonConsumer, MeshAssetLineageId, MshBuildResult, IEnumerable, MshExpert

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
Cohesion: 0.10
Nodes (15): Writer, Task, IParFileService, ILogger, Task, ParFileService, Encoding, IEnumerable (+7 more)

### Community 128 - "Program"
Cohesion: 0.40
Nodes (3): AppBuilder, STAThread, Program

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "EarthTool.WD.Models"
Cohesion: 0.09
Nodes (16): EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Models, EarthTool.WD.Interfaces, EarthTool.WD.Models, ReadOnlyMemory, IArchiveDataSource, ReadOnlyMemory, InMemoryArchiveDataSource (+8 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 135 - "ConvertCommand"
Cohesion: 0.15
Nodes (13): IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+5 more)

### Community 136 - "Dependabot Dependency Automation"
Cohesion: 0.50
Nodes (4): Dependabot Dependency Automation, Weekly GitHub Actions Updates, Weekly NuGet Updates, Security Check Job

### Community 137 - "ConvertCommand"
Cohesion: 0.22
Nodes (9): CommonCommand, CommonSettings, IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, Settings (+1 more)

### Community 138 - "Setup .NET Environment"
Cohesion: 0.67
Nodes (3): .NET SDK Setup, NuGet Package Cache, Setup .NET Environment

### Community 139 - "Mesh Attachments 1..49"
Cohesion: 0.67
Nodes (3): Trailing Hierarchy Unwind Count, Mesh Attachments 1..49, Mesh Extents

### Community 140 - "StaticSourceObject"
Cohesion: 0.08
Nodes (15): PartitionMatch, IReadOnlyList, AnimationClassBytes, CommonMeshBaseHeader, DynamicObject, SourceObjectId, StaticAnimationClass, StaticRenderObjectFlags (+7 more)

### Community 148 - "QualificationProfiler"
Cohesion: 0.20
Nodes (11): Dictionary, int, long, object, string, ProfileScope, QualificationProfiler, TimingAggregate (+3 more)

### Community 149 - "GltfWalkingSkeletonTests"
Cohesion: 0.09
Nodes (10): Action, Guid, IReadOnlyList, JsonDocument, JsonElement, JsonObject, List, BlenderOutputEvidence (+2 more)

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "CommandTypeRegistrar"
Cohesion: 0.12
Nodes (11): EarthTool.CLI.Commands, Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, CancellationToken, CommandContext (+3 more)

### Community 152 - "ParsedGlb"
Cohesion: 0.08
Nodes (22): string, GltfAnimationHandle, GltfDiagnosticCodes, GltfLightHandle, GltfMaterialHandle, GltfMetadataConflictActions, GltfNewModelAnimationClass, GltfNewModelFootprint (+14 more)

### Community 153 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 154 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 155 - "ParameterReader"
Cohesion: 0.22
Nodes (7): Reader, FileType, IReader, BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 158 - "WdSettings.cs"
Cohesion: 0.20
Nodes (8): CancellationToken, CommandContext, List, ExtractCommand, ExtractSettings, WdMultiSettings, extracted, failed

### Community 159 - "GltfExportOptions"
Cohesion: 0.27
Nodes (7): Guid, IReadOnlyDictionary, IReadOnlyList, GltfDynamicEditImportResult, GltfExportOptions, GltfMetadataConflictCatalog, GltfMetadataIdentity

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Measured Result, Official MSH Qualification Performance, Stage Profiling, Aggregate release qualification, Blender matrix, Official MSH corpus

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 171 - "ParsedGltfAnimationChannel"
Cohesion: 0.23
Nodes (7): float, int, string, ParsedAnimationBuilder, ParsedGltfAnimation, ParsedGltfAnimationChannel, ParsedGltfAnimationObject

### Community 172 - "Migrate From COLLADA To glTF"
Cohesion: 0.22
Nodes (7): Dynamic effect-preview contract, glTF API, API migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 177 - "ItemCommand"
Cohesion: 0.31
Nodes (5): CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings

### Community 178 - "MainWindow"
Cohesion: 0.13
Nodes (10): EarthTool.TEX.GUI.Views, EarthTool.WD.GUI.Views, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs (+2 more)

### Community 182 - "EquipableEntity"
Cohesion: 0.07
Nodes (18): BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags, SpaceStationType (+10 more)

### Community 187 - "Missile"
Cohesion: 0.13
Nodes (9): DamageFlags, HitType, MissileType, RocketType, Encoding, IEnumerable, Missile, Fact (+1 more)

### Community 189 - ".Create"
Cohesion: 0.16
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

## Knowledge Gaps
- **320 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+315 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `EarthTool.WD.Models`, `EarthTool.Common`, `ConvertCommand`, `IValueConverter`, `IEarthInfo`, `EarthTool.TEX`, `CommandTypeRegistrar`, `ParameterReader`, `EarthTool.MSH.Assets`, `ParFile`?**
  _High betweenness centrality (0.182) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `.RunAsync` to `IDisposable`, `OperationResult`, `CanonicalDynamicObject`?**
  _High betweenness centrality (0.120) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `.DetectStaleGuards`, `StaticMeshEditSession`, `CanonicalDynamicObject`, `StaticSourceObject`, `StaticAnimationProjection`, `DynamicEffectExtension`, `StaticMeshAsset`, `ParsedGlb`?**
  _High betweenness centrality (0.112) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _320 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MeshAsset` be split into smaller, more focused modules?**
  _Cohesion score 0.08051948051948052 - nodes in this community are weakly interconnected._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10741971207087486 - nodes in this community are weakly interconnected._