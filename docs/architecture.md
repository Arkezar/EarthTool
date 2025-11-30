# EarthTool Architecture

Comprehensive overview of EarthTool's system architecture, design patterns, and implementation details.

## Table of Contents

- [System Overview](#system-overview)
- [Architectural Principles](#architectural-principles)
- [Module Structure](#module-structure)
- [Design Patterns](#design-patterns)
- [Data Flow](#data-flow)
- [Technology Stack](#technology-stack)
- [Dependency Graph](#dependency-graph)
- [Extension Points](#extension-points)

## System Overview

EarthTool follows a **layered, modular architecture** designed for:
- **Separation of concerns**: Each format in its own library
- **Testability**: Interface-based design with DI
- **Extensibility**: Easy to add new formats or features
- **Reusability**: Core libraries can be used in other projects

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌───────────────────┐          ┌──────────────────────┐    │
│  │  EarthTool.CLI    │          │  EarthTool.WD.GUI    │    │
│  │                   │          │                      │    │
│  │  Spectre.Console  │          │  Avalonia UI        │    │
│  │  Commands         │          │  ReactiveUI         │    │
│  └───────────────────┘          └──────────────────────┘    │
└──────────────────┬────────────────────────┬──────────────────┘
                   │                        │
┌──────────────────┴────────────────────────┴──────────────────┐
│                    Service Layer                             │
│  ┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐          │
│  │  WD  │  │ MSH  │  │ DAE  │  │ PAR  │  │ TEX  │          │
│  │      │  │      │  │      │  │      │  │      │          │
│  │ Svc  │  │ Svc  │  │ Svc  │  │ Svc  │  │ Svc  │          │
│  └───┬──┘  └───┬──┘  └───┬──┘  └───┬──┘  └───┬──┘          │
│      │         │          │         │         │              │
│  ┌───┴─────────┴──────────┴─────────┴─────────┴───┐         │
│  │          EarthTool.Common                       │         │
│  │  Interfaces • Utilities • Base Classes          │         │
│  └──────────────────────────────────────────────────┘         │
└─────────────────────────────────────────────────────────────┘
```

## Architectural Principles

### 1. Separation of Concerns

Each module has a single, well-defined responsibility:

| Module | Responsibility |
|--------|---------------|
| **EarthTool.Common** | Shared interfaces, base classes, utilities |
| **EarthTool.WD** | WD archive reading, writing, manipulation |
| **EarthTool.MSH** | MSH 3D model parsing |
| **EarthTool.DAE** | COLLADA 1.4.1 export |
| **EarthTool.PAR** | Parameter file parsing and serialization |
| **EarthTool.TEX** | Texture format handling |
| **EarthTool.CLI** | Command-line interface |
| **EarthTool.WD.GUI** | Graphical user interface |

### 2. Dependency Inversion

All dependencies flow towards abstractions (interfaces), not concrete implementations.

```csharp
// ✅ Good: Depend on interface
public class ArchiverService : IArchiver
{
    private readonly IArchiveFactory _factory;
    private readonly ICompressor _compressor;
    
    public ArchiverService(
        IArchiveFactory factory,
        ICompressor compressor)
    {
        _factory = factory;
        _compressor = compressor;
    }
}

// ❌ Bad: Depend on concrete class
public class ArchiverService
{
    private readonly ArchiveFactory _factory; // Concrete!
}
```

### 3. Interface Segregation

Interfaces are small and focused:

```csharp
// Many small interfaces
public interface IArchive { }
public interface IArchiveFactory { }
public interface IArchiver { }
public interface ICompressor { }
public interface IDecompressor { }

// Not: One giant IArchiveEverything interface
```

### 4. Single Responsibility

Each class has one reason to change:

```csharp
// ✅ Good: Separate concerns
public class ArchiveFactory { /* Create/open archives */ }
public class ArchiverService { /* High-level operations */ }
public class CompressorService { /* Compression only */ }

// ❌ Bad: Does everything
public class ArchiveManager { /* Create, open, compress, extract... */ }
```

### 5. Open/Closed Principle

Open for extension, closed for modification:

```csharp
// Extensible through interfaces
public interface IArchiveDataSource : IDisposable
{
    ReadOnlyMemory<byte> Data { get; }
}

// Implementations can vary
public class InMemoryArchiveDataSource : IArchiveDataSource { }
public class MappedArchiveDataSource : IArchiveDataSource { }
// Future: StreamArchiveDataSource, NetworkArchiveDataSource...
```

## Module Structure

### EarthTool.Common

**Purpose**: Shared functionality across all modules

**Key Components**:
```
EarthTool.Common/
├── Interfaces/
│   ├── IArchive.cs          - Archive container
│   ├── IArchiveItem.cs      - File within archive
│   ├── IArchiveFactory.cs   - Archive creation
│   ├── IArchiver.cs         - High-level operations
│   ├── ICompressor.cs       - Compression interface
│   └── IDecompressor.cs     - Decompression interface
├── Models/
│   └── EarthInfo.cs         - Common header format
├── Factories/
│   └── EarthInfoFactory.cs  - Header creation
├── Validation/
│   └── PathValidator.cs     - Path sanitization
├── Enums/
│   ├── FileType.cs          - Resource types
│   └── FileFlags.cs         - File metadata flags
└── Bases/
    ├── Reader.cs            - Base binary reader
    └── Writer.cs            - Base binary writer
```

**Design Pattern**: Repository + Factory + Strategy

### EarthTool.WD

**Purpose**: WD archive format implementation

**Key Components**:
```
EarthTool.WD/
├── Models/
│   ├── Archive.cs           - IArchive implementation
│   ├── ArchiveItem.cs       - IArchiveItem implementation
│   ├── InMemoryArchiveDataSource.cs
│   └── MappedArchiveDataSource.cs
├── Services/
│   ├── ArchiverService.cs   - IArchiver implementation
│   ├── CompressorService.cs - ICompressor implementation
│   └── DecompressorService.cs
├── Factories/
│   └── ArchiveFactory.cs    - IArchiveFactory implementation
└── WDExtractor.cs           - Legacy compatibility
```

**Design Patterns**: 
- **Factory**: ArchiveFactory for object creation
- **Strategy**: Different data source strategies
- **Facade**: ArchiverService simplifies complex operations
- **Disposal**: Proper resource cleanup with IDisposable

### EarthTool.MSH

**Purpose**: MSH 3D model format parsing

**Key Components**:
```
EarthTool.MSH/
├── Interfaces/
│   ├── IMesh.cs, IMeshPart.cs, IVertex.cs
│   ├── IMaterial.cs, ITexture.cs
│   └── IAnimation.cs, ILight.cs
├── Models/
│   ├── Mesh.cs              - Root mesh model
│   ├── MeshPart.cs          - Geometry parts
│   ├── Vertex.cs, Triangle.cs
│   ├── Material.cs, Texture.cs
│   └── Animation types
├── Collections/
│   └── ModelTree.cs         - Hierarchical structure
├── Services/
│   └── EarthMeshReader.cs   - Binary parser
└── Extensions/
    └── ModelPartExtensions.cs
```

**Design Patterns**:
- **Composite**: ModelTree for hierarchies
- **Builder**: Progressive mesh construction
- **Visitor**: Tree traversal

### EarthTool.DAE

**Purpose**: COLLADA 1.4.1 export

**Key Components**:
```
EarthTool.DAE/
├── Collada141/
│   ├── COLLADA.cs           - Root element
│   ├── Geometry.cs, Material.cs
│   ├── Scene.cs, Node.cs
│   └── [500+ COLLADA elements]
├── Elements/
│   ├── ColladaModelFactory.cs
│   ├── GeometryFactory.cs
│   ├── MaterialFactory.cs
│   └── AnimationsFactory.cs
└── Services/
    └── ColladaMeshWriter.cs  - DAE serialization
```

**Design Patterns**:
- **Factory Method**: Factories for each COLLADA element type
- **Builder**: Complex object construction
- **Serialization**: XML serialization with attributes

### EarthTool.PAR

**Purpose**: Parameter file parsing and editing

**Key Components**:
```
EarthTool.PAR/
├── Models/
│   ├── Abstracts/           - Base entity types
│   ├── [38 entity types]    - Units, buildings, research, etc.
│   └── Serialization/       - JSON converters
├── Services/
│   ├── ParameterReader.cs   - Binary parser
│   └── ParameterWriter.cs   - Binary writer
├── Factories/
│   └── EntityFactory.cs     - Entity type resolution
└── Enums/
    └── [32 enums]           - All parameter types
```

**Design Patterns**:
- **Factory**: Entity creation based on type ID
- **Strategy**: Different serialization strategies per entity
- **Template Method**: Base entity defines structure

### EarthTool.TEX

**Purpose**: Texture format handling

**Key Components**:
```
EarthTool.TEX/
├── Models/
│   ├── TexFile.cs           - ITexFile implementation
│   ├── TexHeader.cs         - Texture metadata
│   └── TexImage.cs          - Image data
├── Services/
│   └── TexReader.cs         - TEX parser
└── Enums/
    ├── TextureType.cs
    └── TextureSubType.cs
```

**Design Patterns**:
- **Facade**: TexFile simplifies complex structure
- **Lazy Loading**: Images loaded on demand

### EarthTool.CLI

**Purpose**: Command-line interface

**Key Components**:
```
EarthTool.CLI/
├── Commands/
│   ├── WD/                  - WD archive commands
│   │   ├── ListCommand.cs
│   │   ├── ExtractCommand.cs
│   │   ├── CreateCommand.cs
│   │   ├── AddCommand.cs
│   │   ├── RemoveCommand.cs
│   │   └── InfoCommand.cs
│   ├── MSH/ConvertCommand.cs
│   ├── DAE/ConvertCommand.cs
│   ├── TEX/ConvertCommand.cs
│   └── PAR/ConvertCommand.cs
└── Program.cs               - Entry point + DI setup
```

**Design Patterns**:
- **Command**: Each CLI command is a command object
- **Dependency Injection**: Services injected via constructor
- **Template Method**: Base command class with Execute()

### EarthTool.WD.GUI

**Purpose**: Graphical user interface

**Key Components**:
```
EarthTool.WD.GUI/
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── ArchiveItemViewModel.cs
│   └── ArchiveInfoViewModel.cs
├── Views/
│   └── MainWindow.axaml     - XAML UI
├── Services/
│   ├── DialogService.cs     - File dialogs
│   └── NotificationService.cs
├── Converters/
│   ├── BytesToHumanReadableConverter.cs
│   └── FileFlagsToStringConverter.cs
└── App.axaml.cs             - DI configuration
```

**Design Patterns**:
- **MVVM**: Model-View-ViewModel architecture
- **Observer**: ReactiveUI for property changes
- **Command**: ReactiveCommand for user actions
- **Service Locator**: Dependency injection container

## Design Patterns

### Creational Patterns

#### Factory Pattern
```csharp
public interface IArchiveFactory
{
    IArchive NewArchive();
    IArchive OpenArchive(string path);
}

public class ArchiveFactory : IArchiveFactory
{
    public IArchive NewArchive()
    {
        var header = _earthInfoFactory.CreateHeader(/* ... */);
        return new Archive(header);
    }
    
    public IArchive OpenArchive(string path)
    {
        // Complex creation logic hidden
    }
}
```

**Used in**: ArchiveFactory, EntityFactory, EarthInfoFactory

#### Builder Pattern
```csharp
// Building complex COLLADA documents
var factory = new ColladaModelFactory();
COLLADA collada = factory.CreateDocument(mesh);
// Complex XML structure built incrementally
```

**Used in**: COLLADA export, mesh parsing

### Structural Patterns

#### Facade Pattern
```csharp
public interface IArchiver
{
    void ExtractAll(IArchive archive, string outputPath);
    // Simple interface for complex operations
}

// Hides complexity of iteration, decompression, file I/O
```

**Used in**: ArchiverService, TexFile

#### Composite Pattern
```csharp
public class ModelTree : IEnumerable<IMeshPart>
{
    // Tree structure for hierarchical meshes
    public void Add(IMeshPart part) { }
    public IEnumerator<IMeshPart> GetEnumerator() { }
}
```

**Used in**: MSH model hierarchies

#### Strategy Pattern
```csharp
public interface IArchiveDataSource
{
    ReadOnlyMemory<byte> Data { get; }
}

// Different strategies for data access
public class InMemoryArchiveDataSource : IArchiveDataSource { }
public class MappedArchiveDataSource : IArchiveDataSource { }
```

**Used in**: Data source strategies, compression

### Behavioral Patterns

#### Command Pattern
```csharp
// CLI commands
public class ExtractCommand : Command<ExtractSettings>
{
    public override int Execute(CommandContext context, ExtractSettings settings)
    {
        // Encapsulates all logic for extract operation
    }
}

// GUI commands
public ReactiveCommand<Unit, Unit> ExtractSelectedCommand { get; }
```

**Used in**: All CLI commands, GUI commands

#### Observer Pattern
```csharp
// ReactiveUI property changes
private string _statusMessage;
public string StatusMessage
{
    get => _statusMessage;
    set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
}

// Observers automatically notified of changes
```

**Used in**: GUI ViewModels (ReactiveUI)

#### Template Method Pattern
```csharp
public abstract class EntityBase
{
    // Template method
    public void Read(BinaryReader reader)
    {
        ReadCommonData(reader);
        ReadSpecificData(reader); // Subclasses implement
    }
    
    protected abstract void ReadSpecificData(BinaryReader reader);
}
```

**Used in**: PAR entity reading/writing

## Data Flow

### Reading a WD Archive

```
User Request
    │
    ▼
IArchiver.OpenArchive(path)
    │
    ▼
IArchiveFactory.OpenArchive(path)
    │
    ├─► Open MemoryMappedFile
    ├─► Read & decompress header (EarthInfo)
    ├─► Seek to end, read descriptor length
    ├─► Read & decompress central directory
    ├─► Parse item entries
    │   └─► Create ArchiveItem for each
    │       └─► MappedArchiveDataSource (lazy)
    └─► Return IArchive
    │
    ▼
User accesses IArchive.Items
    │
    ▼
ArchiveItem.Data accessed
    │
    ▼
MappedArchiveDataSource.Data
    │
    ├─► Create view of memory-mapped file
    ├─► Decompress if needed
    └─► Return ReadOnlyMemory<byte>
```

### Creating a WD Archive

```
User Request
    │
    ▼
IArchiver.CreateArchive()
    │
    ▼
IArchiveFactory.NewArchive()
    │
    ├─► Create EarthInfo header
    ├─► Set GUID, timestamps
    └─► Return new Archive (empty)
    │
    ▼
IArchiver.AddFile(archive, path, compress)
    │
    ├─► Read file from disk
    ├─► Compress if requested
    ├─► Create ArchiveItem
    │   └─► InMemoryArchiveDataSource
    └─► Archive.AddItem(item)
    │
    ▼
IArchiver.SaveArchive(archive, path)
    │
    ▼
Archive.ToByteArray(compressor)
    │
    ├─► Compress & write header
    ├─► Write each item's data
    │   └─► Record offsets/sizes
    ├─► Build central directory
    ├─► Compress & write central directory
    ├─► Write descriptor length
    └─► Return byte[]
    │
    ▼
File.WriteAllBytes(path, data)
```

## Technology Stack

### Core Technologies
- **.NET 8.0**: Latest LTS version
- **C# 12**: Latest language features
- **System.IO.Compression**: ZLib compression
- **System.IO.MemoryMappedFiles**: Efficient large file handling

### CLI Technologies
- **Spectre.Console**: Rich console UI
- **Spectre.Console.Cli**: Command framework
- **Microsoft.Extensions.Hosting**: DI and configuration

### GUI Technologies
- **Avalonia UI 11.x**: Cross-platform UI framework
- **ReactiveUI**: MVVM framework
- **Microsoft.Extensions.DependencyInjection**: DI container
- **System.Reactive**: Reactive extensions

### Testing Technologies
- **xUnit**: Test framework
- **FluentAssertions**: Assertion library
- **NSubstitute**: Mocking framework
- **Coverlet**: Code coverage

## Dependency Graph

```
┌────────────────────┐     ┌──────────────────────┐
│  EarthTool.CLI     │     │  EarthTool.WD.GUI    │
└─────────┬──────────┘     └───────────┬──────────┘
          │                            │
          ├─────────┬──────────────────┤
          │         │                  │
          ▼         ▼                  ▼
    ┌─────────┐ ┌────────┐      ┌─────────┐
    │ WD  │ MSH│ │ DAE    │      │ TEX     │
    │     │    │ │        │      │         │
    └─────┴────┘ └────┬───┘      └────┬────┘
          │           │               │
          │           │               │
          └───────────┴───────────────┘
                      │
                      ▼
            ┌─────────────────┐
            │ EarthTool.Common│
            └─────────────────┘
```

**Dependency Rules**:
1. Common has no dependencies (except .NET)
2. Format libraries depend only on Common
3. Applications depend on format libraries
4. No circular dependencies

## Extension Points

### Adding a New Format

1. **Create library**: `EarthTool.XYZ`
2. **Define interfaces** in `EarthTool.Common` (if needed)
3. **Implement models and services**
4. **Add DI extension method**: `IServiceCollection.AddXyzServices()`
5. **Create CLI command** in `EarthTool.CLI/Commands/XYZ/`
6. **Register in Program.cs**

Example:
```csharp
// 1. New library: EarthTool.XYZ
public class XyzReader
{
    public XyzFile Read(string path) { }
}

// 2. DI extension
public static class HostExtensions
{
    public static IServiceCollection AddXyzServices(
        this IServiceCollection services)
    {
        services.AddSingleton<XyzReader>();
        return services;
    }
}

// 3. CLI command
public class ConvertCommand : Command<Settings>
{
    private readonly XyzReader _reader;
    
    public ConvertCommand(XyzReader reader)
    {
        _reader = reader;
    }
}

// 4. Register
builder.ConfigureServices(services => services
    .AddCommonServices()
    .AddXyzServices()); // Add new format
```

### Adding a CLI Command

1. **Create settings class**: Inherit from `CommandSettings`
2. **Create command class**: Inherit from `Command<TSettings>`
3. **Inject dependencies** via constructor
4. **Implement Execute()** method
5. **Register in Program.cs**

### Extending GUI

1. **Create ViewModel**: Inherit from `ViewModelBase`
2. **Create View**: XAML file
3. **Register in DI** container
4. **Add to navigation/menu**

## Performance Considerations

### Memory Management
- **Memory-mapped files**: For large archives (avoids loading everything)
- **Lazy loading**: Data loaded only when accessed
- **Disposal pattern**: Proper cleanup of unmanaged resources
- **Streaming**: For very large files (future)

### Optimization Strategies
- **Parallel processing**: Batch operations can be parallelized
- **Caching**: Header/metadata cached after first read
- **Efficient algorithms**: ZLib for good compression ratios
- **Minimal allocations**: Reuse buffers where possible

## Security Considerations

### Path Traversal Prevention
```csharp
// PathValidator sanitizes file names
public static string SanitizeFileName(string fileName)
{
    // Remove ../, absolute paths, etc.
}
```

### Validation
- Archive headers validated on open
- File sizes checked before allocation
- Compression bomb protection (future)

---

**For implementation details**, see:
- [API Reference](api/README.md)
- [Development Guide](development/README.md)
- [Testing Guide](development/testing.md)

**For format specifications**, see:
- [WD Format](WD_FORMAT.md)
- [MSH Format](formats/msh-format.md)
- [PAR Format](formats/par-format.md)
