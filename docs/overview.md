# EarthTool - Project Overview

## Introduction

EarthTool is a comprehensive, open-source suite of tools designed for working with game files from **Earth 2150**, the classic real-time strategy game by Reality Pump Studios. The project provides both command-line and graphical interfaces for managing, extracting, converting, and modifying game assets.

## What is Earth 2150?

Earth 2150 is a real-time strategy game released in 2000, featuring:
- Three unique factions with distinct technologies
- Fully 3D graphics and destructible environments
- Complex unit customization system
- Campaign and multiplayer modes

EarthTool enables players, modders, and researchers to access and modify the game's internal file formats.

## Project Goals

1. **Accessibility**: Make Earth 2150 modding accessible to everyone
2. **Preservation**: Document and preserve Earth 2150 file formats
3. **Cross-Platform**: Support Windows, Linux, and macOS
4. **Modern Tech**: Built with modern .NET and best practices
5. **Open Source**: Fully open-source under MIT license

## Key Features

### 🗄️ WD Archive Management
- **Extract** files from WD archives
- **Create** new WD archives
- **Modify** existing archives (add/remove files)
- **Inspect** archive contents and metadata
- **Compression** support (ZLib)
- **Batch processing** multiple archives

### 🎨 TEX Texture Conversion
- Convert TEX textures to PNG
- Support for multiple texture formats
- Mipmap extraction
- Batch conversion

### 🎮 MSH 3D Model Conversion
- Read MSH 3D model format
- Export to COLLADA (DAE) format
- Preserve geometry, materials, and UV mapping
- Animation support (future)

### ⚙️ PAR Parameter Editing
- Parse game parameter files
- Convert to/from JSON for easy editing
- Support for all entity types
- Validation and error checking

### 💻 Dual Interface
- **CLI**: Command-line tool for automation and batch processing
- **GUI**: User-friendly graphical interface built with Avalonia UI

## Architecture Highlights

### Technology Stack
- **.NET 8.0**: Modern, cross-platform framework
- **C# 12**: Latest language features
- **Avalonia UI**: Cross-platform GUI framework
- **ReactiveUI**: MVVM framework for GUI
- **Spectre.Console**: Beautiful CLI interface
- **xUnit**: Comprehensive testing

### Design Principles
- **Modular architecture**: Each format has its own library
- **Dependency injection**: Clean, testable code
- **MVVM pattern**: Separation of concerns in GUI
- **Interface-based design**: Easy to extend and mock
- **Comprehensive testing**: 92%+ code coverage
- **Documentation-first**: Well-documented code and APIs

## Project Structure

```
EarthTool/
├── EarthTool.Common         # Shared interfaces and utilities
├── EarthTool.WD             # WD archive support
├── EarthTool.TEX            # TEX texture support
├── EarthTool.MSH            # MSH 3D model support
├── EarthTool.DAE            # COLLADA export support
├── EarthTool.PAR            # PAR parameter support
├── EarthTool.CLI            # Command-line interface
├── EarthTool.WD.GUI         # Graphical user interface
├── EarthTool.*.Tests        # Unit tests for each module
├── docs/                    # Comprehensive documentation
└── scripts/                 # Utility scripts
```

## Use Cases

### For Players
- Extract game assets for viewing
- Create custom texture packs
- Backup and organize game files

### For Modders
- Create new units and buildings
- Design custom maps and missions
- Modify game parameters and balance
- Replace textures and models
- Create total conversion mods

### For Researchers
- Study game file formats
- Analyze game assets and data structures
- Document proprietary formats
- Reverse engineer game mechanics

### For Developers
- Learn from real-world file format implementation
- Contribute to game preservation
- Build tools on top of EarthTool libraries

## Supported Platforms

### Operating Systems
- ✅ Windows (x64)
- ✅ Linux (x64, ARM, ARM64)
- ✅ macOS (x64, ARM64/Apple Silicon)

### Requirements
- .NET Runtime 8.0 or later
- 2 GB RAM minimum (4 GB recommended)
- 100 MB disk space

## Getting Started

1. **Installation**: See [Installation Guide](installation.md)
2. **Quick Start**: Follow [Quick Start Guide](quickstart.md)
3. **Choose Your Tool**:
   - Command-line power user? → [CLI User Guide](user-guide-cli.md)
   - Prefer visual interface? → [GUI User Guide](user-guide-gui.md)

## Project Status

### Current Version
- **CLI**: v1.x
- **GUI**: v1.x
- **Libraries**: Stable

### Supported Formats

| Format | Read | Write | Convert | Status |
|--------|------|-------|---------|--------|
| WD Archives | ✅ | ✅ | - | Stable |
| TEX Textures | ✅ | 🚧 | ✅ PNG | Stable |
| MSH Models | ✅ | ❌ | ✅ DAE | Stable |
| PAR Parameters | ✅ | ✅ | ✅ JSON | Stable |
| DAE Export | - | ✅ | - | Stable |

Legend: ✅ Complete | 🚧 Partial | ❌ Not Implemented

### Roadmap

#### Version 1.x (Current)
- ✅ Complete WD archive support
- ✅ TEX to PNG conversion
- ✅ MSH to DAE conversion
- ✅ PAR parameter editing
- ✅ Cross-platform GUI
- ✅ Comprehensive testing

#### Version 2.x (Planned)
- 🔄 Animation support for MSH models
- 🔄 Advanced texture editing
- 🔄 In-place archive editing (memory-mapped files)
- 🔄 Plugin system for custom formats
- 🔄 Scripting API

#### Future Considerations
- Model editor integration
- Map editor support
- Mod packaging system
- Steam Workshop integration

## Community & Support

### Communication Channels
- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Questions and community support
- **Discord**: Inside Earth discord server (community)

### Contributing
We welcome contributions! See:
- [Contributing Guide](../CONTRIBUTING.md)
- [Development Guide](development/README.md)
- [Code Style Guide](development/code-style.md)

### Credits
Special thanks to:
- **Reality Pump Studios**: Original game developers
- **Inside Earth Discord Community**: Documentation and research
  - Guardian#2935: Mount points and model templates
  - Ninetailed#9436: Lighting data information
- **Contributors**: Everyone who has contributed code, documentation, or bug reports

## License

EarthTool is released under the **MIT License**, which means:
- ✅ Free to use for any purpose
- ✅ Free to modify and distribute
- ✅ Can be used in commercial projects
- ⚠️ Provided "as-is" without warranty

See [LICENSE](../LICENSE) for full details.

## Disclaimer

EarthTool is an independent project and is not affiliated with, endorsed by, or sponsored by Reality Pump Studios or TopWare Interactive. Earth 2150 is a trademark of its respective owners.

This tool is intended for:
- Educational purposes
- Game modding
- File format research
- Personal use

Users are responsible for complying with the game's EULA and copyright laws.

## Next Steps

- 📖 Read the [Installation Guide](installation.md)
- 🚀 Try the [Quick Start Guide](quickstart.md)
- 💻 Explore [CLI Commands](user-guide-cli.md)
- 🖥️ Try the [GUI Application](user-guide-gui.md)
- 🛠️ Check out [Developer Documentation](development/README.md)

---

**Questions?** Check the [FAQ](faq.md) or open a [GitHub Discussion](https://github.com/Arkezar/EarthTool/discussions).
