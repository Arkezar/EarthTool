# Graph Report - EarthTool  (2026-08-03)

## Corpus Check
- 358 files · ~252,392 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4182 nodes · 11740 edges · 186 communities (177 shown, 9 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 750 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b7ef6613`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- MeshAsset
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- GltfInterchange
- GltfWalkingSkeletonTests
- .ToByteArray
- EarthTool.Common.GUI.Enums
- IValueConverter
- MainWindowViewModel
- OperationResult
- GltfImportPlanSerializer
- .CreateStaticLightGuards
- MshV1Decoder
- .OpenArchive
- CanonicalDynamicObject
- Dynamic MESH Binary Layout
- InteractableEntity
- DynamicMeshAsset
- release-qualification.mjs
- MainWindowViewModel
- GltfPlanAndReportTests
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
- ArchiverService
- OfficialCorpusQualification
- ICompressor
- StaticMeshEditSession
- OfficialCorpusCliOracle
- EarthTool.PAR.GUI.ViewModels
- .BlenderEditsPassOwnershipAwareOracle
- ArchiverServiceTests
- InMemoryArchiveDataSourceTests
- Static Mesh Header
- StaticObject Record
- ArchiveItemTests
- StaticAnimationProjection
- EntityDetailsViewModel
- IArchiveItem
- PublicApiApproval
- IEarthInfo
- StaticMeshAssetTests
- PropertyEditorViewModel
- .Create
- MetadataEnvelope
- IReadOnlyList
- Entity
- EarthTool.MSH.Tests
- .RewriteStatic
- DialogService
- EarthTool.PAR
- GltfCliReportOperation
- EarthTool.PAR.Enums
- DestructibleEntity
- EarthTool.Common.GUI.ViewModels
- CanonicalMeshAuthoringTests
- EarthTool.PAR.GUI
- .DetectStaleGuards
- .GenerateSampleData
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- official-corpus-qualification.mjs
- Task
- .RunAsync
- Runner
- AuthoringValidation
- IReadOnlyList
- EarthTool.sln
- EarthTool.Common.Interfaces
- TreeItemViewModel
- BinaryExtensions
- TexFile
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
- OperationDiagnostic
- MeshAssetLineageId
- ViewLocator
- KhronosValidatorServer
- EarthTool Suite
- WD Central Directory
- VerticalTransporter
- EarthTool.TEX
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- QualificationProfiler
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
- .CreateCurrentStaticLightGuards
- StaticSourceObject
- EarthTool.TEX
- OneTriangleMshFixture
- .ExportGlbAsync
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- GltfCommandSettings.cs
- ParFile
- ViewLocator
- Base Header
- EarthTool.WD.Models
- UnitTest1.cs
- EarthTool Installation Guide
- StaticRenderObject
- MshOperationProfile
- ConvertCommand
- Dependabot Dependency Automation
- ITransactionalFileSystem
- Setup .NET Environment
- Mesh Attachments 1..49
- ParameterReader
- Code Quality Analysis Job
- Dynamic Color
- .ValidateMetadataGraph
- .Write_And_Read_AreSymmetric
- .EditImportSamplesCubicSplineWithoutPreservingTangents
- package.json
- CommandTypeRegistrar
- GltfContracts.cs
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- App
- Research
- .ToByteArray
- EarthTool.CLI.Commands.WD
- Missile
- validate-glb.mjs
- ItemCommand
- Official MSH Qualification Performance
- ParameterReaderTests
- .WriteReportAsync
- EarthTool.CLI.Tests
- CancellingValidationFileSystem
- ViewLocator
- Migrate From COLLADA To glTF
- PublicCutoverAcceptanceTests
- GltfCliReportSerializer
- .Decode
- IDecompressor
- EarthTool.TEX.GUI/App.axaml.cs
- GltfInterchange.cs
- MshCommandComposition.cs
- IExtractor
- EquipableEntity
- ITexFile
- CountingByteEnumerable
- EarthTool.GLTF/HostExtensions.cs
- ParameterReaderTests.cs
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

## Communities (186 total, 9 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - "MeshAsset"
Cohesion: 0.22
Nodes (11): MeshAsset, CancellationToken, Exception, IEnumerable, ILogger, Stream, Task, MshReader (+3 more)

### Community 2 - "AssetResult"
Cohesion: 0.24
Nodes (7): AssetResult, DiagnosticKey, AssetResult, KhronosValidatorServer, OperationCounts, ProfileScope, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.11
Nodes (15): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+7 more)

### Community 5 - "GltfInterchange"
Cohesion: 0.09
Nodes (10): AnimationReplacement, ICollection, JsonNode, Path, GltfInterchange, ISet, ParsedGlb, NewModelAnimationSet (+2 more)

### Community 6 - "GltfWalkingSkeletonTests"
Cohesion: 0.10
Nodes (4): Guid, BlenderOutputEvidence, GltfWalkingSkeletonTests, Action

### Community 7 - ".ToByteArray"
Cohesion: 0.07
Nodes (23): Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding, IEnumerable (+15 more)

### Community 8 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.09
Nodes (15): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.Views, NotificationType, Exception, NotificationEventArgs, IServiceCollection (+7 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.09
Nodes (10): Task, IParFileService, bool, ILogger, ObservableCollection, ReactiveCommand, string, Task (+2 more)

### Community 11 - "OperationResult"
Cohesion: 0.13
Nodes (15): IReadOnlyList, OperationResult, GltfEditImportOptions, GltfEditImportResult, GltfNewModelImportResult, GltfOperationProfile, InterchangeBaseline, CancellationToken (+7 more)

### Community 12 - "GltfImportPlanSerializer"
Cohesion: 0.11
Nodes (13): CancellationToken, IEnumerable, IReadOnlyDictionary, JsonElement, SeparateGltfPackage, Stream, Task, Utf8JsonWriter (+5 more)

### Community 13 - ".CreateStaticLightGuards"
Cohesion: 0.14
Nodes (9): Action, BinaryWriter, MemoryStream, Quaternion, Vector3, ProjectedAttachment, ProjectedCannonRenderPosition, ProjectedStaticLight (+1 more)

### Community 14 - "MshV1Decoder"
Cohesion: 0.16
Nodes (10): int, IReadOnlyDictionary, List, Matrix4x4, ReadOnlySpan, uint, Vector2, Vector3 (+2 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "CanonicalDynamicObject"
Cohesion: 0.09
Nodes (29): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, ReadOnlySpan, DynamicAlphaTiming (+21 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "InteractableEntity"
Cohesion: 0.04
Nodes (38): BarrelBetaType, ConnectorType, LookRoundTypeFlags, RepairerCapabilityFlags, ShadowType, TargetType, WeaponFireType, Encoding (+30 more)

### Community 19 - "DynamicMeshAsset"
Cohesion: 0.43
Nodes (4): Action, Func, DynamicMeshAsset, MeshAssetKind

### Community 20 - "release-qualification.mjs"
Cohesion: 0.15
Nodes (29): buildEvidence(), collectReceivedFiles(), countDiscoveredTests(), exists(), expectedArtifacts, expectedTestCounts, fail(), forbiddenReleasePaths (+21 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.09
Nodes (16): INotificationService, ITextFlagService, DateTime, int, long, string, ArchiveInfoViewModel, bool (+8 more)

### Community 22 - "GltfPlanAndReportTests"
Cohesion: 0.13
Nodes (14): BufferPath, ConflictKey, Directory, Fact, Guid, InlineData, JsonNode, Task (+6 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.13
Nodes (12): Bitmap, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap, string (+4 more)

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

### Community 28 - ".CreateMockHeader"
Cohesion: 0.15
Nodes (11): IEarthInfoFactory, bool, DateTime, IReadOnlyCollection, MemoryMappedFile, Archive, Fact, ArchiveTests (+3 more)

### Community 29 - "EarthTool.MSH.Assets"
Cohesion: 0.20
Nodes (13): EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.Consumer.Tests, EarthTool.MSH.Tests, EarthTool.GLTF.Internal (+5 more)

### Community 30 - "GlbDocument"
Cohesion: 0.09
Nodes (10): Guid, IDictionary, JsonElement, Matrix4x4, ReadOnlySpan, uint, GlbDocument, GltfImportIntent (+2 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.18
Nodes (11): Asset, Bytes, CancellationToken, Fact, Guid, InlineData, IReadOnlyList, Task (+3 more)

### Community 33 - ".Create"
Cohesion: 0.16
Nodes (9): int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture, OmniRecord (+1 more)

### Community 34 - "ArchiverService"
Cohesion: 0.16
Nodes (15): DateTime, Guid, IArchiveFactory, DateTime, Encoding, Guid, ILogger, ArchiverService (+7 more)

### Community 35 - "OfficialCorpusQualification"
Cohesion: 0.17
Nodes (10): ContentFingerprint, BinaryWriter, IEnumerable, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification (+2 more)

### Community 36 - "ICompressor"
Cohesion: 0.24
Nodes (6): Stream, ICompressor, Encoding, Encoding, ArchiveTestsBase, Fixture

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.12
Nodes (12): StaticRenderObjectId, bool, Dictionary, ICollection, IEnumerable, int, Matrix4x4, Vector2 (+4 more)

### Community 38 - "OfficialCorpusCliOracle"
Cohesion: 0.20
Nodes (9): CliProcessResult, CliReportOperation, IReadOnlyList, Task, CliDiagnostic, CliOracleResult, CliProcessResult, CliReportOperation (+1 more)

### Community 39 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.06
Nodes (30): EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, EntityClassType, Faction, ResearchType, ObservableCollection, EntityGroupNodeViewModel (+22 more)

### Community 40 - ".BlenderEditsPassOwnershipAwareOracle"
Cohesion: 0.18
Nodes (5): BlenderOutputEvidence, IEnumerable, InlineData, Theory, Trait

### Community 41 - "ArchiverServiceTests"
Cohesion: 0.15
Nodes (11): CancellationToken, CommandContext, AddCommand, CancellationToken, CommandContext, DateTime, Guid, IArchiver (+3 more)

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - "ArchiveItemTests"
Cohesion: 0.19
Nodes (5): bool, ReadOnlyMemory, ArchiveItem, Fact, ArchiveItemTests

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.15
Nodes (15): AnimationObjectLayout, BinaryWriter, InterchangeBaseline, IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionFingerprint (+7 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.08
Nodes (20): bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, Action, bool (+12 more)

### Community 48 - "IArchiveItem"
Cohesion: 0.08
Nodes (15): Type, CommandTypeResolver, DateTime, Encoding, IReadOnlyCollection, IArchive, ReadOnlyMemory, IArchiveItem (+7 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (20): FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo (+12 more)

### Community 51 - "StaticMeshAssetTests"
Cohesion: 0.10
Nodes (13): Fact, IEnumerable, InlineData, Task, Theory, StaticMeshAssetTests, int, IReadOnlyList (+5 more)

### Community 52 - "PropertyEditorViewModel"
Cohesion: 0.05
Nodes (36): EarthTool.PAR.GUI.Services, Action, IEnumerable, IPropertyEditorFactory, Action, HashSet, IEnumerable, ILogger (+28 more)

### Community 53 - ".Create"
Cohesion: 0.23
Nodes (6): AnimationLengths, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 54 - "MetadataEnvelope"
Cohesion: 0.08
Nodes (34): Discarded, PartitionMatch, ImportPlanException, IReadOnlyList, Projection, Version, ByteArrayComparer, GeometryPartition (+26 more)

### Community 55 - "IReadOnlyList"
Cohesion: 0.10
Nodes (21): IDictionary, IReadOnlyCollection, IReadOnlyDictionary, IReadOnlyList, ISet, List, Matrix4x4, Quaternion (+13 more)

### Community 56 - "Entity"
Cohesion: 0.08
Nodes (23): EarthTool.PAR.Models.Serialization, EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List, ValidationError, ValidationResult (+15 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - ".RewriteStatic"
Cohesion: 0.18
Nodes (9): CanonicalStaticVertex, CanonicalTriangle, StaticRenderObjectAddition, IEnumerable, IReadOnlyDictionary, IReadOnlyList, ISet, TriangleCount (+1 more)

### Community 59 - "DialogService"
Cohesion: 0.22
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - "GltfCliReportOperation"
Cohesion: 0.24
Nodes (11): GltfExportReceipt, NativeProjectionFingerprint, int, IReadOnlyList, string, GltfCliReport, GltfCliReportFormat, GltfCliReportOperation (+3 more)

### Community 62 - "EarthTool.PAR.Enums"
Cohesion: 0.10
Nodes (7): EarthTool.PAR.Extensions, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Tests.Factories, EarthTool.PAR.Tests.Models, EarthTool.PAR.Factories, EarthTool.PAR.Models

### Community 63 - "DestructibleEntity"
Cohesion: 0.05
Nodes (30): ArtifactType, ExplosionFlags, PassiveMask, StandType, StoreableFlags, WasteSize, Encoding, IEnumerable (+22 more)

### Community 64 - "EarthTool.Common.GUI.ViewModels"
Cohesion: 0.14
Nodes (10): EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, ReactiveCommand, Unit, AboutViewModel, ViewModelBase, ParAboutViewModel, TexAboutViewModel (+2 more)

### Community 65 - "CanonicalMeshAuthoringTests"
Cohesion: 0.28
Nodes (4): Fact, Guid, Task, CanonicalMeshAuthoringTests

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - ".DetectStaleGuards"
Cohesion: 0.16
Nodes (17): bool, GltfOperationProfile, IEnumerable, int, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList, List (+9 more)

### Community 68 - ".GenerateSampleData"
Cohesion: 0.24
Nodes (4): Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "official-corpus-qualification.mjs"
Cohesion: 0.14
Nodes (27): assertPrivacySafe(), binaryStages, buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames(), currentPlatform(), fail() (+19 more)

### Community 72 - "Task"
Cohesion: 0.12
Nodes (4): Fact, JsonDocument, JsonElement, Task

### Community 73 - ".RunAsync"
Cohesion: 0.08
Nodes (25): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+17 more)

### Community 74 - "Runner"
Cohesion: 0.21
Nodes (6): ChannelReader, ChannelWriter, Guid, Task, Runner, ValidatorAggregate

### Community 75 - "AuthoringValidation"
Cohesion: 0.11
Nodes (11): HashSet, IReadOnlyList, List, AuthoringValidation, CanonicalHorizontalExtents, CanonicalStaticFootprint, CanonicalStaticRenderObject, CanonicalStaticSourceObject (+3 more)

### Community 76 - "IReadOnlyList"
Cohesion: 0.22
Nodes (7): DecodedStaticRecord, IReadOnlyList, DecodedStaticRecord, StaticHierarchy, StaticSourceBuilder, StaticHierarchy, StaticSourceBuilder

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.06
Nodes (21): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Services, EarthTool.Common, EarthTool.Common.Models, EarthTool.WD.Services (+13 more)

### Community 79 - "TreeItemViewModel"
Cohesion: 0.15
Nodes (7): ArchiveItemViewModel, HashSet, bool, Guid, ObservableCollection, TreeItemViewModel, ViewModelBase

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - "TexFile"
Cohesion: 0.24
Nodes (8): BinaryReader, IEnumerable, TexFile, TexHeader, BinaryReader, IEnumerable, SKBitmap, TexImage

### Community 82 - "StaticMeshAsset"
Cohesion: 0.11
Nodes (12): AnimationLayout, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, NativeProjectionFingerprint, Utf8JsonWriter, GltfPackage, StaticMeshAsset (+4 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.08
Nodes (24): Animations, Blender 4.5 glTF round-trip research, Conclusion, Decision consequences for later tickets, Diagnostic asset, EarthTool metadata requirements, Evidence model, Extras and custom properties (+16 more)

### Community 84 - "MshCanonicalSerializer"
Cohesion: 0.14
Nodes (9): CanonicalStaticRecord, AnimationClassBytes, MeshArchiveFraming, Encoding, Guid, int, List, Matrix4x4 (+1 more)

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "ConvertCommand"
Cohesion: 0.27
Nodes (7): IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, IReader, Settings

### Community 88 - "ParsedGltfAnimationChannel"
Cohesion: 0.29
Nodes (5): int, string, ParsedAnimationBuilder, ParsedGltfAnimationChannel, float

### Community 89 - "FlagsPropertyEditorViewModel"
Cohesion: 0.23
Nodes (6): bool, object, ObservableCollection, Type, FlagsPropertyEditorViewModel, FlagValueViewModel

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

### Community 94 - "OperationDiagnostic"
Cohesion: 0.08
Nodes (10): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, GltfMetadataConflictResolution, IEnumerable, JsonObject, ReadOnlySpan, MetadataConflictResolutionResult (+2 more)

### Community 95 - "MeshAssetLineageId"
Cohesion: 0.16
Nodes (10): Guid, IEnumerable, WalkingSkeletonConsumer, Guid, MeshAssetLineageId, Guid, DynamicMeshBuilder, MshBuildResult (+2 more)

### Community 96 - "ViewLocator"
Cohesion: 0.17
Nodes (7): EarthTool.TEX.GUI, AppBuilder, STAThread, Program, Control, ViewLocator, IDataTemplate

### Community 97 - "KhronosValidatorServer"
Cohesion: 0.18
Nodes (8): List, KhronosValidatorServer, ValidatorResult, IAsyncDisposable, Process, ValidatorCode, ValidatorResult, ValueTask

### Community 98 - "EarthTool Suite"
Cohesion: 0.22
Nodes (10): EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management, MSH Model Export Workflow (+2 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "VerticalTransporter"
Cohesion: 0.12
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 101 - "EarthTool.TEX"
Cohesion: 0.10
Nodes (10): EarthTool.TEX, EarthTool.PAR, EarthTool.CLI, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, EarthTool.CLI.Commands, CommonSettings, IServiceCollection (+2 more)

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

### Community 106 - "QualificationProfiler"
Cohesion: 0.20
Nodes (11): Dictionary, int, long, object, string, ProfileScope, QualificationProfiler, TimingAggregate (+3 more)

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

### Community 119 - "StaticSourceObject"
Cohesion: 0.14
Nodes (11): IReadOnlyList, CommonMeshBaseHeader, DynamicObject, SourceObjectId, StaticAnimationClass, StaticRenderObjectFlags, StaticSourceObject, CanonicalStaticObjectRole (+3 more)

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

### Community 126 - "GltfCommandSettings.cs"
Cohesion: 0.22
Nodes (13): AsyncCommand, CancellationToken, CommandContext, Task, ExportGltfCommand, ImportEditGltfCommand, ImportNewGltfCommand, Guid (+5 more)

### Community 127 - "ParFile"
Cohesion: 0.15
Nodes (10): Encoding, IBinarySerializable, ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile (+2 more)

### Community 128 - "ViewLocator"
Cohesion: 0.18
Nodes (6): EarthTool.WD.GUI, AppBuilder, STAThread, Program, Control, ViewLocator

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "EarthTool.WD.Models"
Cohesion: 0.10
Nodes (16): EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Models, EarthTool.WD.Interfaces, EarthTool.WD.Models, ReadOnlyMemory, IArchiveDataSource, ReadOnlyMemory, InMemoryArchiveDataSource (+8 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - "StaticRenderObject"
Cohesion: 0.19
Nodes (10): byte, Matrix4x4, Vector3, StaticAnimationTracks, StaticRenderObject, StaticAnimationReplacement, ReadOnlySpan, Vector3 (+2 more)

### Community 134 - "MshOperationProfile"
Cohesion: 0.14
Nodes (14): CancellationToken, FailingMshWriter, CancellationToken, Stream, Task, CancellationToken, Stream, string (+6 more)

### Community 135 - "ConvertCommand"
Cohesion: 0.20
Nodes (10): CommonCommand, CommonSettings, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+2 more)

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

### Community 140 - "ParameterReader"
Cohesion: 0.26
Nodes (6): Reader, FileType, BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 143 - ".ValidateMetadataGraph"
Cohesion: 0.24
Nodes (6): CarrierKind, ICollection, Path, Value, MetadataConflictException, Envelope

### Community 148 - ".Write_And_Read_AreSymmetric"
Cohesion: 0.29
Nodes (5): Writer, Fact, ParameterWriterTests, Encoding, ParTestData

### Community 149 - ".EditImportSamplesCubicSplineWithoutPreservingTangents"
Cohesion: 0.18
Nodes (5): Action, IReadOnlyList, JsonObject, List, JsonArray

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "CommandTypeRegistrar"
Cohesion: 0.22
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 152 - "GltfContracts.cs"
Cohesion: 0.10
Nodes (23): Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfAnimationHandle, GltfDiagnosticCodes, GltfExportOptions, GltfLightHandle (+15 more)

### Community 153 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 154 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 155 - "Research"
Cohesion: 0.31
Nodes (6): IDictionary, IEnumerable, ParameterEntry, IEnumerable, Research, TreeNode

### Community 156 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 158 - "EarthTool.CLI.Commands.WD"
Cohesion: 0.07
Nodes (31): Command, CommandSettings, EarthTool.CLI.Commands.WD, CreateCommand, CancellationToken, CommandContext, DebugCommand, CancellationToken (+23 more)

### Community 159 - "Missile"
Cohesion: 0.13
Nodes (9): DamageFlags, HitType, MissileType, RocketType, Encoding, IEnumerable, Missile, Fact (+1 more)

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - "ItemCommand"
Cohesion: 0.25
Nodes (6): EarthTool.CLI.Commands.PAR, CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Measured Result, Official MSH Qualification Performance, Stage Profiling, Aggregate release qualification, Blender matrix, Official MSH corpus

### Community 167 - ".WriteReportAsync"
Cohesion: 0.15
Nodes (5): EarthTool.CLI.Commands.MSH, Stream, CliReportFileSystem, ICliReportFileSystem, Exception

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 170 - "CancellingValidationFileSystem"
Cohesion: 0.15
Nodes (6): byte, CancellationTokenSource, Stream, CancellingReadStream, CancellingValidationFileSystem, FailingCommitFileSystem

### Community 171 - "ViewLocator"
Cohesion: 0.18
Nodes (6): EarthTool.PAR.GUI, AppBuilder, STAThread, Program, Control, ViewLocator

### Community 172 - "Migrate From COLLADA To glTF"
Cohesion: 0.25
Nodes (6): glTF API, API migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 173 - "PublicCutoverAcceptanceTests"
Cohesion: 0.29
Nodes (5): CliResult, Fact, Task, CliResult, PublicCutoverAcceptanceTests

### Community 175 - ".Decode"
Cohesion: 0.31
Nodes (5): MeshAssetOrigin, CancellationToken, Guid, IEnumerable, MshDecodeResult

### Community 177 - "IDecompressor"
Cohesion: 0.33
Nodes (3): ReadOnlySpan, Stream, IDecompressor

### Community 178 - "EarthTool.TEX.GUI/App.axaml.cs"
Cohesion: 0.13
Nodes (10): EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs (+2 more)

### Community 179 - "GltfInterchange.cs"
Cohesion: 0.48
Nodes (6): Exception, AmbiguousPartitionCorrespondenceException, MetadataIdentityException, ResourceLimitException, StaleNativeProjectionException, StaticLightMetadataException

### Community 180 - "MshCommandComposition.cs"
Cohesion: 0.33
Nodes (3): EarthTool.MSH, IServiceCollection, HostExtensions

### Community 181 - "IExtractor"
Cohesion: 0.33
Nodes (3): Task, IExtractor, IWDExtractor

### Community 182 - "EquipableEntity"
Cohesion: 0.07
Nodes (18): BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags, SpaceStationType (+10 more)

### Community 183 - "ITexFile"
Cohesion: 0.40
Nodes (5): IEnumerable, TexHeader, TexImage, ITexFile, TexReader

### Community 184 - "CountingByteEnumerable"
Cohesion: 0.40
Nodes (4): int, CountingByteEnumerable, IEnumerable, IEnumerator

### Community 187 - "ParameterReaderTests.cs"
Cohesion: 0.24
Nodes (6): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.PAR.Tests.Services, IServiceCollection, HostExtensions

### Community 189 - ".Create"
Cohesion: 0.17
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

## Knowledge Gaps
- **317 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+312 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **9 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `EarthTool.WD.Models`, `EarthTool.TEX`, `IValueConverter`, `ParameterReader`, `IEarthInfo`, `EarthTool.MSH.Assets`?**
  _High betweenness centrality (0.176) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `.DetectStaleGuards`, `MshOperationProfile`, `StaticAnimationProjection`, `CanonicalDynamicObject`, `GltfInterchange.cs`, `MetadataEnvelope`, `StaticSourceObject`, `GltfContracts.cs`, `GltfCliReportOperation`?**
  _High betweenness centrality (0.113) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `.RunAsync` to `IArchiveItem`, `GltfImportPlanSerializer`?**
  _High betweenness centrality (0.112) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _317 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10741971207087486 - nodes in this community are weakly interconnected._
- **Should `GltfInterchange` be split into smaller, more focused modules?**
  _Cohesion score 0.09180327868852459 - nodes in this community are weakly interconnected._