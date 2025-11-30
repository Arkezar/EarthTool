# Installation Guide

This guide covers all methods of installing EarthTool on your system.

## Table of Contents

- [System Requirements](#system-requirements)
- [Installation Methods](#installation-methods)
  - [Binary Downloads (Recommended)](#binary-downloads-recommended)
  - [Building from Source](#building-from-source)
  - [Docker (Advanced)](#docker-advanced)
- [Verifying Installation](#verifying-installation)
- [Updating](#updating)
- [Uninstalling](#uninstalling)
- [Troubleshooting](#troubleshooting)

## System Requirements

### Minimum Requirements
- **Operating System**: Windows 10+, Linux (any recent distro), macOS 11+
- **.NET Runtime**: 8.0 or later
- **RAM**: 2 GB
- **Disk Space**: 100 MB

### Recommended Requirements
- **RAM**: 4 GB or more
- **Disk Space**: 500 MB (for development)
- **CPU**: Multi-core processor for faster batch operations

## Installation Methods

### Binary Downloads (Recommended)

This is the easiest method for end users.

#### Step 1: Download .NET Runtime

If you don't have .NET Runtime installed:

**Windows:**
```powershell
# Download and install from:
https://dotnet.microsoft.com/download/dotnet/8.0/runtime
```

**Linux (Ubuntu/Debian):**
```bash
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-runtime-8.0
```

**macOS:**
```bash
# Using Homebrew
brew install dotnet@8
```

#### Step 2: Download EarthTool

Go to [GitHub Releases](https://github.com/Arkezar/EarthTool/releases/latest) and download:

**For CLI:**
- Windows: `EarthTool.CLI-Windows-x64.zip`
- Linux: `EarthTool.CLI-Linux-x64.tar.gz`
- macOS: `EarthTool.CLI-macOS-x64.tar.gz`

**For GUI:**
- Windows: `EarthTool.WD.GUI-Windows-x64.zip`
- Linux: `EarthTool.WD.GUI-Linux-x64.tar.gz`
- macOS: `EarthTool.WD.GUI-macOS-x64.tar.gz`

#### Step 3: Extract and Run

**Windows:**
```powershell
# Extract the ZIP file
Expand-Archive EarthTool.CLI-Windows-x64.zip -DestinationPath C:\EarthTool

# Add to PATH (optional, for CLI)
$env:PATH += ";C:\EarthTool"

# Run
C:\EarthTool\EarthTool.CLI.exe --help
C:\EarthTool\EarthTool.WD.GUI.exe
```

**Linux:**
```bash
# Extract
tar -xzf EarthTool.CLI-Linux-x64.tar.gz -C ~/EarthTool

# Make executable
chmod +x ~/EarthTool/EarthTool.CLI

# Add to PATH (optional)
echo 'export PATH="$HOME/EarthTool:$PATH"' >> ~/.bashrc
source ~/.bashrc

# Run
~/EarthTool/EarthTool.CLI --help
~/EarthTool/EarthTool.WD.GUI
```

**macOS:**
```bash
# Extract
tar -xzf EarthTool.CLI-macOS-x64.tar.gz -C ~/EarthTool

# Make executable
chmod +x ~/EarthTool/EarthTool.CLI

# If you get "unidentified developer" warning, allow in Security settings
# Or run:
xattr -d com.apple.quarantine ~/EarthTool/EarthTool.CLI

# Add to PATH (optional)
echo 'export PATH="$HOME/EarthTool:$PATH"' >> ~/.zshrc
source ~/.zshrc

# Run
~/EarthTool/EarthTool.CLI --help
~/EarthTool/EarthTool.WD.GUI
```

### Building from Source

For developers or users who want the latest features.

#### Prerequisites

- **Git**: For cloning the repository
- **.NET SDK 8.0**: For building
- **IDE** (optional): Visual Studio 2022, VS Code, or Rider

#### Step 1: Install .NET SDK

**Windows:**
```powershell
# Download and install from:
https://dotnet.microsoft.com/download/dotnet/8.0
```

**Linux (Ubuntu/Debian):**
```bash
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

**macOS:**
```bash
brew install dotnet-sdk@8
```

#### Step 2: Clone Repository

```bash
git clone https://github.com/Arkezar/EarthTool.git
cd EarthTool
```

#### Step 3: Build

**Build everything:**
```bash
dotnet build EarthTool.sln --configuration Release
```

**Build CLI only:**
```bash
dotnet build EarthTool.CLI/EarthTool.CLI.csproj --configuration Release
```

**Build GUI only:**
```bash
dotnet build EarthTool.WD.GUI/EarthTool.WD.GUI.csproj --configuration Release
```

#### Step 4: Publish (Optional)

Create self-contained executables:

**CLI (Windows):**
```bash
dotnet publish EarthTool.CLI/EarthTool.CLI.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/cli-win
```

**CLI (Linux):**
```bash
dotnet publish EarthTool.CLI/EarthTool.CLI.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/cli-linux
```

**GUI (Windows):**
```bash
dotnet publish EarthTool.WD.GUI/EarthTool.WD.GUI.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/gui-win
```

#### Step 5: Run

**Without publishing:**
```bash
dotnet run --project EarthTool.CLI/EarthTool.CLI.csproj -- --help
dotnet run --project EarthTool.WD.GUI/EarthTool.WD.GUI.csproj
```

**After publishing:**
```bash
./publish/cli-win/EarthTool.CLI.exe --help
./publish/gui-win/EarthTool.WD.GUI.exe
```

### Docker (Advanced)

For containerized deployments (CLI only).

#### Create Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EarthTool.CLI.dll"]
```

#### Build and Run

```bash
# Build image
docker build -t earthtool:latest .

# Run
docker run --rm -v $(pwd)/data:/data earthtool:latest wd list /data/archive.wd
```

## Verifying Installation

### Check .NET Version
```bash
dotnet --version
# Should show 8.0.x or later
```

### Check EarthTool Version

**CLI:**
```bash
EarthTool.CLI --version
```

**GUI:**
- Open the application
- Go to Help → About
- Version should be displayed

### Run Test Command

**CLI:**
```bash
EarthTool.CLI wd list path/to/some/archive.wd
```

**GUI:**
- Open the application
- Try opening a WD archive
- Verify file list appears

## Updating

### Binary Installation

1. Download the latest release from GitHub
2. Extract to the same location (overwrite existing files)
3. Verify new version: `EarthTool.CLI --version`

### Source Installation

```bash
cd EarthTool
git pull origin main
dotnet build --configuration Release
```

### Automatic Updates (Future)

Planned feature: Built-in update checker in GUI.

## Uninstalling

### Binary Installation

Simply delete the installation directory:

**Windows:**
```powershell
Remove-Item -Recurse -Force C:\EarthTool
```

**Linux/macOS:**
```bash
rm -rf ~/EarthTool
```

**Remove from PATH** (if added):
- Windows: Edit System Environment Variables
- Linux/macOS: Remove the export line from `~/.bashrc` or `~/.zshrc`

### Source Installation

```bash
cd EarthTool
dotnet clean
cd ..
rm -rf EarthTool
```

## Troubleshooting

### "dotnet command not found"

**Problem**: .NET runtime/SDK not installed or not in PATH.

**Solution**:
1. Install .NET 8.0 Runtime or SDK (see above)
2. Restart terminal/command prompt
3. Verify: `dotnet --version`

### "Application won't start on macOS"

**Problem**: macOS Gatekeeper blocks unsigned applications.

**Solution**:
```bash
# Remove quarantine flag
xattr -d com.apple.quarantine ~/EarthTool/EarthTool.CLI
xattr -d com.apple.quarantine ~/EarthTool/EarthTool.WD.GUI

# Or: System Preferences → Security & Privacy → Allow app to run
```

### "Permission denied" on Linux

**Problem**: Executable bit not set.

**Solution**:
```bash
chmod +x ~/EarthTool/EarthTool.CLI
chmod +x ~/EarthTool/EarthTool.WD.GUI
```

### "DLL not found" errors

**Problem**: Missing .NET runtime dependencies.

**Solution**:
1. Ensure .NET Runtime 8.0 is installed
2. Download self-contained build (includes runtime)
3. On Linux, install: `sudo apt-get install libgdiplus`

### GUI doesn't start on Linux

**Problem**: Missing display/GUI libraries.

**Solution** (Ubuntu/Debian):
```bash
sudo apt-get install libx11-dev libice-dev libsm-dev libfontconfig1
```

**Solution** (Arch):
```bash
sudo pacman -S libx11 libice libsm fontconfig
```

### "Archive cannot be opened" errors

**Problem**: File permissions or corrupted archive.

**Solution**:
1. Check file permissions: `ls -l archive.wd`
2. Verify file is not corrupted
3. Try a different archive
4. Check [Troubleshooting Guide](troubleshooting.md)

## Platform-Specific Notes

### Windows

- GUI supports Windows 10 and later
- CLI works on Windows 7+ (with .NET 8.0)
- Antivirus may flag executables (false positive)
- Add exception in Windows Defender if needed

### Linux

- Tested on Ubuntu 20.04+, Debian 11+, Fedora 36+
- Requires X11 or Wayland for GUI
- AppImage support planned
- For server: use CLI only (no GUI dependencies)

### macOS

- Supports macOS 11 (Big Sur) and later
- Both Intel and Apple Silicon supported
- GUI may require allowing in Security settings
- CLI works perfectly in terminal

## Getting Help

If you encounter issues:

1. **Check documentation**: [Troubleshooting Guide](troubleshooting.md)
2. **Search issues**: [GitHub Issues](https://github.com/Arkezar/EarthTool/issues)
3. **Ask for help**: [GitHub Discussions](https://github.com/Arkezar/EarthTool/discussions)
4. **Report bug**: [Open new issue](https://github.com/Arkezar/EarthTool/issues/new)

## Next Steps

- ✅ Installation complete!
- 📖 Read [Quick Start Guide](quickstart.md)
- 💻 Learn [CLI Commands](user-guide-cli.md)
- 🖥️ Explore [GUI Features](user-guide-gui.md)

---

**Installation successful?** Time to start using EarthTool! Check the [Quick Start Guide](quickstart.md).
