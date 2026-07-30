# Graph Report - .  (2026-07-30)

## Corpus Check
- 365 files · ~117,947 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2477 nodes · 5056 edges · 148 communities (142 shown, 6 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 366 edges (avg confidence: 0.81)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- MSH Collada Metadata
- WD CLI Commands
- PAR Entity Models
- MSH DAE Core
- WD Compression Services
- CLI Format Services
- MSH Conformance Tests
- PAR Combat Enums
- MSH Core Interfaces
- GUI Value Converters
- PAR GUI File Operations
- WD Archive Metadata
- Collada Model Tree
- WD Application Composition
- MSH Dynamic Parts
- PAR Faction Research Tree
- PAR Entity Serialization
- Dynamic MSH Layout
- PAR GUI Application
- MSH Binary Reader
- PAR Building Enums
- PAR Transport Capabilities
- GUI Folder Operations
- TEX GUI Image Workflow
- PAR Service Tests
- PAR Vehicle Entities
- MSH Design Decisions
- PAR Weapon Enums
- WD Archive Tests
- DAE CLI Conversion
- CLI Dependency Resolution
- DAE Project Dependencies
- MSH Light Models
- GUI Dialog Service
- WD Archive Items
- MSH Static Reader
- PAR Transport Entities
- WD Archive Item Tests
- WD Extraction Tests
- WD Decompression Paths
- MSH Collada Serialization
- WD Mapped Data Tests
- GUI App Bootstrap
- Static MSH Header
- Static MSH Records
- WD GUI Archive Actions
- MSH Static Metadata
- PAR Entity Details
- WD Archive Factory Tests
- Shared GUI ViewModels
- Collada Geometry Factory
- PAR Undo Redo
- PAR Research Editor
- WD Archive Data Sources
- TEX Reader Abstractions
- TEX GUI Application
- PAR Entity Validation
- PAR File Services
- TEX Binary Format
- Shared GUI Services
- PAR Project Dependencies
- PAR Editable Entities
- WD Archive ViewModels
- GUI Notifications
- Barrel Angle Conversion
- MSH Animation Models
- PAR GUI Dependencies
- PAR Editing Workflow
- PAR Property Undo
- TEX GUI Dependencies
- WD GUI Dependencies
- GUI Dialog Commands
- WD Archive Interfaces
- Collada Animation Factory
- PAR Passive Entities
- PAR GUI Navigation
- PAR Writer Tests
- Common Project Dependencies
- Collada Lighting Factory
- PAR Entity Factory
- PAR Binary Extensions
- PAR Property Editors
- PAR String Editors
- PAR JSON Serialization
- PAR Research Hierarchy
- PAR CLI Conversion
- Common GUI Dependencies
- WD Compression Contracts
- Static Hierarchy Conversion
- PAR Flags Editor
- WD GUI Architecture
- WD Memory Data Tests
- WD Test Dependencies
- WD Testing Standards
- Async CLI Commands
- Collada Scene Factory
- PAR CLI Item Commands
- TEX CLI Services
- EarthTool User Workflows
- WD Archive Format
- Attachment Conversion Tests
- PAR Enum Editor
- EarthTool Architecture
- Module Dependency Graph
- PAR Format Concepts
- PAR Capability Hierarchy
- TEX CLI Conversion
- CLI Project Dependencies
- Collada Materials Factory
- Collada Slot Factory
- GUI View Locators
- WD Archive Factory
- CI Build Matrix
- Release Pipeline
- Contribution Release Rules
- WD CLI Workflows
- WD Extraction Service
- MSH DAE Writers
- PAR Editable Research
- PAR Binary Reader
- TEX Project Dependencies
- WD Archive Model
- PAR Reader Tests
- TEX Test Dependencies
- EarthTool Module Suite
- MSH Static Lights
- PAR GUI Bootstrap
- PAR Research Tests
- WD Project Dependencies
- MSH Domain Framing
- MSH Service Registration
- TEX Placeholder Tests
- EarthTool Installation
- Application Host Bootstrap
- PAR Integer Collection Editor
- GUI Desktop Bootstrap
- Dependency Automation
- MSH Hierarchy Builder
- Dotnet Environment Setup
- MSH Hierarchy Tail
- PAR Validation Results
- CI Quality Preview
- Dynamic Light Color

## God Nodes (most connected - your core abstractions)
1. `EarthTool.PAR.Enums` - 90 edges
2. `EarthTool.Common.Interfaces` - 82 edges
3. `EarthTool.MSH.Interfaces` - 67 edges
4. `EarthTool.PAR.Models` - 64 edges
5. `MainWindowViewModel` - 51 edges
6. `EarthTool.PAR.Models.Abstracts` - 46 edges
7. `EarthMeshReader` - 44 edges
8. `MainWindowViewModel` - 43 edges
9. `MinimalStaticMeshConformanceTests` - 41 edges
10. `EarthTool.Common.Enums` - 40 edges

## Surprising Connections (you probably didn't know these)
- `Memory-Mapped Archive Data Source` --semantically_similar_to--> `WD Reading Algorithm`  [INFERRED] [semantically similar]
  docs/architecture.md → docs/WD_FORMAT.md
- `ConvertCommand` --references--> `IReader`  [EXTRACTED]
  EarthTool.CLI/Commands/DAE/ConvertCommand.cs → EarthTool.Common/Interfaces/IReader.cs
- `ConvertCommand` --references--> `IReader`  [EXTRACTED]
  EarthTool.CLI/Commands/MSH/ConvertCommand.cs → EarthTool.Common/Interfaces/IReader.cs
- `ConvertCommand` --references--> `IEarthInfoFactory`  [EXTRACTED]
  EarthTool.CLI/Commands/PAR/ConvertCommand.cs → EarthTool.Common/Interfaces/IEarthInfoFactory.cs
- `ConvertCommand` --references--> `IReader`  [EXTRACTED]
  EarthTool.CLI/Commands/PAR/ConvertCommand.cs → EarthTool.Common/Interfaces/IReader.cs

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

## Communities (148 total, 6 thin omitted)

### Community 0 - "MSH Collada Metadata"
Cohesion: 0.05
Nodes (39): Node, string, SlotExtensions, COLLADA, Func, Geometry, IEnumerable, Light (+31 more)

### Community 1 - "WD CLI Commands"
Cohesion: 0.05
Nodes (43): ArchiveTestsBase, Command, CommandSettings, EarthTool.CLI.Commands.WD, CancellationToken, CommandContext, AddCommand, CancellationToken (+35 more)

### Community 2 - "PAR Entity Models"
Cohesion: 0.12
Nodes (4): EarthTool.PAR.Extensions, EarthTool.PAR.Enums, EarthTool.PAR.Models.Abstracts, EarthTool.PAR.Models

### Community 3 - "MSH DAE Core"
Cohesion: 0.11
Nodes (15): EarthTool.DAE.Services, EarthTool.DAE.Tests, EarthTool.MSH.Services, EarthTool.MSH.Models, EarthTool.MSH.Models.Elements, EarthTool.MSH.Interfaces, EarthTool.MSH.Enums, EarthTool.Common.Bases (+7 more)

### Community 4 - "WD Compression Services"
Cohesion: 0.09
Nodes (17): EarthTool.WD.Tests.Services, EarthTool.WD.Services, ILogger, Stream, CompressorService, ILogger, ReadOnlySpan, Stream (+9 more)

### Community 5 - "CLI Format Services"
Cohesion: 0.08
Nodes (14): EarthTool.CLI.Commands.DAE, EarthTool.Common.Enums, EarthTool.Common.Interfaces, EarthTool.WD.Tests.Factories, EarthTool.CLI.Commands.MSH, EarthTool.Common.Models, EarthTool.Common.Validation, EarthTool.WD.Tests.Models (+6 more)

### Community 6 - "MSH Conformance Tests"
Cohesion: 0.14
Nodes (8): Fact, Guid, IEnumerable, InlineData, ReadOnlySpan, Theory, MinimalStaticMeshConformanceTests, MemberData

### Community 7 - "PAR Combat Enums"
Cohesion: 0.05
Nodes (28): DamageFlags, ExplosionFlags, HitType, MissileType, RocketType, StandType, StoreableFlags, WasteSize (+20 more)

### Community 8 - "MSH Core Interfaces"
Cohesion: 0.06
Nodes (23): IEnumerable, IMeshBaseHeader, IMeshFootprint, IMeshFrames, IMeshHorizontalExtents, IEnumerable, IModelSlots, MeshKind (+15 more)

### Community 9 - "GUI Value Converters"
Cohesion: 0.07
Nodes (22): EarthTool.PAR.GUI.Converters, EarthTool.TEX.GUI.Converters, EarthTool.WD.GUI.Converters, CultureInfo, Type, GroupNameToIconConverter, CultureInfo, Type (+14 more)

### Community 10 - "PAR GUI File Operations"
Cohesion: 0.10
Nodes (10): Task, IParFileService, bool, ILogger, ObservableCollection, ReactiveCommand, string, Task (+2 more)

### Community 11 - "WD Archive Metadata"
Cohesion: 0.09
Nodes (21): byte, FileFlags, ResourceType, Encoding, Guid, Stream, EarthInfoFactory, Guid (+13 more)

### Community 12 - "Collada Model Tree"
Cohesion: 0.07
Nodes (23): EarthTool.DAE.Collections, COLLADA, IEnumerator, Node, ModelTree, COLLADA, int, Node (+15 more)

### Community 13 - "WD Application Composition"
Cohesion: 0.07
Nodes (17): EarthTool.WD.GUI.ViewModels, EarthTool.WD.Tests, EarthTool.PAR, EarthTool.Common, EarthTool.WD.GUI, EarthTool.CLI, EarthTool.WD, EarthTool.WD.Factories (+9 more)

### Community 14 - "MSH Dynamic Parts"
Cohesion: 0.07
Nodes (20): Encoding, IBinarySerializable, EffectType, Color, IEnumerable, Vector2, Vector3, IDynamicPart (+12 more)

### Community 15 - "PAR Faction Research Tree"
Cohesion: 0.08
Nodes (24): Faction, ResearchType, ObservableCollection, EntityGroupNodeViewModel, ObservableCollection, EntityGroupsRootNodeViewModel, ObservableCollection, FactionNodeViewModel (+16 more)

### Community 16 - "PAR Entity Serialization"
Cohesion: 0.07
Nodes (23): Encoding, IEnumerable, TypelessEntity, Encoding, IEnumerable, Parameter, Encoding, IEnumerable (+15 more)

### Community 17 - "Dynamic MSH Layout"
Cohesion: 0.07
Nodes (31): Alpha and Scale Parameters, Animation Lengths, Archive Type 1, Attachments 1..49, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps (+23 more)

### Community 18 - "PAR GUI Application"
Cohesion: 0.11
Nodes (5): EarthTool.PAR.GUI, EarthTool.PAR.GUI.Services, EarthTool.PAR.GUI.ViewModels, EarthTool.PAR.GUI.Models, EarthTool.PAR.GUI.Views

### Community 19 - "MSH Binary Reader"
Cohesion: 0.19
Nodes (7): BinaryReader, Color, Encoding, Func, uint, Vector3, EarthMeshReader

### Community 20 - "PAR Building Enums"
Cohesion: 0.07
Nodes (18): BuildingExType, BuildingTabType, BuildingType, CopulaAnimationFlags, MaxShieldUpgradeType, PositionType, ResourceInputOutputFlags, SpaceStationType (+10 more)

### Community 21 - "PAR Transport Capabilities"
Cohesion: 0.08
Nodes (19): ConnectorType, LookRoundTypeFlags, RepairerCapabilityFlags, Encoding, IEnumerable, ContainerTransporter, Encoding, IEnumerable (+11 more)

### Community 22 - "GUI Folder Operations"
Cohesion: 0.11
Nodes (13): bool, HashSet, ILogger, object, ObservableCollection, ReactiveCommand, string, Unit (+5 more)

### Community 23 - "TEX GUI Image Workflow"
Cohesion: 0.12
Nodes (13): Bitmap, INotificationService, ILogger, int, List, ObservableCollection, ReactiveCommand, SKBitmap (+5 more)

### Community 24 - "PAR Service Tests"
Cohesion: 0.09
Nodes (13): EarthTool.PAR.Tests.TestDoubles, EarthTool.PAR.Services, EarthTool.PAR.Tests.TestData, EarthTool.PAR.Tests.Services, EarthTool.PAR.Tests.Factories, EarthTool.PAR.Tests.Models, EarthTool.PAR.Factories, IServiceCollection (+5 more)

### Community 25 - "PAR Vehicle Entities"
Cohesion: 0.09
Nodes (18): VehicleObjectType, Encoding, IEnumerable, Builder, Encoding, IEnumerable, Harvester, Encoding (+10 more)

### Community 26 - "MSH Design Decisions"
Cohesion: 0.09
Nodes (26): Attachment-Based Active Light Detection, EARTHTOOL Static Light Metadata, Preserve MSH Light Parameters in COLLADA Extra Metadata, Model MSH Framing and Record Extensions Explicitly, Canonical Next Record Markers, MSH Footprint API, MSH Horizontal Extents API, IMeshBaseHeader (+18 more)

### Community 27 - "PAR Weapon Enums"
Cohesion: 0.09
Nodes (17): BarrelBetaType, ShadowType, TargetType, WeaponFireType, Encoding, IEnumerable, InteractableEntity, Encoding (+9 more)

### Community 28 - "WD Archive Tests"
Cohesion: 0.20
Nodes (3): Fact, ArchiveTests, TestDataGenerator

### Community 29 - "DAE CLI Conversion"
Cohesion: 0.16
Nodes (12): CommonCommand, Dictionary, Settings, Task, TreeNode, ConvertCommand, IEnumerable, JsonSerializerOptions (+4 more)

### Community 30 - "CLI Dependency Resolution"
Cohesion: 0.09
Nodes (13): EarthTool.CLI.Commands, Func, IHostBuilder, ITypeResolver, Type, CommandTypeRegistrar, Type, CommandTypeResolver (+5 more)

### Community 31 - "DAE Project Dependencies"
Cohesion: 0.10
Nodes (20): EarthTool.DAE, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.NET.Sdk, EarthTool.DAE.Tests, net8.0, coverlet.collector, Microsoft.NET.Test.Sdk (+12 more)

### Community 32 - "MSH Light Models"
Cohesion: 0.11
Nodes (12): Color, ILight, IOmniLight, ISpotLight, Color, Encoding, Light, Encoding (+4 more)

### Community 33 - "GUI Dialog Service"
Cohesion: 0.20
Nodes (9): Button, MessageBoxResult, MessageBoxType, IEnumerable, ILogger, Task, Window, DialogService (+1 more)

### Community 34 - "WD Archive Items"
Cohesion: 0.14
Nodes (6): ReadOnlyMemory, IArchiveItem, ITextFlagService, HashSet, TextFlagService, IComparable

### Community 35 - "MSH Static Reader"
Cohesion: 0.20
Nodes (6): IEnumerable, Exception, IEnumerable, ExpectedKind, Header, InvalidDataException

### Community 36 - "PAR Transport Entities"
Cohesion: 0.12
Nodes (14): ResourceVehicleType, VerticalVehicleAnimationType, Encoding, IEnumerable, VerticalTransporter, Encoding, IEnumerable, BuildingTransporter (+6 more)

### Community 37 - "WD Archive Item Tests"
Cohesion: 0.26
Nodes (3): Fact, ArchiveItemTests, Guid

### Community 38 - "WD Extraction Tests"
Cohesion: 0.30
Nodes (7): DateTime, Guid, Fact, string, Task, WDExtractorTests, Task

### Community 39 - "WD Decompression Paths"
Cohesion: 0.15
Nodes (7): ReadOnlySpan, Stream, IDecompressor, PathValidator, Encoding, ILogger, ArchiverService

### Community 40 - "MSH Collada Serialization"
Cohesion: 0.18
Nodes (10): COLLADA, MeshModelFactory, IEnumerable, IModelPart, Encoding, BinaryWriter, Encoding, IEnumerable (+2 more)

### Community 41 - "WD Mapped Data Tests"
Cohesion: 0.24
Nodes (4): Fact, MemoryMappedFile, string, MappedArchiveDataSourceTests

### Community 42 - "GUI App Bootstrap"
Cohesion: 0.13
Nodes (8): Application, IServiceCollection, App, IServiceCollection, App, IServiceCollection, App, IServiceProvider

### Community 43 - "Static MSH Header"
Cohesion: 0.11
Nodes (18): Animation Length Encoding, Animation Type, Reverse-indexed Box Flags, Reverse-indexed Box Heights, 4x4 Box Presence Mask, 4x4 Coverage Bitmaps, 4x4 Coverage Descriptors, Header Flags and Reserved Field (+10 more)

### Community 44 - "Static MSH Records"
Cohesion: 0.11
Nodes (18): Baked TCBScale Vectors, Baked Transform Matrices, Baked Translation Vectors, Barrel Angle, End of File, Matrix Count, Next-record Heap Pointer Marker, Object Flags (+10 more)

### Community 46 - "MSH Static Metadata"
Cohesion: 0.16
Nodes (7): Node, string, ModelPartExtensions, AnimationType, PartType, FrameCount, XmlElement

### Community 47 - "PAR Entity Details"
Cohesion: 0.18
Nodes (12): Action, bool, IEnumerable, ILogger, ObservableCollection, ReactiveCommand, string, Type (+4 more)

### Community 48 - "WD Archive Factory Tests"
Cohesion: 0.26
Nodes (4): DateTime, Guid, Fact, ArchiveFactoryTests

### Community 49 - "Shared GUI ViewModels"
Cohesion: 0.15
Nodes (9): EarthTool.Common.GUI.ViewModels, EarthTool.TEX.GUI.ViewModels, ReactiveCommand, Unit, AboutViewModel, ViewModelBase, ParAboutViewModel, TexAboutViewModel (+1 more)

### Community 50 - "Collada Geometry Factory"
Cohesion: 0.24
Nodes (9): Func, Geometry, IEnumerable, Node, Source, GeometriesFactory, IEnumerator, List (+1 more)

### Community 51 - "PAR Undo Redo"
Cohesion: 0.14
Nodes (9): Action, DateTime, UndoAction, Action, IEnumerable, ILogger, int, Stack (+1 more)

### Community 52 - "PAR Research Editor"
Cohesion: 0.20
Nodes (8): Action, bool, IEnumerable, ObservableCollection, ReactiveCommand, Unit, ResearchReferenceCollectionEditorViewModel, ResearchReferenceViewModel

### Community 53 - "WD Archive Data Sources"
Cohesion: 0.12
Nodes (14): ReadOnlyMemory, IArchiveDataSource, bool, ReadOnlyMemory, ArchiveItem, int, MemoryMappedFile, ReadOnlyMemory (+6 more)

### Community 54 - "TEX Reader Abstractions"
Cohesion: 0.16
Nodes (11): CommonSettings, Settings, Settings, Reader, FileType, IReader, IEnumerable, TexHeader (+3 more)

### Community 55 - "TEX GUI Application"
Cohesion: 0.13
Nodes (8): EarthTool.TEX.GUI, EarthTool.TEX.GUI.Views, EarthTool.Common.GUI, AppBuilder, STAThread, Program, Control, ViewLocator

### Community 56 - "PAR Entity Validation"
Cohesion: 0.27
Nodes (7): List, ValidationResult, ILogger, EntityValidationService, IEntityValidationService, IEnumerable, Entity

### Community 57 - "PAR File Services"
Cohesion: 0.20
Nodes (8): ILogger, Task, ParFileService, Encoding, IEnumerable, ParFile, Encoding, ParameterWriter

### Community 58 - "TEX Binary Format"
Cohesion: 0.18
Nodes (9): BinaryReader, IEnumerable, TexFile, TexFlags, TexHeader, BinaryReader, IEnumerable, SKBitmap (+1 more)

### Community 59 - "Shared GUI Services"
Cohesion: 0.17
Nodes (8): EarthTool.Common.GUI.Enums, EarthTool.Common.GUI.Services, EarthTool.Common.GUI.Interfaces, EarthTool.Common.GUI.Views, IServiceCollection, ServiceCollectionExtensions, AboutView, UserControl

### Community 60 - "PAR Project Dependencies"
Cohesion: 0.13
Nodes (15): EarthTool.PAR, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Json, Microsoft.NET.Sdk, EarthTool.PAR.Tests, net8.0 (+7 more)

### Community 61 - "PAR Editable Entities"
Cohesion: 0.17
Nodes (8): EntityClassType, bool, Dictionary, EditableEntity, ObservableCollection, EntityListItemViewModel, NewValue, OldValue

### Community 62 - "WD Archive ViewModels"
Cohesion: 0.15
Nodes (7): DateTime, int, string, ArchiveInfoViewModel, ArchiveItemViewModel, ViewModelBase, long

### Community 63 - "GUI Notifications"
Cohesion: 0.19
Nodes (7): NotificationType, Exception, NotificationEventArgs, Exception, ILogger, NotificationService, EventArgs

### Community 64 - "Barrel Angle Conversion"
Cohesion: 0.25
Nodes (4): Fact, InlineData, Theory, BarrelAngleConversionTests

### Community 65 - "MSH Animation Models"
Cohesion: 0.15
Nodes (9): IEnumerable, IAnimations, Matrix4x4, IRotationFrame, IEnumerable, Animations, Encoding, Matrix4x4 (+1 more)

### Community 66 - "PAR GUI Dependencies"
Cohesion: 0.14
Nodes (14): EarthTool.PAR.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 67 - "PAR Editing Workflow"
Cohesion: 0.20
Nodes (3): Action, IEnumerable, IPropertyEditorFactory

### Community 68 - "PAR Property Undo"
Cohesion: 0.15
Nodes (6): Action, IEnumerable, IUndoRedoService, int, string, IntPropertyEditorViewModel

### Community 69 - "TEX GUI Dependencies"
Cohesion: 0.14
Nodes (14): EarthTool.TEX.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 70 - "WD GUI Dependencies"
Cohesion: 0.14
Nodes (14): EarthTool.WD.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.Configuration (+6 more)

### Community 71 - "GUI Dialog Commands"
Cohesion: 0.23
Nodes (3): IEnumerable, Task, IDialogService

### Community 72 - "WD Archive Interfaces"
Cohesion: 0.18
Nodes (7): DateTime, Encoding, IReadOnlyCollection, IArchive, DateTime, Guid, IArchiveFactory

### Community 73 - "Collada Animation Factory"
Cohesion: 0.27
Nodes (6): Animation, IEnumerable, Matrix4x4, Source, AnimationsFactory, float

### Community 74 - "PAR Passive Entities"
Cohesion: 0.17
Nodes (9): ArtifactType, PassiveMask, Encoding, IEnumerable, PassiveEntity, Encoding, IEnumerable, Artifact (+1 more)

### Community 75 - "PAR GUI Navigation"
Cohesion: 0.18
Nodes (8): Task, MainWindow, MainWindow, MainWindow, KeyEventArgs, PointerPressedEventArgs, RoutedEventArgs, Window

### Community 76 - "PAR Writer Tests"
Cohesion: 0.29
Nodes (5): Writer, Fact, ParameterWriterTests, Encoding, ParTestData

### Community 77 - "Common Project Dependencies"
Cohesion: 0.17
Nodes (12): EarthTool.Common, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, EarthTool.MSH.Tests, net8.0, coverlet.collector (+4 more)

### Community 78 - "Collada Lighting Factory"
Cohesion: 0.30
Nodes (6): IEnumerable, Light, Node, LightingFactory, LightNode, LightTechnique_Common

### Community 79 - "PAR Entity Factory"
Cohesion: 0.21
Nodes (6): EntityGroupType, BinaryReader, IEnumerable, EntityFactory, IEnumerable, IEnumerable

### Community 80 - "PAR Binary Extensions"
Cohesion: 0.24
Nodes (5): BinaryReader, BinaryWriter, Encoding, int, BinaryExtensions

### Community 81 - "PAR Property Editors"
Cohesion: 0.27
Nodes (7): Action, HashSet, IEnumerable, ILogger, Type, PropertyEditorFactory, PropertyInfo

### Community 82 - "PAR String Editors"
Cohesion: 0.17
Nodes (11): bool, ReactiveCommand, string, Type, Unit, PropertyEditorViewModel, bool, int (+3 more)

### Community 83 - "PAR JSON Serialization"
Cohesion: 0.20
Nodes (8): EarthTool.PAR.Models.Serialization, JsonSerializerOptions, Type, EntityConverter, TypeReader, JsonConverter, Utf8JsonReader, Utf8JsonWriter

### Community 84 - "PAR Research Hierarchy"
Cohesion: 0.31
Nodes (6): IEnumerable, TreeNode, ParameterEntry, IEnumerable, Research, IDictionary

### Community 85 - "PAR CLI Conversion"
Cohesion: 0.33
Nodes (6): JsonSerializerOptions, string, Task, ConvertCommand, Guid, ParSettings

### Community 86 - "Common GUI Dependencies"
Cohesion: 0.18
Nodes (11): EarthTool.Common.GUI, net8.0, Avalonia, Avalonia.Controls.DataGrid, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Microsoft.Extensions.DependencyInjection.Abstractions (+3 more)

### Community 87 - "WD Compression Contracts"
Cohesion: 0.24
Nodes (6): Stream, ICompressor, Encoding, Encoding, ArchiveTestsBase, Fixture

### Community 88 - "Static Hierarchy Conversion"
Cohesion: 0.42
Nodes (4): COLLADA, Fact, Node, StaticHierarchyConversionTests

### Community 89 - "PAR Flags Editor"
Cohesion: 0.31
Nodes (4): object, ObservableCollection, Type, FlagsPropertyEditorViewModel

### Community 90 - "WD GUI Architecture"
Cohesion: 0.20
Nodes (11): GUI Dependency Injection, MVVM Architecture, Notification-Based Error Handling, Reactive Command Pattern, EarthTool WD Archive Manager, Archive Management Workflow, Automatic Compression and Decompression, In-Memory Archive Modification (+3 more)

### Community 92 - "WD Test Dependencies"
Cohesion: 0.18
Nodes (11): EarthTool.WD.Tests, net8.0, AwesomeAssertions, coverlet.collector, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.NET.Test.Sdk, xunit (+3 more)

### Community 93 - "WD Testing Standards"
Cohesion: 0.22
Nodes (10): EarthTool Code Style, Arrange-Act-Assert, Pull Request Quality Gate, Test Coverage Requirements, ArchiveTestsBase, WD Extraction Integration Tests, WD Model Tests, WD Service Tests (+2 more)

### Community 94 - "Async CLI Commands"
Cohesion: 0.31
Nodes (5): AsyncCommand, CancellationToken, CommandContext, Task, CommonCommand

### Community 95 - "Collada Scene Factory"
Cohesion: 0.31
Nodes (6): COLLADAScene, COLLADA, IEnumerable, Node, ColladaModelFactory, Library_Visual_Scenes

### Community 96 - "PAR CLI Item Commands"
Cohesion: 0.27
Nodes (6): EarthTool.CLI.Commands.PAR, CancellationToken, CommandContext, IEnumerable, ItemCommand, ItemSettings

### Community 97 - "TEX CLI Services"
Cohesion: 0.27
Nodes (5): EarthTool.TEX, EarthTool.TEX.Interfaces, EarthTool.CLI.Commands.TEX, IServiceCollection, HostExtensions

### Community 98 - "EarthTool User Workflows"
Cohesion: 0.22
Nodes (10): EarthTool Dual Interface, EarthTool Project Goals, EarthTool Project Overview, EarthTool Suite, MSH Model Conversion, TEX Texture Conversion, WD Archive Management, MSH Model Export Workflow (+2 more)

### Community 99 - "WD Archive Format"
Cohesion: 0.29
Nodes (10): wd create, WD Central Directory, WD Descriptor Length, EarthInfo Archive Header, WD File Data Section, FileFlags, WD Archive Format Specification, WD Reading Algorithm (+2 more)

### Community 100 - "Attachment Conversion Tests"
Cohesion: 0.36
Nodes (3): Fact, IEnumerable, AttachmentConversionTests

### Community 101 - "PAR Enum Editor"
Cohesion: 0.29
Nodes (5): object, ObservableCollection, Type, EnumPropertyEditorViewModel, EnumValueViewModel

### Community 102 - "EarthTool Architecture"
Cohesion: 0.25
Nodes (9): Dependency Inversion, EarthTool Architecture, Interface Segregation, Layered Modular Architecture, Central Package Management, EarthTool Project Structure, EarthTool Module Dependency Rules, EarthTool Test Project Structure (+1 more)

### Community 103 - "Module Dependency Graph"
Cohesion: 0.25
Nodes (9): EarthTool.CLI, EarthTool.Common, EarthTool.DAE, EarthTool.MSH, EarthTool.PAR, EarthTool.TEX, EarthTool.WD, EarthTool.WD.GUI (+1 more)

### Community 104 - "PAR Format Concepts"
Cohesion: 0.25
Nodes (9): PAR Parameter Editing, PAR Binary Serialization, EarthTool PAR Entity Hierarchy, Entity, IBinarySerializable, ParameterEntry, Polymorphic JSON Deserialization, Research (+1 more)

### Community 105 - "PAR Capability Hierarchy"
Cohesion: 0.28
Nodes (9): Capability Stacking Architecture, DestructibleEntity, EquipableEntity, Equipment, InteractableEntity, PassiveEntity, TypedEntity, Vehicle (+1 more)

### Community 106 - "TEX CLI Conversion"
Cohesion: 0.33
Nodes (5): CommonSettings, Task, TreeNode, ConvertCommand, IWriter

### Community 107 - "CLI Project Dependencies"
Cohesion: 0.22
Nodes (9): EarthTool.CLI, net8.0, Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console, Microsoft.NET.Sdk, Microsoft.Extensions.Hosting (+1 more)

### Community 108 - "Collada Materials Factory"
Cohesion: 0.36
Nodes (5): IEnumerable, Material, MaterialFactory, Effect, Image

### Community 109 - "Collada Slot Factory"
Cohesion: 0.33
Nodes (6): IEnumerable, Light, Node, Slot, SlotFactory, SlotNode

### Community 110 - "GUI View Locators"
Cohesion: 0.22
Nodes (5): Control, ViewLocator, Control, ViewLocator, IDataTemplate

### Community 111 - "WD Archive Factory"
Cohesion: 0.33
Nodes (5): BinaryReader, IEnumerable, ILogger, MemoryMappedFile, ArchiveFactory

### Community 112 - "CI Build Matrix"
Cohesion: 0.29
Nodes (8): Build CLI Job, Build PAR GUI Job, Build TEX GUI Job, Build WD GUI Job, Detect Changes Job, Branch Version Labels, Continuous Deployment Versioning, Semantic Versioning Configuration

### Community 113 - "Release Pipeline"
Cohesion: 0.29
Nodes (8): Unified CI Pipeline, Development Preview Builds, Smart Change Detection, Unified Workflow Architecture, Release Build and Test Job, Create Release Job, Unified Release Pipeline, Self-Contained Multiplatform Artifacts

### Community 114 - "Contribution Release Rules"
Cohesion: 0.36
Nodes (8): Automatic Changelog Generation, Breaking Change Signaling, Conventional Commits, Release Type Detection, Generate Unified Changelog, EarthTool Agent Guidelines, Build and Test Commands, EarthTool Contributor Workflow

### Community 115 - "WD CLI Workflows"
Cohesion: 0.25
Nodes (8): CLI Archive Workflow, GUI Archive Workflow, EarthTool Quick Start Guide, wd add, WD Archive Commands, wd info, wd list, wd remove

### Community 116 - "WD Extraction Service"
Cohesion: 0.25
Nodes (5): Task, IExtractor, IWDExtractor, ILogger, WDExtractor

### Community 117 - "MSH DAE Writers"
Cohesion: 0.39
Nodes (5): ColladaMeshWriter, IEnumerable, IMesh, Encoding, EarthMeshWriter

### Community 118 - "PAR Editable Research"
Cohesion: 0.29
Nodes (6): bool, Dictionary, EditableResearch, bool, FlagValueViewModel, ReactiveObject

### Community 119 - "PAR Binary Reader"
Cohesion: 0.46
Nodes (4): BinaryReader, Encoding, IEnumerable, ParameterReader

### Community 120 - "TEX Project Dependencies"
Cohesion: 0.25
Nodes (8): EarthTool.TEX, netstandard2.1, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk, SkiaSharp, SkiaSharp.NativeAssets.Linux

### Community 121 - "WD Archive Model"
Cohesion: 0.29
Nodes (6): bool, DateTime, IReadOnlyCollection, MemoryMappedFile, Archive, SortedSet

### Community 123 - "TEX Test Dependencies"
Cohesion: 0.29
Nodes (7): EarthTool.TEX.Tests, net8.0, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 124 - "EarthTool Module Suite"
Cohesion: 0.29
Nodes (7): EarthTool, EarthTool.CLI, EarthTool.DAE, EarthTool.MSH, EarthTool.PAR, EarthTool.PAR.GUI, EarthTool.TEX

### Community 125 - "MSH Static Lights"
Cohesion: 0.33
Nodes (6): Active Static Light, Light Attachment, Light Parameters, Spot Heading, Spot Shape Values, Static Light

### Community 126 - "PAR GUI Bootstrap"
Cohesion: 0.40
Nodes (3): AppBuilder, STAThread, Program

### Community 127 - "PAR Research Tests"
Cohesion: 0.47
Nodes (3): Encoding, Fact, ResearchSerializationTests

### Community 128 - "WD Project Dependencies"
Cohesion: 0.33
Nodes (6): EarthTool.WD, net8.0, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.Text.Encoding.CodePages, Microsoft.NET.Sdk

### Community 129 - "MSH Domain Framing"
Cohesion: 0.40
Nodes (5): Archive Framing, Base Header, Mesh Kind, MSH Domain Language, Trailing Hierarchy Unwind Count

### Community 130 - "MSH Service Registration"
Cohesion: 0.40
Nodes (3): EarthTool.MSH, IServiceCollection, HostExtensions

### Community 131 - "TEX Placeholder Tests"
Cohesion: 0.40
Nodes (3): EarthTool.TEX.Tests, Fact, UnitTest1

### Community 132 - "EarthTool Installation"
Cohesion: 0.60
Nodes (5): Binary Download Installation, Docker Installation, EarthTool Installation Guide, .NET 8 Requirement, Source Build Installation

### Community 133 - "Application Host Bootstrap"
Cohesion: 0.50
Nodes (3): IHostBuilder, Task, Program

### Community 134 - "PAR Integer Collection Editor"
Cohesion: 0.60
Nodes (3): IEnumerable, string, IntCollectionPropertyEditorViewModel

### Community 135 - "GUI Desktop Bootstrap"
Cohesion: 0.50
Nodes (3): AppBuilder, STAThread, Program

### Community 136 - "Dependency Automation"
Cohesion: 0.50
Nodes (4): Dependabot Dependency Automation, Weekly GitHub Actions Updates, Weekly NuGet Updates, Security Check Job

### Community 137 - "MSH Hierarchy Builder"
Cohesion: 0.50
Nodes (3): IHierarchyBuilder, IEnumerable, HierarchyBuilder

### Community 138 - "Dotnet Environment Setup"
Cohesion: 0.67
Nodes (3): .NET SDK Setup, NuGet Package Cache, Setup .NET Environment

### Community 139 - "MSH Hierarchy Tail"
Cohesion: 0.67
Nodes (3): Trailing Hierarchy Unwind Count, Mesh Attachments 1..49, Mesh Extents

## Knowledge Gaps
- **238 isolated node(s):** `EarthTool.CLI.Commands.DAE`, `EarthTool.CLI.Commands.MSH`, `EarthTool.CLI.Commands.TEX`, `net8.0`, `Microsoft.Extensions.Configuration` (+233 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EarthTool.Common.Interfaces` connect `CLI Format Services` to `MSH Light Models`, `TEX CLI Services`, `PAR Entity Models`, `WD CLI Commands`, `WD Archive Items`, `MSH DAE Core`, `MSH Service Registration`, `WD Decompression Paths`, `WD Archive Interfaces`, `MSH Animation Models`, `MSH Core Interfaces`, `WD Compression Services`, `WD Application Composition`, `MSH Dynamic Parts`, `Shared GUI ViewModels`, `WD Extraction Service`, `WD Compression Contracts`, `PAR Service Tests`?**
  _High betweenness centrality (0.168) - this node is a cross-community bridge._
- **Why does `EarthTool.PAR.Enums` connect `PAR Entity Models` to `PAR Transport Entities`, `CLI Format Services`, `PAR Combat Enums`, `PAR Passive Entities`, `PAR Entity Factory`, `PAR Faction Research Tree`, `PAR GUI Application`, `PAR Building Enums`, `PAR Transport Capabilities`, `PAR Service Tests`, `PAR Vehicle Entities`, `PAR Weapon Enums`, `PAR Editable Entities`?**
  _High betweenness centrality (0.080) - this node is a cross-community bridge._
- **Why does `IBinarySerializable` connect `MSH Dynamic Parts` to `MSH Collada Metadata`, `MSH Animation Models`, `MSH Light Models`, `MSH Core Interfaces`, `MSH Collada Serialization`, `WD Archive Metadata`, `PAR Faction Research Tree`, `PAR Research Hierarchy`, `MSH DAE Writers`, `PAR Entity Validation`, `PAR File Services`?**
  _High betweenness centrality (0.080) - this node is a cross-community bridge._
- **What connects `EarthTool.CLI.Commands.DAE`, `EarthTool.CLI.Commands.MSH`, `EarthTool.CLI.Commands.TEX` to the rest of the system?**
  _238 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MSH Collada Metadata` be split into smaller, more focused modules?**
  _Cohesion score 0.05132788559754852 - nodes in this community are weakly interconnected._
- **Should `WD CLI Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.051590483827853514 - nodes in this community are weakly interconnected._
- **Should `PAR Entity Models` be split into smaller, more focused modules?**
  _Cohesion score 0.11840120663650075 - nodes in this community are weakly interconnected._