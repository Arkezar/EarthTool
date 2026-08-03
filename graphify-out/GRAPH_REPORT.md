# Graph Report - EarthTool  (2026-08-03)

## Corpus Check
- 363 files · ~278,189 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4534 nodes · 13277 edges · 181 communities (176 shown, 5 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 840 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `dd71661b`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- .WriteFileAsync
- AssetResult
- FramedMshBaseHeaderTests
- .Compress
- GltfPlanAndReportTests
- GltfWalkingSkeletonTests
- .ToByteArray
- .ResolveAndLoad
- IValueConverter
- MainWindowViewModel
- OperationResult
- DynamicGltfInterchangeTests
- .CreateStaticLightGuards
- MshV1Decoder
- .OpenArchive
- DynamicEffectSemantics
- Dynamic MESH Binary Layout
- .CreateEffectPreview
- GltfImportPlanSerializer
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
- GltfInterchange
- EarthTool.Common
- IReadOnlyList
- StaticMeshEditSession
- InteractableEntity
- Equipment
- ArchiverService
- .GenerateSampleData
- .EditImportSamplesCubicSplineWithoutPreservingTangents
- Static Mesh Header
- StaticObject Record
- WDExtractorTests
- StaticAnimationProjection
- EntityDetailsViewModel
- ParFile
- PublicApiApproval
- IEarthInfo
- CanonicalMeshAuthoringTests
- GltfPlanAndReport.cs
- OfficialCorpusQualification
- IReadOnlyList
- StaticMeshAsset
- Entity
- EarthTool.MSH.Tests
- GltfCommandSettings.cs
- DialogService
- EarthTool.PAR
- DynamicGltfDocument
- EarthTool.PAR.Enums
- DestructibleEntity
- NotificationService
- WdSettings.cs
- EarthTool.PAR.GUI
- .DetectStaleGuards
- IArchiveItem
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- EntityGroup
- TexPreviewLoader
- Task
- .BlenderEditsPassOwnershipAwareOracle
- OperationDiagnostic
- GltfOperationProfile
- EarthTool.sln
- EarthTool.Common.Interfaces
- MeshAssetLineageId
- BinaryExtensions
- .Create
- .CreateJson
- Blender 4.5 glTF round-trip research
- MshCanonicalSerializer
- OfficialCorpusQualificationTests
- EarthTool.Common.GUI
- .RewriteStatic
- DynamicEffectExtension
- ResearchReferenceCollectionEditorViewModel
- EarthTool WD Archive Manager
- IUndoRedoService
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- EarthTool.Common.GUI.Enums
- QualificationProfiler
- ViewLocator
- .Create
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
- Runner
- .Decode
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- EnumPropertyEditorViewModel
- .WriteReportAsync
- .WriteUInt32
- .ValidateMetadataGraph
- EarthTool.TEX
- MshBuildResult
- .ExportGlbAsync
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- PropertyEditorViewModel
- .LoadPreview
- Base Header
- EarthTool.WD.Models
- UnitTest1.cs
- EarthTool Installation Guide
- Program
- ParameterReader
- ConvertCommand
- Dependabot Dependency Automation
- ConvertCommand
- Setup .NET Environment
- Mesh Attachments 1..49
- MshOperationProfile
- Code Quality Analysis Job
- Dynamic Color
- FlagsPropertyEditorViewModel
- MeshAsset
- Task
- package.json
- CommandTypeRegistrar
- GltfContracts.cs
- Q: analyze complexity of @EarthTool.TEX/TexReader.cs
- App
- EarthTool.TEX.GUI/App.axaml.cs
- CommonCommand
- StaticMeshAsset.cs
- EarthTool.TEX
- validate-glb.mjs
- PassiveEntity
- .Resolve
- Official MSH Qualification Performance
- .CreateStatic
- EarthTool.PAR.Factories
- .Reconcile
- EarthTool.CLI.Tests
- OneTriangleMshFixture
- GlbDocument.cs
- ResolutionBudget
- .DeserializeAsync
- TransactionalFileSystem
- .ToByteArray
- .Write_And_Read_AreSymmetric
- Archive
- StaticHierarchy
- EquipableEntity
- .Create

## God Nodes (most connected - your core abstractions)
1. `GltfWalkingSkeletonTests` - 179 edges
2. `GltfInterchange` - 173 edges
3. `GlbDocument` - 138 edges
4. `DynamicGltfDocument` - 111 edges
5. `EarthTool.PAR.Enums` - 90 edges
6. `OperationDiagnostic` - 89 edges
7. `DynamicGltfInterchangeTests` - 87 edges
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

## Communities (181 total, 5 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - ".WriteFileAsync"
Cohesion: 0.15
Nodes (11): ITransactionalFileSystem, IMshValidator, CancellationToken, Exception, IEnumerable, ILogger, Stream, Task (+3 more)

### Community 2 - "AssetResult"
Cohesion: 0.29
Nodes (6): AssetResult, DiagnosticKey, AssetResult, KhronosValidatorServer, OperationCounts, WorkerContext

### Community 3 - "FramedMshBaseHeaderTests"
Cohesion: 0.06
Nodes (30): Diagnostics, Asset, CancellationToken, CancellationTokenSource, Exception, Fact, Func, Guid (+22 more)

### Community 4 - ".Compress"
Cohesion: 0.10
Nodes (16): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+8 more)

### Community 5 - "GltfPlanAndReportTests"
Cohesion: 0.13
Nodes (14): BufferPath, ConflictKey, Directory, Fact, Guid, InlineData, JsonNode, Task (+6 more)

### Community 6 - "GltfWalkingSkeletonTests"
Cohesion: 0.10
Nodes (4): Guid, BlenderOutputEvidence, GltfWalkingSkeletonTests, Action

### Community 7 - ".ToByteArray"
Cohesion: 0.08
Nodes (21): Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding, IEnumerable (+13 more)

### Community 8 - ".ResolveAndLoad"
Cohesion: 0.16
Nodes (12): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IReadOnlyDictionary, IReadOnlyList, Vector3, MshPreviewLoader (+4 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.09
Nodes (11): bool, ILogger, ObservableCollection, ReactiveCommand, string, Task, Unit, MainWindowViewModel (+3 more)

### Community 11 - "OperationResult"
Cohesion: 0.12
Nodes (20): IReadOnlyList, OperationResult, GltfDynamicEditImportResult, GltfEditImportOptions, GltfEditImportResult, GltfExportReceipt, GltfMeshEditImportResult, GltfMetadataConflictResolution (+12 more)

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.06
Nodes (35): DynamicAlphaTiming, DynamicEffectType, DynamicLightType, DynamicMeshAsset, EffectRectangle, IEnumerable, Vector3, CanonicalDynamicAlpha (+27 more)

### Community 13 - ".CreateStaticLightGuards"
Cohesion: 0.16
Nodes (7): BinaryWriter, MemoryStream, Quaternion, Vector3, ProjectedAttachment, ProjectedCannonRenderPosition, ProjectedStaticLight

### Community 14 - "MshV1Decoder"
Cohesion: 0.16
Nodes (11): int, IReadOnlyDictionary, List, Matrix4x4, ReadOnlySpan, uint, Vector2, Vector3 (+3 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.15
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "DynamicEffectSemantics"
Cohesion: 0.17
Nodes (9): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, Fact, Guid (+1 more)

### Community 17 - "Dynamic MESH Binary Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - ".CreateEffectPreview"
Cohesion: 0.13
Nodes (11): DynamicEditedPreview, DynamicEffectPreview, ReadOnlySpan, Vector2, Vector3, DynamicEditedPreview, DynamicEffectPreview, DynamicPreviewException (+3 more)

### Community 19 - "GltfImportPlanSerializer"
Cohesion: 0.19
Nodes (6): IEnumerable, JsonElement, SeparateGltfPackage, GltfImportPlanSerializer, ImportPlanException, JsonValueKind

### Community 20 - "release-qualification.mjs"
Cohesion: 0.07
Nodes (62): corpusBinaryStages, corpusInterchangeStages, recognizedDynamicEffectTypes, assertPrivacySafe(), buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames() (+54 more)

### Community 21 - "MainWindowViewModel"
Cohesion: 0.08
Nodes (20): DateTime, int, long, string, ArchiveInfoViewModel, bool, HashSet, ILogger (+12 more)

### Community 22 - "EarthTool.PAR.GUI.ViewModels"
Cohesion: 0.06
Nodes (25): EarthTool.PAR.GUI, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views, Faction, ResearchType, ValidationError (+17 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.08
Nodes (16): Bitmap, IEnumerable, Task, IDialogService, INotificationService, ILogger, int, List (+8 more)

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
Cohesion: 0.12
Nodes (8): bool, ReadOnlyMemory, ArchiveItem, Fact, ArchiveItemTests, Fact, ArchiveTests, Guid

### Community 29 - "EarthTool.MSH.Assets"
Cohesion: 0.08
Nodes (34): CliResult, EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.CLI.Commands.MSH, EarthTool.MSH (+26 more)

### Community 30 - "GlbDocument"
Cohesion: 0.08
Nodes (12): GltfOperationProfile, Guid, IDictionary, JsonDocument, JsonElement, Matrix4x4, ReadOnlySpan, uint (+4 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Asset, byte, Bytes, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - ".Create"
Cohesion: 0.16
Nodes (9): int, IReadOnlyCollection, IReadOnlyDictionary, Vector3, OmniRecord, SpotRecord, StaticLightMshFixture, OmniRecord (+1 more)

### Community 34 - "GltfInterchange"
Cohesion: 0.07
Nodes (17): AnimationReplacement, ICollection, IReadOnlyList, JsonNode, JsonObject, Path, AnimationReplacement, GltfInterchange (+9 more)

### Community 35 - "EarthTool.Common"
Cohesion: 0.13
Nodes (10): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.Common, EarthTool.CLI, EarthTool.PAR.Tests.Services, Identifiers (+2 more)

### Community 36 - "IReadOnlyList"
Cohesion: 0.22
Nodes (10): DynamicImageLayout, DynamicMeshLayout, DynamicObjectScope, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList, NativeProjectionFingerprint, Vector2 (+2 more)

### Community 37 - "StaticMeshEditSession"
Cohesion: 0.08
Nodes (30): IReadOnlyCollection, PartitionMatch, StaticHierarchyPlan, SourceObjectId, StaticRenderObjectId, StaticSourceObject, bool, Dictionary (+22 more)

### Community 38 - "InteractableEntity"
Cohesion: 0.08
Nodes (19): BarrelBetaType, ShadowType, TargetType, WeaponFireType, Encoding, IEnumerable, InteractableEntity, Encoding (+11 more)

### Community 39 - "Equipment"
Cohesion: 0.09
Nodes (18): LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable, Equipment (+10 more)

### Community 40 - "ArchiverService"
Cohesion: 0.09
Nodes (18): Encoding, DateTime, Guid, IArchiveFactory, Stream, ICompressor, ReadOnlySpan, Stream (+10 more)

### Community 41 - ".GenerateSampleData"
Cohesion: 0.24
Nodes (4): Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 42 - ".EditImportSamplesCubicSplineWithoutPreservingTangents"
Cohesion: 0.18
Nodes (5): Action, IReadOnlyList, JsonObject, List, JsonArray

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - "WDExtractorTests"
Cohesion: 0.25
Nodes (9): DateTime, Guid, Fact, string, Task, WDExtractorTests, ILogger, Task (+1 more)

### Community 46 - "StaticAnimationProjection"
Cohesion: 0.27
Nodes (5): Matrix4x4, Quaternion, Vector3, ProjectedAnimationFrame, StaticAnimationProjection

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.07
Nodes (26): EntityClassType, bool, Dictionary, EditableEntity, bool, Dictionary, EditableResearch, Action (+18 more)

### Community 48 - "ParFile"
Cohesion: 0.15
Nodes (10): Task, IParFileService, ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile (+2 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (20): FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo (+12 more)

### Community 51 - "CanonicalMeshAuthoringTests"
Cohesion: 0.07
Nodes (21): Fact, Guid, int, Task, CanonicalMeshAuthoringTests, CountingByteEnumerable, Fact, IEnumerable (+13 more)

### Community 52 - "GltfPlanAndReport.cs"
Cohesion: 0.21
Nodes (9): int, IReadOnlyList, string, Utf8JsonWriter, GltfCliReport, GltfCliReportFormat, GltfCliReportOperationKind, GltfCliReportSerializer (+1 more)

### Community 53 - "OfficialCorpusQualification"
Cohesion: 0.17
Nodes (10): ContentFingerprint, BinaryWriter, IEnumerable, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification (+2 more)

### Community 54 - "IReadOnlyList"
Cohesion: 0.11
Nodes (15): AnimationObjectLayout, IReadOnlyList, NativeProjectionFingerprint, AnimationLayout, ByteArrayComparer, GeometryPartition, ParsedGltfPrimitive, PartitionLayout (+7 more)

### Community 55 - "StaticMeshAsset"
Cohesion: 0.08
Nodes (22): Discarded, Action, BinaryWriter, IDictionary, IReadOnlyDictionary, ISet, List, Matrix4x4 (+14 more)

### Community 56 - "Entity"
Cohesion: 0.09
Nodes (21): EarthTool.PAR.Models.Serialization, EntityGroupType, BinaryReader, IEnumerable, EntityFactory, List, ValidationResult, IEnumerable (+13 more)

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
Cohesion: 0.11
Nodes (13): BinaryWriter, float, int, Stream, string, uint, Utf8JsonWriter, DynamicGltfDocument (+5 more)

### Community 62 - "EarthTool.PAR.Enums"
Cohesion: 0.11
Nodes (4): EarthTool.PAR.Extensions, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Models

### Community 63 - "DestructibleEntity"
Cohesion: 0.05
Nodes (30): DamageFlags, ExplosionFlags, HitType, MissileType, RocketType, StandType, StoreableFlags, WasteSize (+22 more)

### Community 64 - "NotificationService"
Cohesion: 0.19
Nodes (7): NotificationType, Exception, NotificationEventArgs, Exception, ILogger, NotificationService, EventArgs

### Community 65 - "WdSettings.cs"
Cohesion: 0.07
Nodes (29): Command, CommandSettings, CommonSettings, CancellationToken, CommandContext, DebugCommand, CancellationToken, CommandContext (+21 more)

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - ".DetectStaleGuards"
Cohesion: 0.15
Nodes (19): MetadataConflictException, bool, GltfOperationProfile, IEnumerable, int, InterchangeBaseline, IReadOnlyDictionary, IReadOnlyList (+11 more)

### Community 68 - "IArchiveItem"
Cohesion: 0.13
Nodes (10): DateTime, IReadOnlyCollection, IArchive, ReadOnlyMemory, IArchiveItem, ITextFlagService, HashSet, TextFlagService (+2 more)

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "EntityGroup"
Cohesion: 0.12
Nodes (11): EarthTool.CLI.Commands.PAR, CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings, Encoding, IBinarySerializable (+3 more)

### Community 72 - "TexPreviewLoader"
Cohesion: 0.17
Nodes (11): byte, CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IReadOnlyDictionary, IReadOnlyList, DynamicTexPreviewLoadResult (+3 more)

### Community 73 - "Task"
Cohesion: 0.07
Nodes (27): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+19 more)

### Community 74 - ".BlenderEditsPassOwnershipAwareOracle"
Cohesion: 0.18
Nodes (5): BlenderOutputEvidence, IEnumerable, InlineData, Theory, Trait

### Community 75 - "OperationDiagnostic"
Cohesion: 0.12
Nodes (8): IReadOnlyDictionary, OperationDiagnostic, IEnumerable, ReadOnlySpan, ParseScopeResolution, HashSet, List, AuthoringValidation

### Community 76 - "GltfOperationProfile"
Cohesion: 0.14
Nodes (11): CancellationToken, GltfOperationProfile, ICollection, ISet, JsonDocument, JsonElement, ReadOnlyMemory, DynamicGltfImport (+3 more)

### Community 77 - "EarthTool.sln"
Cohesion: 0.11
Nodes (21): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.Consumer.Tests, net8.0, Microsoft.NET.Sdk (+13 more)

### Community 78 - "EarthTool.Common.Interfaces"
Cohesion: 0.05
Nodes (24): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Services, EarthTool.Common.Models, EarthTool.CLI.Commands.WD, EarthTool.WD.Services (+16 more)

### Community 79 - "MeshAssetLineageId"
Cohesion: 0.20
Nodes (10): DecodedStaticRecord, Guid, MeshAssetLineageId, MeshAssetOrigin, CancellationToken, Guid, IEnumerable, IReadOnlyList (+2 more)

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - ".Create"
Cohesion: 0.23
Nodes (6): AnimationLengths, IReadOnlyList, Matrix4x4, Vector3, AnimationLengths, StaticAnimationMshFixture

### Community 82 - ".CreateJson"
Cohesion: 0.12
Nodes (12): AnimationLayout, Action, IEnumerable, InterchangeBaseline, IReadOnlyDictionary, Utf8JsonWriter, GltfPackage, PartitionLayout (+4 more)

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.06
Nodes (31): Dynamic effect-preview contract, glTF API, Animations, Blender 4.5 glTF round-trip research, Conclusion, Decision consequences for later tickets, Diagnostic asset, EarthTool metadata requirements (+23 more)

### Community 84 - "MshCanonicalSerializer"
Cohesion: 0.20
Nodes (7): Encoding, int, IReadOnlyList, Matrix4x4, Vector3, MshCanonicalSerializer, RewrittenStaticRecord

### Community 85 - "OfficialCorpusQualificationTests"
Cohesion: 0.34
Nodes (4): Fact, Task, Trait, OfficialCorpusQualificationTests

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - ".RewriteStatic"
Cohesion: 0.16
Nodes (8): IDictionary, IEnumerable, IReadOnlyDictionary, ISet, ReadOnlySpan, RewrittenStaticRecord, TriangleCount, VertexCount

### Community 88 - "DynamicEffectExtension"
Cohesion: 0.21
Nodes (8): IEnumerable, DynamicObjectScope, IReadOnlyList, ReadOnlySpan, Vector3, CommonMeshBaseHeader, DynamicEffectExtension, DynamicObject

### Community 89 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.23
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

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

### Community 94 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.09
Nodes (17): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, EarthTool.Common.GUI.Views, IServiceCollection, ServiceCollectionExtensions (+9 more)

### Community 95 - "QualificationProfiler"
Cohesion: 0.10
Nodes (17): DynamicCoverage, Dictionary, IDictionary, int, IReadOnlyDictionary, ISet, long, object (+9 more)

### Community 96 - "ViewLocator"
Cohesion: 0.07
Nodes (15): EarthTool.TEX.GUI, EarthTool.WD.GUI, Control, ViewLocator, AppBuilder, STAThread, Program, Control (+7 more)

### Community 97 - ".Create"
Cohesion: 0.31
Nodes (8): BinaryWriter, InterchangeBaseline, IReadOnlyList, AnimationProjectionFingerprint, AnimationProjectionSet, ProjectedAnimationClip, ProjectedAnimationObject, AnimationObjectLayout

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
Cohesion: 0.13
Nodes (14): CancellationToken, CommandContext, AddCommand, CancellationToken, CommandContext, CreateCommand, AddSettings, CreateSettings (+6 more)

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
Cohesion: 0.19
Nodes (9): List, KhronosValidatorServer, ValidatorResult, WorkerContext, IAsyncDisposable, Process, ValidatorCode, ValidatorResult (+1 more)

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - "Runner"
Cohesion: 0.20
Nodes (6): ChannelReader, ChannelWriter, Guid, Task, Runner, ProfileScope

### Community 109 - ".Decode"
Cohesion: 0.14
Nodes (9): BinaryReader, IEnumerable, int, long, TexHeader, TexResolutionBudget, TexFlags, HasVariants (+1 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.13
Nodes (15): CancellationToken, Func, IEnumerable, IReadOnlyList, Task, TextWriter, GltfCommandExecutor, OperationStatus (+7 more)

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

### Community 118 - ".WriteUInt32"
Cohesion: 0.20
Nodes (3): CanonicalHorizontalExtents, CanonicalStaticFootprint, StaticMeshBuilder

### Community 119 - ".ValidateMetadataGraph"
Cohesion: 0.21
Nodes (5): CarrierKind, ICollection, Path, Value, Envelope

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "MshBuildResult"
Cohesion: 0.26
Nodes (6): Guid, IEnumerable, WalkingSkeletonConsumer, MshBuildResult, IEnumerable, MshExpert

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
Cohesion: 0.13
Nodes (14): CliProcessResult, CliReportOperation, DiagnosticSeverity, IReadOnlyList, JsonElement, string, Task, CliBatchOracleResult (+6 more)

### Community 127 - "PropertyEditorViewModel"
Cohesion: 0.13
Nodes (16): Action, IEnumerable, IPropertyEditorFactory, Action, HashSet, IEnumerable, ILogger, Type (+8 more)

### Community 128 - ".LoadPreview"
Cohesion: 0.27
Nodes (5): Exception, PreviewResolution, PreviewResolution, PreviewResolutionKind, TexResolutionBudget

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "EarthTool.WD.Models"
Cohesion: 0.08
Nodes (18): EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Models, EarthTool.WD.Interfaces, EarthTool.WD.Models, ReadOnlyMemory, IArchiveDataSource, ReadOnlyMemory, InMemoryArchiveDataSource (+10 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - "Program"
Cohesion: 0.50
Nodes (3): AppBuilder, STAThread, Program

### Community 134 - "ParameterReader"
Cohesion: 0.24
Nodes (7): Reader, FileType, IReader, BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 135 - "ConvertCommand"
Cohesion: 0.14
Nodes (13): IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings (+5 more)

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

### Community 140 - "MshOperationProfile"
Cohesion: 0.22
Nodes (9): CancellationToken, Stream, Task, CancellationToken, Stream, Task, IMshReader, IMshWriter (+1 more)

### Community 143 - "FlagsPropertyEditorViewModel"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 148 - "MeshAsset"
Cohesion: 0.31
Nodes (4): Action, Func, MeshAsset, MeshAssetKind

### Community 149 - "Task"
Cohesion: 0.12
Nodes (4): Fact, JsonDocument, JsonElement, Task

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - "CommandTypeRegistrar"
Cohesion: 0.11
Nodes (11): EarthTool.CLI.Commands, Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, Type, CommandTypeResolver (+3 more)

### Community 152 - "GltfContracts.cs"
Cohesion: 0.09
Nodes (24): Guid, IReadOnlyDictionary, IReadOnlyList, string, GltfAnimationHandle, GltfDiagnosticCodes, GltfExportOptions, GltfLightHandle (+16 more)

### Community 153 - "Q: analyze complexity of @EarthTool.TEX/TexReader.cs"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: analyze complexity of @EarthTool.TEX/TexReader.cs, Source Nodes

### Community 154 - "App"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 155 - "EarthTool.TEX.GUI/App.axaml.cs"
Cohesion: 0.12
Nodes (10): EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs (+2 more)

### Community 156 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 158 - "StaticMeshAsset.cs"
Cohesion: 0.26
Nodes (8): byte, Matrix4x4, AnimationClassBytes, StaticAnimationClass, StaticAnimationTracks, StaticRenderObject, StaticRenderObjectFlags, StaticAnimationReplacement

### Community 159 - "EarthTool.TEX"
Cohesion: 0.09
Nodes (18): EarthTool.TEX, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, IServiceCollection, HostExtensions, IEnumerable, TexHeader, TexImage (+10 more)

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - "PassiveEntity"
Cohesion: 0.17
Nodes (9): ArtifactType, PassiveMask, Encoding, IEnumerable, PassiveEntity, Encoding, IEnumerable, Artifact (+1 more)

### Community 164 - ".Resolve"
Cohesion: 0.29
Nodes (5): Func, IEnumerable, IReadOnlyList, SafeResourceLookup, SafeResourceMatch

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Historical Measured Result, Official MSH Qualification Performance, Stage Profiling, Aggregate release qualification, Blender matrix, Official MSH corpus

### Community 166 - ".CreateStatic"
Cohesion: 0.25
Nodes (4): CanonicalStaticRecord, MeshArchiveFraming, Guid, List

### Community 167 - "EarthTool.PAR.Factories"
Cohesion: 0.24
Nodes (5): EarthTool.PAR.Tests.Factories, EarthTool.PAR.Tests.Models, EarthTool.PAR.Factories, Fact, EntityFactoryTests

### Community 168 - ".Reconcile"
Cohesion: 0.27
Nodes (3): DynamicRecordSlice, IDictionary, NativeObjectGraph

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 171 - "GlbDocument.cs"
Cohesion: 0.11
Nodes (20): ImportPlanException, DynamicMetadataIdentityException, float, int, string, MalformedMetadataException, MetadataAnimationClass, MetadataAnimationProjection (+12 more)

### Community 172 - "ResolutionBudget"
Cohesion: 0.25
Nodes (4): IEnumerable, int, long, ResolutionBudget

### Community 173 - ".DeserializeAsync"
Cohesion: 0.40
Nodes (4): CancellationToken, IReadOnlyDictionary, Stream, Task

### Community 175 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 176 - ".Write_And_Read_AreSymmetric"
Cohesion: 0.19
Nodes (7): Writer, Fact, ParameterReaderTests, Fact, ParameterWriterTests, Encoding, ParTestData

### Community 177 - "Archive"
Cohesion: 0.29
Nodes (6): bool, DateTime, IReadOnlyCollection, MemoryMappedFile, Archive, SortedSet

### Community 178 - "StaticHierarchy"
Cohesion: 0.38
Nodes (3): StaticHierarchy, StaticSourceBuilder, StaticSourceBuilder

### Community 182 - "EquipableEntity"
Cohesion: 0.07
Nodes (19): BuildingExType, BuildingTabType, BuildingType, ConnectorType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags (+11 more)

### Community 189 - ".Create"
Cohesion: 0.17
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

## Knowledge Gaps
- **320 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+315 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **5 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `EarthTool.WD.Models`, `EarthTool.Common`, `ParameterReader`, `ConvertCommand`, `IValueConverter`, `Archive`, `IEarthInfo`, `CommandTypeRegistrar`, `EarthTool.MSH.Assets`, `EarthTool.TEX`?**
  _High betweenness centrality (0.157) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `Task` to `DynamicGltfInterchangeTests`, `IArchiveItem`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **Why does `MainWindowViewModel` connect `MainWindowViewModel` to `IArchiveItem`, `EntityDetailsViewModel`, `ParFile`, `MainWindowViewModel`, `Entity`, `IUndoRedoService`, `EarthTool.Common.GUI.Enums`?**
  _High betweenness centrality (0.101) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _320 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FramedMshBaseHeaderTests` be split into smaller, more focused modules?**
  _Cohesion score 0.06265984654731457 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10465116279069768 - nodes in this community are weakly interconnected._
- **Should `GltfPlanAndReportTests` be split into smaller, more focused modules?**
  _Cohesion score 0.13076923076923078 - nodes in this community are weakly interconnected._