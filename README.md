![](https://ci.appveyor.com/api/projects/status/github/Arkezar/EarthTool?svg=true)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

# EarthTool

A comprehensive suite of tools for working with Earth 2150 game data files.

## Features

- **WD Archives** - Extract, create, and modify WD archive files
- **TEX Textures** - Convert TEX format to PNG and other image formats
- **MSH Models** - Read and convert MSH 3D models to COLLADA (DAE) format
- **PAR Parameters** - Parse and edit game parameter files
- **GUI Application** - User-friendly graphical interface for archive management

## Applications

### EarthTool.CLI (Command Line Interface)

Command-line tool for batch processing and automation.

**Requirements:** [.NET Runtime 8.0 x64](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime)

**Usage:**
```bash
EarthTool.CLI.exe --help
```

Download the latest release or build from source.

### EarthTool.WD.GUI (Graphical User Interface)

Desktop application with visual interface for managing WD archives.

**Features:**
- Browse archive contents
- Extract files (single or batch)
- Add files to archives
- Create new archives
- View file information and statistics

See [EarthTool.WD.GUI/README.md](EarthTool.WD.GUI/README.md) for detailed documentation.

## Building from Source

```bash
git clone https://github.com/Arkezar/EarthTool.git
cd EarthTool
dotnet build
```

**Build CLI:**
```bash
cd EarthTool.CLI
dotnet publish -c Release
```

**Build GUI:**
```bash
cd EarthTool.WD.GUI
dotnet publish -c Release
```

## Project Structure

- **EarthTool.Common** - Shared interfaces and utilities
- **EarthTool.WD** - WD archive format support
- **EarthTool.TEX** - TEX texture format support
- **EarthTool.MSH** - MSH 3D model format support
- **EarthTool.DAE** - COLLADA export functionality
- **EarthTool.PAR** - Parameter file support
- **EarthTool.CLI** - Command-line interface
- **EarthTool.WD.GUI** - Graphical user interface (Avalonia UI)

## Testing

The project includes comprehensive test suites:

```bash
dotnet test
```

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues.

## Credits

Special thanks to members of Inside Earth discord:

* Guardian#2935 - information on mount points, part offsets, model build templates
* Ninetailed#9436 - information on lights data

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
