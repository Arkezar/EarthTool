# EarthTool Project Structure

Complete guide to the codebase organization and file structure.

## Repository Structure

```
EarthTool/
├── .github/                    # GitHub-specific files
│   ├── workflows/              # CI/CD workflows
│   │   ├── ci-unified.yml      # Unified CI pipeline
│   │   ├── release.yml         # Unified release pipeline
│   │   ├── README.md           # Workflow documentation
│   │   ├── CONVENTIONAL_COMMITS.md  # Commit format guide
│   │   └── ...                 # Other workflow files
│   └── actions/                # Custom GitHub actions
│
├── .run/                       # IDE run configurations (JetBrains)
├── .vscode/                    # VS Code configuration
│   ├── launch.json             # Debug configurations
│   ├── settings.json           # Workspace settings
│   └── tasks.json              # Build tasks
│
├── docs/                       # 📚 Documentation
│   ├── README.md               # Documentation index
│   ├── overview.md             # Project overview
│   ├── installation.md         # Installation guide
│   ├── quickstart.md           # Quick start guide
│   ├── architecture.md         # Architecture documentation
│   ├── WD_FORMAT.md            # WD archive format spec
│   ├── WD_COMMANDS.md          # WD command reference
│   └── ...                     # Additional documentation
│
├── scripts/                    # Utility scripts
│   └── repack.sh               # Archive repacking script (has --help)
│
├── EarthTool.Common/           # 🔧 Shared library
│   ├── Bases/                  # Base classes
│   ├── Enums/                  # Shared enumerations
│   ├── Factories/              # Factory implementations
│   ├── Interfaces/             # Core interfaces
│   ├── Models/                 # Shared models
│   ├── Validation/             # Validation utilities
│   ├── HostExtensions.cs       # DI registration
│   └── EarthTool.Common.csproj
│
├── EarthTool.WD/               # 📦 WD Archive library
│   ├── Factories/
│   │   └── ArchiveFactory.cs   # Archive creation/opening
│   ├── Interfaces/
│   │   └── IArchiveDataSource.cs
│   ├── Models/
│   │   ├── Archive.cs          # IArchive implementation
│   │   ├── ArchiveItem.cs      # IArchiveItem implementation
│   │   ├── InMemoryArchiveDataSource.cs
│   │   └── MappedArchiveDataSource.cs
│   ├── Services/
│   │   ├── ArchiverService.cs  # High-level operations
│   │   ├── CompressorService.cs
│   │   └── DecompressorService.cs
│   ├── HostExtensions.cs
│   ├── WDExtractor.cs
│   └── EarthTool.WD.csproj
│
├── EarthTool.WD.Tests/         # ✅ WD Tests (92% coverage)
│   ├── Factories/
│   ├── Models/
│   ├── Services/
│   ├── ArchiveTestsBase.cs
│   ├── TestDataGenerator.cs
│   ├── README.md               # Testing documentation
│   └── EarthTool.WD.Tests.csproj
│
├── EarthTool.MSH/              # 🎮 MSH Model library
│   ├── Collections/
│   │   ├── ModelTree.cs        # Hierarchical structure
│   │   └── ModelTreeEnumerator.cs
│   ├── Elements/
│   │   ├── AnimationsFactory.cs
│   │   └── ColladaModelFactory.cs
│   ├── Enums/
│   │   ├── AnimationType.cs
│   │   └── PartType.cs
│   ├── Extensions/
│   │   └── ModelPartExtensions.cs
│   ├── Interfaces/             # 26 interfaces
│   ├── Models/
│   │   ├── Collections/        # Model collections
│   │   ├── Elements/           # Model elements (12 types)
│   │   └── [9 model files]
│   ├── Services/
│   │   ├── EarthMeshReader.cs
│   │   ├── EarthMeshWriter.cs
│   │   └── MeshOptimizer.cs
│   ├── HostExtensions.cs
│   └── EarthTool.MSH.csproj
│
├── EarthTool.MSH.Tests/        # ✅ MSH Tests
│   └── EarthTool.MSH.Tests.csproj
│
├── EarthTool.DAE/              # 📐 COLLADA Export library
│   ├── Collada141/             # 518 COLLADA element classes
│   │   ├── COLLADA.cs          # Root element
│   │   ├── Geometry.cs
│   │   ├── Material.cs
│   │   ├── Scene.cs
│   │   └── ...                 # All COLLADA 1.4.1 elements
│   ├── Collections/
│   │   ├── ModelTree.cs
│   │   ├── ModelTreeEnumerator.cs
│   │   └── ModelTreeNode.cs
│   ├── Elements/
│   │   ├── AnimationsFactory.cs
│   │   ├── ColladaModelFactory.cs
│   │   ├── GeometryFactory.cs
│   │   ├── MaterialFactory.cs
│   │   ├── NodeFactory.cs
│   │   └── SceneFactory.cs
│   ├── Extensions/
│   │   └── ModelPartExtensions.cs
│   ├── Services/
│   │   ├── ColladaMeshReader.cs
│   │   └── ColladaMeshWriter.cs
│   ├── HostExtensions.cs
│   └── EarthTool.DAE.csproj
│
├── EarthTool.DAE.Tests/        # ✅ DAE Tests
│   └── EarthTool.DAE.Tests.csproj
│
├── EarthTool.PAR/              # ⚙️ Parameter library
│   ├── Enums/                  # 32 enumeration types
│   │   ├── ArtifactType.cs
│   │   ├── BarrelBetaType.cs
│   │   └── ...
│   ├── Factories/
│   │   └── EntityFactory.cs    # Entity type resolution
│   ├── Models/
│   │   ├── Abstracts/          # 9 abstract base types
│   │   ├── Serialization/      # JSON converters
│   │   └── [38 entity types]   # All game entities
│   ├── Services/
│   │   ├── ParameterReader.cs  # Binary parser
│   │   └── ParameterWriter.cs  # Binary writer
│   ├── HostExtensions.cs
│   └── EarthTool.PAR.csproj
│
├── EarthTool.PAR.Tests/        # ✅ PAR Tests
│   ├── Factories/
│   ├── Models/
│   ├── Services/
│   ├── TestData/
│   └── EarthTool.PAR.Tests.csproj
│
├── EarthTool.TEX/              # 🎨 Texture library
│   ├── Interfaces/
│   │   └── ITexFile.cs
│   ├── AnimationType.cs
│   ├── Header.cs
│   ├── HostExtensions.cs
│   ├── TexFile.cs
│   ├── TexHeader.cs
│   ├── TexImage.cs
│   ├── TexReader.cs
│   ├── TextureSubType.cs
│   ├── TextureType.cs
│   └── EarthTool.TEX.csproj
│
├── EarthTool.TEX.Tests/        # ✅ TEX Tests
│   └── EarthTool.TEX.Tests.csproj
│
├── EarthTool.CLI/              # 💻 Command Line Interface
│   ├── Commands/
│   │   ├── DAE/
│   │   │   └── ConvertCommand.cs
│   │   ├── MSH/
│   │   │   └── ConvertCommand.cs
│   │   ├── PAR/
│   │   │   ├── ConvertCommand.cs
│   │   │   └── ItemCommand.cs
│   │   ├── TEX/
│   │   │   └── ConvertCommand.cs
│   │       └── WD/
│   │           ├── WdSettings.cs   # Command settings
│   │           ├── WdCommandBase.cs
│   │           ├── ListCommand.cs
│   │           ├── ExtractCommand.cs
│   │           ├── CreateCommand.cs
│   │           ├── AddCommand.cs
│   │           ├── RemoveCommand.cs
│   │           └── InfoCommand.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Program.cs              # Entry point + DI setup
│   └── EarthTool.CLI.csproj
│
├── EarthTool.WD.GUI/           # 🖥️ Graphical User Interface
│   ├── Assets/
│   │   └── avalonia-logo.ico
│   ├── Converters/
│   │   ├── BytesToHumanReadableConverter.cs
│   │   └── FileFlagsToStringConverter.cs
│   ├── Services/
│   │   ├── DialogService.cs
│   │   ├── IDialogService.cs
│   │   ├── INotificationService.cs
│   │   └── NotificationService.cs
│   ├── ViewModels/
│   │   ├── AboutViewModel.cs
│   │   ├── ArchiveInfoViewModel.cs
│   │   ├── ArchiveItemViewModel.cs
│   │   ├── MainWindowViewModel.cs
│   │   └── ViewModelBase.cs
│   ├── Views/
│   │   ├── AboutView.axaml
│   │   ├── AboutView.axaml.cs
│   │   ├── MainWindow.axaml    # Main UI layout
│   │   └── MainWindow.axaml.cs
│   ├── App.axaml               # Application resources
│   ├── App.axaml.cs            # DI configuration
│   ├── app.manifest
│   ├── Program.cs              # Entry point
│   ├── ViewLocator.cs
│   ├── README.md               # GUI module documentation
│   ├── USER_GUIDE.md           # Detailed GUI user guide
│   └── EarthTool.WD.GUI.csproj
│
├── .editorconfig               # Code style configuration
├── .gitattributes              # Git attributes
├── .gitignore                  # Git ignore rules
├── AGENTS.md                   # AI agent guidelines
├── Directory.Build.props       # Shared MSBuild properties
├── Directory.Packages.props    # Central package management
├── Earth2150_MSH.bt            # 010 Editor template (MSH)
├── Earth2150_WD.bt             # 010 Editor template (WD)
├── EarthTool.sln               # Visual Studio solution
├── GitVersion.yml              # GitVersion configuration
├── LICENSE                     # MIT License
├── msh.hexpat                  # ImHex pattern (MSH)
└── README.md                   # Main project README
```

## Module Responsibilities

### Core Libraries

#### EarthTool.Common
- **Purpose**: Shared infrastructure for all modules
- **Key Exports**: Interfaces (IArchive, IArchiver, etc.), base classes, utilities
- **Dependencies**: None (only .NET)
- **Used By**: All other modules

#### EarthTool.WD
- **Purpose**: WD archive format support (read, write, manipulate)
- **Key Exports**: ArchiveFactory, ArchiverService, Compressor/Decompressor
- **Dependencies**: EarthTool.Common
- **Used By**: CLI, GUI

#### EarthTool.MSH
- **Purpose**: MSH 3D model format parsing
- **Key Exports**: EarthMeshReader, Mesh models
- **Dependencies**: EarthTool.Common
- **Used By**: CLI, DAE (for conversion)

#### EarthTool.DAE
- **Purpose**: COLLADA 1.4.1 export from MSH
- **Key Exports**: ColladaMeshWriter, COLLADA types
- **Dependencies**: EarthTool.Common, EarthTool.MSH
- **Used By**: CLI

#### EarthTool.PAR
- **Purpose**: Parameter file parsing and editing
- **Key Exports**: ParameterReader, ParameterWriter, Entity models
- **Dependencies**: EarthTool.Common
- **Used By**: CLI

#### EarthTool.TEX
- **Purpose**: Texture format handling
- **Key Exports**: TexReader, TexFile
- **Dependencies**: EarthTool.Common
- **Used By**: CLI

### Applications

#### EarthTool.CLI
- **Purpose**: Command-line interface for all operations
- **Dependencies**: All format libraries
- **Output**: Executable (`EarthTool.CLI.exe` / `EarthTool.CLI`)

#### EarthTool.WD.GUI
- **Purpose**: Graphical interface for WD archive management
- **Dependencies**: EarthTool.Common, EarthTool.WD
- **Output**: Executable (`EarthTool.WD.GUI.exe` / `EarthTool.WD.GUI`)

## File Naming Conventions

### C# Files
- **Classes**: PascalCase, match file name (e.g., `ArchiveFactory.cs`)
- **Interfaces**: Prefix with `I` (e.g., `IArchive.cs`)
- **Extensions**: `*Extensions.cs` (e.g., `HostExtensions.cs`)
- **Base Classes**: `*Base.cs` (e.g., `ViewModelBase.cs`)

### Project Files
- **Projects**: `*.csproj` (e.g., `EarthTool.WD.csproj`)
- **Solution**: `*.sln` (e.g., `EarthTool.sln`)
- **Props**: `*.props` (e.g., `Directory.Build.props`)

### Documentation
- **Markdown**: Lowercase with hyphens (e.g., `quick-start.md`)
- **Format specs**: UPPERCASE (e.g., `WD_FORMAT.md`)
- **Special**: UPPERCASE (e.g., `README.md`, `LICENSE`)

## Configuration Files

### Build Configuration
- **`Directory.Build.props`**: Shared MSBuild properties (version, etc.)
- **`Directory.Packages.props`**: Central package management
- **`*.csproj`**: Individual project configuration

### Code Style
- **`.editorconfig`**: Code style rules
  - Indentation: 2 spaces
  - Line endings: LF
  - Encoding: UTF-8 with BOM
  - Naming conventions (fields, properties, methods)

### Git
- **`.gitignore`**: Files to ignore
- **`.gitattributes`**: File attributes (line endings, etc.)
- **`GitVersion.yml`**: Automatic versioning

### IDE
- **`.vscode/`**: VS Code configuration
- **`.run/`**: JetBrains Rider configuration
- **`Properties/launchSettings.json`**: Launch profiles

## Build Outputs

### Debug Build
```
bin/Debug/net8.0/
├── EarthTool.CLI.dll
├── EarthTool.CLI.exe (on Windows)
├── EarthTool.WD.dll
├── EarthTool.Common.dll
└── ... (all dependencies)
```

### Release Build
```
bin/Release/net8.0/
├── EarthTool.CLI.dll
├── EarthTool.CLI.exe
└── ... (optimized)
```

### Published (Single-File)
```
publish/
├── cli-win/
│   └── EarthTool.CLI.exe (self-contained, ~50MB)
├── cli-linux/
│   └── EarthTool.CLI (self-contained, ~50MB)
└── gui-win/
    └── EarthTool.WD.GUI.exe (self-contained, ~70MB)
```

## Test Structure

All test projects follow the same structure:

```
EarthTool.*.Tests/
├── [Component]/        # Tests organized by component
│   ├── *Tests.cs       # Test classes
│   └── ...
├── TestData/           # Test data
├── TestDoubles/        # Mocks, stubs, fakes
├── Usings.cs           # Global usings
├── README.md           # Test documentation
└── *.Tests.csproj      # Test project file
```

## Navigation Guide

### For Users
- **Installation**: See [`docs/installation.md`](installation.md)
- **Quick Start**: See [`docs/quickstart.md`](quickstart.md)
- **CLI Guide**: See [`docs/user-guide-cli.md`](user-guide-cli.md)
- **GUI Guide**: See [`EarthTool.WD.GUI/USER_GUIDE.md`](../EarthTool.WD.GUI/USER_GUIDE.md)

### For Developers
- **Architecture**: See [`docs/architecture.md`](architecture.md)
- **Code Style**: See [`AGENTS.md`](../AGENTS.md)
- **Testing**: See [`EarthTool.WD.Tests/README.md`](../EarthTool.WD.Tests/README.md)
- **Contributing**: See [`CONTRIBUTING.md`](../CONTRIBUTING.md)

### For Format Documentation
- **WD Format**: See [`docs/WD_FORMAT.md`](WD_FORMAT.md)
- **WD Commands**: See [`docs/WD_COMMANDS.md`](WD_COMMANDS.md)
- **010 Editor**: See `Earth2150_*.bt` files
- **ImHex**: See `*.hexpat` files

## Important Files

| File | Purpose |
|------|---------|
| `README.md` | Project overview and main documentation |
| `AGENTS.md` | Guidelines for AI agents (build commands, code style) |
| `LICENSE` | MIT License |
| `EarthTool.sln` | Visual Studio solution file |
| `Directory.Build.props` | Shared build properties |
| `Directory.Packages.props` | Centralized package versions |
| `.editorconfig` | Code style configuration |
| `GitVersion.yml` | Automatic versioning configuration |

## Dependencies

### NuGet Packages (Notable)
- **Microsoft.Extensions.DependencyInjection**: 8.0.0
- **Microsoft.Extensions.Logging**: 8.0.0
- **System.Text.Encoding.CodePages**: For Windows-1252 encoding
- **Avalonia**: 11.x (GUI only)
- **ReactiveUI.Avalonia**: 11.x (GUI only)
- **Spectre.Console**: (CLI only)
- **xUnit**: 2.x (Tests only)
- **FluentAssertions**: 6.x (Tests only)

### Development Tools
- **.NET SDK 8.0**: Required for building
- **Git**: Version control
- **Visual Studio 2022** / **VS Code** / **Rider**: IDEs

## Common Operations

### Find a Feature Implementation
1. Check `EarthTool.CLI/Commands/` for CLI implementation
2. Check `EarthTool.*.GUI/ViewModels/` for GUI implementation
3. Check corresponding library (`EarthTool.WD`, etc.) for core logic

### Find Format Specification
1. Check `docs/` for high-level documentation
2. Check `*_FORMAT.md` for detailed specifications
3. Check `*.bt` or `*.hexpat` for binary templates

### Find Tests
1. Navigate to `EarthTool.*.Tests/`
2. Mirror structure of main project
3. `*Tests.cs` files contain test classes

---

**Need more details?** See:
- [Architecture Documentation](architecture.md)
- [Development Guide](development/README.md)
- [API Reference](api/README.md)
