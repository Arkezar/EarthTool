# Graph Report - EarthTool  (2026-08-07)

## Corpus Check
- 382 files · ~256,813 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4479 nodes · 12003 edges · 204 communities (195 shown, 9 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 663 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `85af5e13`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- blender-qualification.mjs
- MeshAsset
- AssetResult
- .Create
- .Compress
- CancellingTransactionalFileSystem
- MshOperationProfile
- GltfImportPlanSerializer
- .ResolveAndLoad
- IValueConverter
- MainWindowViewModel
- OperationResult
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
- .ExportGlbFileAsync
- Vehicle
- Common MSH Base Header
- EarthTool.WD.GUI/App.axaml.cs
- .CreateMockHeader
- ConvertCommand
- EarthTool.TEX
- EarthTool.CLI
- DynamicMeshAssetTests
- GltfWalkingSkeletonTests
- EquipableEntity
- JsonElement
- RecordingLogger
- JsonObject
- StaticRenderObject
- CanonicalAuthoringMetadata
- DynamicEffectExtension
- MshCanonicalSerializer
- GltfPlanAndReport.cs
- Static Mesh Header
- StaticObject Record
- DynamicGltfDocument
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
- .Decode
- MainWindowViewModel
- CanonicalGltfMetadataContractTests
- MshDecodeContext
- .Match
- EarthTool.PAR.GUI
- ArchiveItem
- InteractableEntity
- EarthTool.TEX.GUI
- EarthTool.WD.GUI
- ITransactionalFileSystem
- Equipment
- Task
- TexPreviewLoader
- EditableEntity
- UndoRedoService
- EarthTool.sln
- EarthTool.Common.Interfaces
- ArchiverServiceTests
- BinaryExtensions
- .ExportGlbAsync
- Program
- Blender 4.5 glTF round-trip research
- StaticMeshSequenceFixture
- .RunAsync
- EarthTool.Common.GUI
- CanonicalStaticRenderObjectSequenceEncoder
- CanonicalStaticMeshAssemblyInput
- 0003-create-immutable-msh-assets-from-gltf.md
- EarthTool WD Archive Manager
- .CreatePackage
- EarthTool.WD.Tests
- EarthTool.WD Test Suite
- TexPreview
- EarthTool.Common.GUI.Enums
- EarthTool.Common.GUI.ViewModels
- EarthTool Suite
- WD Central Directory
- EarthTool.PAR.Enums
- .Write_And_Read_AreSymmetric
- EarthTool Documentation
- EarthTool.Common
- Entity
- DestructibleEntity
- .IsFinite
- Reader
- .Decode
- GltfInterchange
- GltfCommandExecutor
- glTF .NET foundation research
- Detect Changes Job
- Unified CI Pipeline
- Conventional Commits
- WD Archive Commands
- ParsedGltfAnimationChannel
- DynamicEffectRecipeTests
- .GenerateSampleData
- IArchiveItem
- EarthTool.TEX
- VerticalTransporter
- ConvertCommand
- EarthTool.TEX.Tests
- EarthTool
- Static Light
- OfficialCorpusCliOracle
- IReadOnlyList
- MeshAssetAuthoring.cs
- Base Header
- PAR/ConvertCommand.cs
- UnitTest1.cs
- EarthTool Installation Guide
- CommandTypeRegistrar
- StaticMeshDecoder
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
- .Resolve
- Migrate From COLLADA To glTF
- PropertyEditorViewModel
- FlagsPropertyEditorViewModel
- GlbDocument
- GltfOperationProfile
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
- .CollectNewModelAnimationPaths
- ItemCommand
- ResolutionBudget
- KhronosValidatorServer
- InMemoryArchiveDataSourceTests
- ViewLocator
- ResearchReferenceCollectionEditorViewModel
- .CreateBinary
- GltfSourceLossDiagnosticsTests
- .GetMinimumSerializedLength
- .AssembleCanonicalStaticParsed
- EnumPropertyEditorViewModel
- ParameterReaderTests
- Metadata Envelope Reference
- ManifestFailingFileSystem
- ParFile
- DynamicObject
- ArchiveInfoViewModel
- IntCollectionPropertyEditorViewModel
- CanonicalStaticGltfCreationOptions
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
- .ReadFloatAccessor
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

## Communities (204 total, 9 thin omitted)

### Community 0 - "blender-qualification.mjs"
Cohesion: 0.15
Nodes (21): archiveSuffix(), buildEvidence(), compareVersions(), currentPlatform(), deduplicateBuilds(), download(), expectedOwnershipOutcomes, findExecutable() (+13 more)

### Community 1 - "MeshAsset"
Cohesion: 0.22
Nodes (11): MeshAsset, CancellationToken, Exception, IEnumerable, ILogger, Stream, Task, MshReader (+3 more)

### Community 2 - "AssetResult"
Cohesion: 0.20
Nodes (10): AssetResult, DiagnosticKey, CliDiagnostic, Task, AssetResult, KhronosValidatorServer, OperationCounts, ProfileScope (+2 more)

### Community 3 - ".Create"
Cohesion: 0.12
Nodes (16): Diagnostics, Asset, CancellationToken, Fact, Guid, InlineData, IReadOnlyList, Task (+8 more)

### Community 4 - ".Compress"
Cohesion: 0.11
Nodes (15): ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream, DecompressorService, Fact (+7 more)

### Community 5 - "CancellingTransactionalFileSystem"
Cohesion: 0.12
Nodes (8): CancellationTokenSource, int, Stream, CancellingTransactionalFileSystem, ControlledReadStream, CorruptingTransactionalFileSystem, FaultingWriteStream, MemoryStream

### Community 6 - "MshOperationProfile"
Cohesion: 0.14
Nodes (10): HashSet, IReadOnlyList, List, AuthoringValidation, MshBuildResult, IEnumerable, MshExpert, Matrix4x4 (+2 more)

### Community 7 - "GltfImportPlanSerializer"
Cohesion: 0.27
Nodes (5): IEnumerable, JsonElement, GltfImportPlanSerializer, ImportPlanException, JsonValueKind

### Community 8 - ".ResolveAndLoad"
Cohesion: 0.15
Nodes (14): CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, IReadOnlyDictionary, IReadOnlyList, Vector2, Vector3 (+6 more)

### Community 9 - "IValueConverter"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "MainWindowViewModel"
Cohesion: 0.08
Nodes (15): Task, IParFileService, ObservableCollection, EntityGroupsRootNodeViewModel, bool, ILogger, ObservableCollection, ReactiveCommand (+7 more)

### Community 11 - "OperationResult"
Cohesion: 0.19
Nodes (7): IReadOnlyList, OperationResult, GltfOperationProfile, CancellationToken, Stream, Task, SeparateGltfPackage

### Community 12 - "DynamicGltfInterchangeTests"
Cohesion: 0.12
Nodes (10): Fact, IEnumerable, InlineData, JsonDocument, JsonElement, Task, Theory, Vector2 (+2 more)

### Community 13 - "CanonicalStaticGltfCreationTests"
Cohesion: 0.11
Nodes (19): Action, Fact, Guid, IEnumerable, InlineData, IReadOnlyList, JsonNode, JsonObject (+11 more)

### Community 14 - "EarthTool.MSH.Assets"
Cohesion: 0.07
Nodes (37): EarthTool.MSH.Services, EarthTool.MSH.Authoring, EarthTool.MSH.Internal, EarthTool.MSH.Operations, EarthTool.GLTF, EarthTool.CLI.Commands.MSH, EarthTool.MSH, EarthTool.Consumer.Tests (+29 more)

### Community 15 - ".OpenArchive"
Cohesion: 0.16
Nodes (10): ArchiveTestsBase, BinaryReader, DateTime, Guid, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory (+2 more)

### Community 16 - "CanonicalDynamicGltfImporter"
Cohesion: 0.11
Nodes (20): CanonicalDynamicGraph, CanonicalDynamicNode, CanonicalDynamicPreview, CancellationToken, GltfNewModelImportOptions, GltfOperationProfile, Guid, ICollection (+12 more)

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
Nodes (62): corpusBinaryStages, corpusInterchangeStages, recognizedDynamicEffectTypes, assertPrivacySafe(), buildEvidence(), canonicalDiagnostics(), canonicalValidatorCodes(), collectPrivateNames() (+54 more)

### Community 21 - "DynamicEffectType"
Cohesion: 0.21
Nodes (16): AuthoredEffect, DynamicAuthoringValues, DynamicAlphaTiming, DynamicEffectType, EffectRectangle, IEnumerable, Vector3, CanonicalDynamicAlpha (+8 more)

### Community 22 - ".Create"
Cohesion: 0.25
Nodes (9): Action, Fact, Guid, IEnumerable, InlineData, JsonObject, Task, Theory (+1 more)

### Community 23 - "MainWindowViewModel"
Cohesion: 0.13
Nodes (12): Bitmap, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap, string (+4 more)

### Community 24 - ".ExportGlbFileAsync"
Cohesion: 0.06
Nodes (10): Stream, ITransactionalFileSystem, TransactionalFileSystem, CancellationTokenSource, Stream, CancellingAfterSidecarTransactionalFileSystem, CorruptingSidecarTransactionalFileSystem, FailingManifestTransactionalFileSystem (+2 more)

### Community 25 - "Vehicle"
Cohesion: 0.08
Nodes (18): VehicleObjectType, Encoding, IEnumerable, Builder, Encoding, IEnumerable, Harvester, Encoding (+10 more)

### Community 26 - "Common MSH Base Header"
Cohesion: 0.10
Nodes (23): Model MSH Framing and Record Extensions Explicitly, Canonical Next Record Markers, MSH Footprint API, MSH Horizontal Extents API, IMeshBaseHeader, Legacy MSH Model Migration, MSH API, MSH Slots API (+15 more)

### Community 27 - "EarthTool.WD.GUI/App.axaml.cs"
Cohesion: 0.06
Nodes (19): Application, EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, EarthTool.WD.GUI.Views, IServiceCollection, App, Task, MainWindow (+11 more)

### Community 28 - ".CreateMockHeader"
Cohesion: 0.13
Nodes (14): ResourceType, Guid, Stream, IEarthInfoFactory, bool, DateTime, IReadOnlyCollection, MemoryMappedFile (+6 more)

### Community 29 - "ConvertCommand"
Cohesion: 0.27
Nodes (7): IEnumerable, JsonSerializerOptions, SKBitmap, Task, ConvertCommand, IReader, Settings

### Community 30 - "EarthTool.TEX"
Cohesion: 0.13
Nodes (10): EarthTool.TEX, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, IServiceCollection, HostExtensions, IEnumerable, TexHeader, TexImage (+2 more)

### Community 31 - "EarthTool.CLI"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 32 - "DynamicMeshAssetTests"
Cohesion: 0.10
Nodes (17): Bytes, Asset, byte, CancellationToken, CancellationTokenSource, Fact, Guid, InlineData (+9 more)

### Community 33 - "GltfWalkingSkeletonTests"
Cohesion: 0.08
Nodes (5): Fact, JsonDocument, JsonElement, Vector3, GltfWalkingSkeletonTests

### Community 34 - "EquipableEntity"
Cohesion: 0.12
Nodes (10): MaxShieldUpgradeType, PositionType, Encoding, IEnumerable, EquipableEntity, Encoding, Platoon, Encoding (+2 more)

### Community 35 - "JsonElement"
Cohesion: 0.18
Nodes (4): GltfOperationProfile, JsonDocument, JsonElement, GltfImportIntent

### Community 36 - "RecordingLogger"
Cohesion: 0.21
Nodes (9): EventId, Exception, Func, IDisposable, List, RecordingLogger, ILogger, Level (+1 more)

### Community 37 - "JsonObject"
Cohesion: 0.20
Nodes (5): Action, IReadOnlyList, JsonObject, List, JsonArray

### Community 38 - "StaticRenderObject"
Cohesion: 0.19
Nodes (13): IDictionary, byte, IReadOnlyList, Matrix4x4, Vector3, MeshAssetKind, StaticAnimationClass, StaticAnimationTracks (+5 more)

### Community 39 - "CanonicalAuthoringMetadata"
Cohesion: 0.06
Nodes (34): Carrier, bool, GltfOperationProfile, GltfStaticObjectRoles, HashSet, IEnumerable, int, IReadOnlyDictionary (+26 more)

### Community 40 - "DynamicEffectExtension"
Cohesion: 0.06
Nodes (31): Vector3, DynamicEffectEvaluationContext, DynamicEffectSemantics, DynamicFrameSelection, DynamicSemanticFailure, DynamicTextureRegion, ReadOnlySpan, DynamicEffectExtension (+23 more)

### Community 41 - "MshCanonicalSerializer"
Cohesion: 0.16
Nodes (8): Encoding, Guid, int, IReadOnlyList, ReadOnlySpan, Vector3, MshCanonicalSerializer, ReadOnlyListCopyExtensions

### Community 42 - "GltfPlanAndReport.cs"
Cohesion: 0.22
Nodes (10): Guid, int, IReadOnlyList, string, Utf8JsonWriter, GltfCliReport, GltfCliReportFormat, GltfCliReportOperationKind (+2 more)

### Community 43 - "Static Mesh Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "StaticObject Record"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 45 - "DynamicGltfDocument"
Cohesion: 0.15
Nodes (12): DynamicEffectPreview, float, int, uint, Vector2, Vector3, DynamicAnimationLayout, DynamicAnimationTrack (+4 more)

### Community 46 - "ProjectedAnimationFrame"
Cohesion: 0.23
Nodes (10): IReadOnlyList, Matrix4x4, Quaternion, Vector3, AnimationProjectionSet, ProjectedAnimationClip, ProjectedAnimationFrame, ProjectedAnimationObject (+2 more)

### Community 47 - "EntityDetailsViewModel"
Cohesion: 0.13
Nodes (12): Action, bool, IEnumerable, ILogger, ObservableCollection, ReactiveCommand, string, Type (+4 more)

### Community 48 - "Vector3"
Cohesion: 0.13
Nodes (11): float, Matrix4x4, Quaternion, Translation, Vector3, AttachmentHeadingProjection, ProjectedAttachment, ProjectedCannon (+3 more)

### Community 49 - "PublicApiApproval"
Cohesion: 0.13
Nodes (11): IEnumerable, Type, PublicApiApproval, Fact, Stream, Task, FailingTransactionalFileSystem, SafeMshWalkingSkeletonTests (+3 more)

### Community 50 - "IEarthInfo"
Cohesion: 0.09
Nodes (17): FileFlags, Encoding, Guid, Stream, EarthInfoFactory, Guid, IEarthInfo, Encoding (+9 more)

### Community 51 - "CanonicalMeshAuthoringTests"
Cohesion: 0.16
Nodes (5): Fact, Guid, IReadOnlyDictionary, Task, CanonicalMeshAuthoringTests

### Community 52 - ".WriteReportAsync"
Cohesion: 0.17
Nodes (4): Stream, CliReportFileSystem, ICliReportFileSystem, Exception

### Community 53 - "PublicCutoverAcceptanceTests"
Cohesion: 0.21
Nodes (7): CliResult, Fact, Task, CliResult, PublicCutoverAcceptanceTests, GeneratedRegex, Regex

### Community 54 - ".ToByteArray"
Cohesion: 0.12
Nodes (9): Encoding, Encoding, Encoding, Encoding, Encoding, Encoding, Encoding, Fact (+1 more)

### Community 55 - "EarthTool.PAR.Models.Abstracts"
Cohesion: 0.04
Nodes (44): EarthTool.PAR.Extensions, EarthTool.PAR.Models.Abstracts, ExplosionFlags, StandType, StoreableFlags, WasteSize, Encoding, IEnumerable (+36 more)

### Community 56 - "Entity"
Cohesion: 0.06
Nodes (30): Encoding, IBinarySerializable, EntityClassType, EntityGroupType, Faction, BinaryReader, IEnumerable, EntityFactory (+22 more)

### Community 57 - "EarthTool.MSH.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.MSH.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 58 - "CanonicalBaseHeaderEncoder"
Cohesion: 0.10
Nodes (22): CornerPassageMaps, Exception, MetadataResourceLimitException, RequiredTextureResourceBindingException, ResourceLimitException, StaticLightMetadataException, AnimationClassBytes, byte (+14 more)

### Community 59 - "DialogService"
Cohesion: 0.19
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 60 - "EarthTool.PAR"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - ".Decode"
Cohesion: 0.12
Nodes (10): BinaryReader, IEnumerable, int, long, TexHeader, TexResolutionBudget, TexFlags, TexHeader (+2 more)

### Community 62 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (12): IEnumerable, INotificationService, ITextFlagService, bool, ILogger, object, ObservableCollection, ReactiveCommand (+4 more)

### Community 63 - "CanonicalGltfMetadataContractTests"
Cohesion: 0.24
Nodes (7): Action, Fact, IEnumerable, JsonNode, JsonObject, Task, CanonicalGltfMetadataContractTests

### Community 64 - "MshDecodeContext"
Cohesion: 0.18
Nodes (9): int, DynamicMeshDecoder, CancellationToken, IReadOnlyList, List, ReadOnlySpan, MshDecodeContext, MshContentException (+1 more)

### Community 66 - "EarthTool.PAR.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "ArchiveItem"
Cohesion: 0.12
Nodes (13): ReadOnlyMemory, IArchiveDataSource, bool, ReadOnlyMemory, ArchiveItem, int, MemoryMappedFile, ReadOnlyMemory (+5 more)

### Community 68 - "InteractableEntity"
Cohesion: 0.07
Nodes (19): BarrelBetaType, ShadowType, TargetType, WeaponFireType, Encoding, IEnumerable, InteractableEntity, Encoding (+11 more)

### Community 69 - "EarthTool.TEX.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "EarthTool.WD.GUI"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "ITransactionalFileSystem"
Cohesion: 0.19
Nodes (3): Stream, ITransactionalFileSystem, TransactionalFileSystem

### Community 72 - "Equipment"
Cohesion: 0.07
Nodes (19): ConnectorType, LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable (+11 more)

### Community 73 - "Task"
Cohesion: 0.06
Nodes (30): CliFixture, Action, CancellationToken, IEnumerable, int, IServiceCollection, Task, TextWriter (+22 more)

### Community 74 - "TexPreviewLoader"
Cohesion: 0.22
Nodes (7): byte, CancellationToken, GltfExportOptions, GltfOperationProfile, ICollection, PreviewResolutionKind, TexPreviewLoader

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
Nodes (26): EarthTool.WD.Tests, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Factories, EarthTool.WD.Tests.Services, EarthTool.Common.Models, EarthTool.WD.Services, EarthTool.WD (+18 more)

### Community 79 - "ArchiverServiceTests"
Cohesion: 0.14
Nodes (12): CancellationToken, CommandContext, CancellationToken, CommandContext, CreateCommand, CreateSettings, DateTime, Guid (+4 more)

### Community 80 - "BinaryExtensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - ".ExportGlbAsync"
Cohesion: 0.17
Nodes (6): InlineData, JsonNode, Task, Theory, IReadOnlyCollection, IReadOnlyDictionary

### Community 82 - "Program"
Cohesion: 0.40
Nodes (3): AppBuilder, STAThread, Program

### Community 83 - "Blender 4.5 glTF round-trip research"
Cohesion: 0.15
Nodes (13): Animations, Blender 4.5 glTF round-trip research, Conclusion, Evidence model, Extras and custom properties, JSON value behavior, Meshes, primitives, and topology, Nodes, hierarchy, scenes, and transforms (+5 more)

### Community 84 - "StaticMeshSequenceFixture"
Cohesion: 0.12
Nodes (12): Fact, InlineData, Task, Theory, StaticMeshAssetTests, int, IReadOnlyList, Matrix4x4 (+4 more)

### Community 85 - ".RunAsync"
Cohesion: 0.38
Nodes (4): Fact, Task, OfficialCorpusQualificationTests, Trait

### Community 86 - "EarthTool.Common.GUI"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "CanonicalStaticRenderObjectSequenceEncoder"
Cohesion: 0.19
Nodes (12): CanonicalStaticRecord, CanonicalStaticRenderObject, CanonicalStaticSourceObject, StaticAnimationReplacement, ICollection, int, IReadOnlyDictionary, IReadOnlyList (+4 more)

### Community 88 - "CanonicalStaticMeshAssemblyInput"
Cohesion: 0.50
Nodes (4): Guid, IReadOnlyDictionary, Vector3, CanonicalStaticMeshAssemblyInput

### Community 90 - "EarthTool WD Archive Manager"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 91 - ".CreatePackage"
Cohesion: 0.15
Nodes (10): DynamicAnimationLayout, DynamicAnimationTrack, DynamicImageLayout, DynamicMeshLayout, DynamicObjectScope, BinaryWriter, IReadOnlyDictionary, IReadOnlyList (+2 more)

### Community 92 - "EarthTool.WD.Tests"
Cohesion: 0.12
Nodes (17): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.WD.Tests, net8.0 (+9 more)

### Community 93 - "EarthTool.WD Test Suite"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 95 - "TexPreview"
Cohesion: 0.20
Nodes (10): Exception, IReadOnlyDictionary, IReadOnlyList, DynamicTexPreviewLoadResult, PreviewResolution, TexPreview, TexPreviewLoadResult, PreviewResolution (+2 more)

### Community 96 - "EarthTool.Common.GUI.Enums"
Cohesion: 0.09
Nodes (15): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.Views, NotificationType, Exception, NotificationEventArgs, IServiceCollection (+7 more)

### Community 97 - "EarthTool.Common.GUI.ViewModels"
Cohesion: 0.17
Nodes (9): EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, ReactiveCommand, Unit, AboutViewModel, ViewModelBase, ParAboutViewModel, TexAboutViewModel (+1 more)

### Community 98 - "EarthTool Suite"
Cohesion: 0.22
Nodes (10): EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management, MSH Model Export Workflow (+2 more)

### Community 99 - "WD Central Directory"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "EarthTool.PAR.Enums"
Cohesion: 0.06
Nodes (23): EarthTool.PAR.Enums, EarthTool.PAR.Tests.Factories, EarthTool.PAR.Tests.Models, EarthTool.PAR.Factories, ArtifactType, BuildingExType, BuildingTabType, BuildingType (+15 more)

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

### Community 106 - ".IsFinite"
Cohesion: 0.17
Nodes (9): GltfNewModelStaticLightOptions, IReadOnlyCollection, Matrix4x4, Quaternion, Translation, Vector3, ParsedGltfLight, Heading (+1 more)

### Community 107 - "Reader"
Cohesion: 0.32
Nodes (9): dump(), dump_dynamic_record(), dump_object(), main(), Path, read_base_header(), Reader, rotate_footprint_slot() (+1 more)

### Community 108 - ".Decode"
Cohesion: 0.23
Nodes (9): Guid, MeshArchiveFraming, MeshAssetOrigin, IReadOnlyDictionary, CancellationToken, IReadOnlyList, uint, MshDecodeResult (+1 more)

### Community 109 - "GltfInterchange"
Cohesion: 0.11
Nodes (13): IReadOnlyDictionary, DiagnosticSeverity, OperationDiagnostic, Func, ICollection, IEnumerable, IReadOnlyList, ReadOnlySpan (+5 more)

### Community 110 - "GltfCommandExecutor"
Cohesion: 0.14
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

### Community 116 - "ParsedGltfAnimationChannel"
Cohesion: 0.31
Nodes (5): int, string, ParsedAnimationBuilder, ParsedGltfAnimationChannel, ParsedGltfAnimationObject

### Community 117 - "DynamicEffectRecipeTests"
Cohesion: 0.42
Nodes (4): Fact, Guid, Task, DynamicEffectRecipeTests

### Community 118 - ".GenerateSampleData"
Cohesion: 0.13
Nodes (6): Fact, ArchiveItemTests, Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 119 - "IArchiveItem"
Cohesion: 0.05
Nodes (39): Type, CommandTypeResolver, DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid (+31 more)

### Community 120 - "EarthTool.TEX"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "VerticalTransporter"
Cohesion: 0.09
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 122 - "ConvertCommand"
Cohesion: 0.12
Nodes (16): CommonCommand, CommonSettings, IDictionary, IEnumerable, JsonSerializerOptions, string, Task, ConvertCommand (+8 more)

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
Cohesion: 0.15
Nodes (11): CliProcessResult, CliReportOperation, IEnumerable, IReadOnlyList, string, Task, CliBatchOracleResult, CliOracleResult (+3 more)

### Community 127 - "IReadOnlyList"
Cohesion: 0.15
Nodes (9): AnimationObjectLayout, IReadOnlyList, AnimationLayout, ParsedGltfMesh, ParsedGltfPrimitive, ProjectedPartition, Vector2, RenderVertex (+1 more)

### Community 128 - "MeshAssetAuthoring.cs"
Cohesion: 0.12
Nodes (11): Guid, IEnumerable, Vector2, Vector3, CanonicalHorizontalExtents, CanonicalStaticFootprint, CanonicalStaticObjectRole, CanonicalStaticVertex (+3 more)

### Community 129 - "Base Header"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "PAR/ConvertCommand.cs"
Cohesion: 0.15
Nodes (9): EarthTool.CLI.Commands.PAR, EarthTool.PAR.Models.Serialization, JsonSerializerOptions, Type, Utf8JsonWriter, EntityConverter, TypeReader, JsonConverter (+1 more)

### Community 131 - "UnitTest1.cs"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation Guide"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - "CommandTypeRegistrar"
Cohesion: 0.24
Nodes (6): Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, ITypeRegistrar

### Community 134 - "StaticMeshDecoder"
Cohesion: 0.20
Nodes (10): DecodedStaticRecord, IReadOnlyList, List, Vector3, DecodedStaticRecord, StaticHierarchy, StaticMeshDecoder, StaticSourceBuilder (+2 more)

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
Cohesion: 0.26
Nodes (6): Reader, FileType, BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 143 - "EarthTool.CLI.Commands.WD"
Cohesion: 0.06
Nodes (33): Command, CommandSettings, EarthTool.CLI.Commands.WD, EarthTool.CLI.Commands, CommonSettings, AddCommand, CancellationToken, CommandContext (+25 more)

### Community 148 - "EarthTool.PAR.Models"
Cohesion: 0.06
Nodes (25): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.GUI, EarthTool.PAR, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.Common, EarthTool.PAR.GUI.Services, EarthTool.CLI (+17 more)

### Community 149 - "Mesh Artist Quick Start And Cheat Sheet"
Cohesion: 0.15
Nodes (13): Attachment Identifier Cheat Sheet, Choose The Correct Workflow, Create The Canonical MSH, Directional Empty Presentation In Blender, Edit The Scene, Export An MSH, Export From Blender, Fast Checks Before Import (+5 more)

### Community 150 - "package.json"
Cohesion: 0.18
Nodes (10): gltf-validator, devDependencies, gltf-validator, name, private, scripts, qualify:corpus, qualify:release (+2 more)

### Community 151 - ".Resolve"
Cohesion: 0.29
Nodes (5): Func, IEnumerable, IReadOnlyList, SafeResourceLookup, SafeResourceMatch

### Community 152 - "Migrate From COLLADA To glTF"
Cohesion: 0.33
Nodes (6): API migration, Attachment helper name migration, CLI migration, Last COLLADA release, Migrate From COLLADA To glTF, Workflow migration

### Community 153 - "PropertyEditorViewModel"
Cohesion: 0.21
Nodes (9): Action, IEnumerable, IPropertyEditorFactory, bool, ReactiveCommand, string, Type, Unit (+1 more)

### Community 154 - "FlagsPropertyEditorViewModel"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 155 - "GlbDocument"
Cohesion: 0.10
Nodes (14): IEnumerable, IReadOnlyDictionary, Span, uint, Utf8JsonWriter, GlbDocument, GltfPackage, PreviewLayout (+6 more)

### Community 156 - "GltfOperationProfile"
Cohesion: 0.33
Nodes (3): GltfOperationProfile, ISet, JsonElement

### Community 158 - "PropertyEditorFactory"
Cohesion: 0.27
Nodes (7): Action, HashSet, IEnumerable, ILogger, Type, PropertyEditorFactory, PropertyInfo

### Community 159 - "OfficialCorpusQualification"
Cohesion: 0.16
Nodes (10): ContentFingerprint, BinaryWriter, IEnumerable, IReadOnlyList, Vector3, ContentFingerprint, DiagnosticKey, OfficialCorpusQualification (+2 more)

### Community 162 - "validate-glb.mjs"
Cohesion: 0.64
Nodes (6): hasIssues(), main(), parseOptions(), runServer(), summarizeValidatorReport(), validateFile()

### Community 163 - "TreeItemViewModel"
Cohesion: 0.24
Nodes (5): HashSet, bool, Guid, ObservableCollection, TreeItemViewModel

### Community 164 - "Decision consequences for later tickets"
Cohesion: 0.40
Nodes (5): Decision consequences for later tickets, EarthTool metadata requirements, Native glTF candidates, Required fingerprints and invalidation, What stock Blender cannot promise

### Community 165 - "Official MSH Qualification Performance"
Cohesion: 0.22
Nodes (7): Before/After Protocol, Historical Measured Result, Official MSH Qualification Performance, Stage Profiling, Blender matrix, Local pre-publish qualification, Official MSH corpus

### Community 166 - "Runner"
Cohesion: 0.08
Nodes (23): ChannelReader, ChannelWriter, DynamicCoverage, Dictionary, IDictionary, int, IReadOnlyDictionary, ISet (+15 more)

### Community 167 - "Tested build and fixture"
Cohesion: 0.67
Nodes (3): Diagnostic asset, Stock options, Tested build and fixture

### Community 168 - "TexFile"
Cohesion: 0.33
Nodes (7): BinaryReader, IEnumerable, TexFile, BinaryReader, IEnumerable, SKBitmap, TexImage

### Community 169 - "EarthTool.CLI.Tests"
Cohesion: 0.25
Nodes (8): EarthTool.CLI.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 170 - ".CollectNewModelAnimationPaths"
Cohesion: 0.22
Nodes (4): Path, ParsedGltfAnimation, NewModelAnimationSet, NodeIndex

### Community 171 - "ItemCommand"
Cohesion: 0.36
Nodes (5): CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings

### Community 172 - "ResolutionBudget"
Cohesion: 0.29
Nodes (4): IEnumerable, int, long, ResolutionBudget

### Community 173 - "KhronosValidatorServer"
Cohesion: 0.40
Nodes (4): KhronosValidatorServer, IAsyncDisposable, Process, ValueTask

### Community 175 - "ViewLocator"
Cohesion: 0.07
Nodes (15): EarthTool.TEX.GUI, EarthTool.WD.GUI, Control, ViewLocator, AppBuilder, STAThread, Program, Control (+7 more)

### Community 176 - "ResearchReferenceCollectionEditorViewModel"
Cohesion: 0.23
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 177 - ".CreateBinary"
Cohesion: 0.25
Nodes (6): AnimationLayout, BinaryWriter, MemoryStream, PartitionLayout, PreviewLayout, ProjectedPartition

### Community 178 - "GltfSourceLossDiagnosticsTests"
Cohesion: 0.25
Nodes (10): Code, EventId, Fact, IEnumerable, IReadOnlyList, Path, string, Task (+2 more)

### Community 179 - ".GetMinimumSerializedLength"
Cohesion: 0.25
Nodes (6): IEnumerable, TriangleCount, VertexCount, IEnumerable, TriangleCount, VertexCount

### Community 180 - ".AssembleCanonicalStaticParsed"
Cohesion: 0.14
Nodes (11): Guid, IDictionary, IReadOnlyDictionary, ISet, List, EmitterOwnershipPlan, SeparateGltfPackage, ParsedGltfNode (+3 more)

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
Cohesion: 0.20
Nodes (8): ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile, Encoding, ParameterWriter

### Community 186 - "DynamicObject"
Cohesion: 0.33
Nodes (4): ICollection, IEnumerable, DynamicObjectScope, DynamicObject

### Community 187 - "ArchiveInfoViewModel"
Cohesion: 0.13
Nodes (8): EarthTool.WD.GUI.ViewModels, DateTime, int, long, string, ArchiveInfoViewModel, ArchiveItemViewModel, ViewModelBase

### Community 188 - "IntCollectionPropertyEditorViewModel"
Cohesion: 0.60
Nodes (3): IEnumerable, string, IntCollectionPropertyEditorViewModel

### Community 189 - "CanonicalStaticGltfCreationOptions"
Cohesion: 0.40
Nodes (5): GltfNewModelImportOptions, Guid, IReadOnlyDictionary, CanonicalStaticGltfCreationOptions, CanonicalStaticGltfSemanticOptions

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
Cohesion: 0.11
Nodes (16): IReadOnlyDictionary, IReadOnlyList, string, GltfDiagnosticCodes, GltfExportOptions, GltfLightHandle, GltfMaterialHandle, GltfMeshResourceLimits (+8 more)

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
Cohesion: 0.22
Nodes (6): AttachmentRecord, int, IReadOnlyDictionary, Vector3, AttachmentAndCannonMshFixture, AttachmentRecord

### Community 206 - "CommonCommand"
Cohesion: 0.36
Nodes (4): CancellationToken, CommandContext, Task, CommonCommand

### Community 210 - ".ToByteArray"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 216 - "CommonMeshBaseHeader"
Cohesion: 0.38
Nodes (5): byte, int, IReadOnlyList, ReadOnlySpan, CommonMeshBaseHeader

## Knowledge Gaps
- **348 isolated node(s):** `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (+343 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **9 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Enums` connect `EarthTool.Common.Interfaces` to `PAR/ConvertCommand.cs`, `IValueConverter`, `ParameterReader`, `EarthTool.MSH.Assets`, `EarthTool.CLI.Commands.WD`, `IEarthInfo`, `EarthTool.PAR.Models`, `ConvertCommand`, `ArchiveInfoViewModel`, `.CreateMockHeader`, `EarthTool.TEX`?**
  _High betweenness centrality (0.189) - this node is a cross-community bridge._
- **Why does `EarthTool.MSH.Assets` connect `EarthTool.MSH.Assets` to `MeshAssetAuthoring.cs`, `ParsedGlb`, `StaticRenderObject`, `CanonicalAuthoringMetadata`, `DynamicEffectExtension`, `GltfPlanAndReport.cs`, `ProjectedAnimationFrame`, `DynamicEffectType`, `CanonicalBaseHeaderEncoder`, `TexPreview`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **Why does `CliFixture` connect `Task` to `DynamicEffectType`, `IArchiveItem`?**
  _High betweenness centrality (0.101) - this node is a cross-community bridge._
- **What connects `net8.0`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk` to the rest of the system?**
  _348 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.Create` be split into smaller, more focused modules?**
  _Cohesion score 0.12322695035460993 - nodes in this community are weakly interconnected._
- **Should `.Compress` be split into smaller, more focused modules?**
  _Cohesion score 0.10741971207087486 - nodes in this community are weakly interconnected._
- **Should `CancellingTransactionalFileSystem` be split into smaller, more focused modules?**
  _Cohesion score 0.12105263157894737 - nodes in this community are weakly interconnected._