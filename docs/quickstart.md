# Quick Start Guide

Get up and running with EarthTool in 5 minutes!

## Prerequisites

- ✅ EarthTool installed ([Installation Guide](installation.md))
- ✅ .NET Runtime 8.0
- ✅ Earth 2150 WD archive files (for testing)

## Choose Your Adventure

### 🖥️ [GUI Quick Start](#gui-quick-start) - Visual Interface
Perfect if you prefer clicking buttons and visual feedback.

### 💻 [CLI Quick Start](#cli-quick-start) - Command Line
Perfect for automation, scripting, and batch operations.

---

## GUI Quick Start

### Step 1: Launch the Application

**Windows:**
```powershell
EarthTool.WD.GUI.exe
```

**Linux/macOS:**
```bash
./EarthTool.WD.GUI
```

### Step 2: Open an Archive

1. Click **File → Open Archive** (or press `Ctrl+O`)
2. Navigate to your Earth 2150 directory
3. Select a `.wd` file (e.g., `Data01.wd`)
4. Click **Open**

✅ You should now see the archive contents in the main window!

### Step 3: Browse Files

- **Sort**: Click on column headers (Name, Size, Ratio)
- **Filter**: Use the search box (future feature)
- **Details**: Click a file to see details in the info panel

### Step 4: Extract Files

**Extract a single file:**
1. Select a file in the list
2. Click **Archive → Extract Selected** (or toolbar button)
3. Choose destination folder
4. Click **Select Folder**

**Extract all files:**
1. Click **Archive → Extract All** (or press `Ctrl+E`)
2. Choose destination folder
3. Wait for extraction to complete

✅ **Success!** Your files are now extracted!

### Step 5: Create a New Archive

1. Click **File → New Archive** (or press `Ctrl+N`)
2. Click **Archive → Add Files** (or press `Ctrl+A`)
3. Select files to add
4. Click **File → Save Archive As**
5. Choose name and location
6. Click **Save**

✅ **You've created your first archive!**

---

## CLI Quick Start

### Step 1: Verify Installation

```bash
EarthTool.CLI --help
```

You should see a list of available commands.

### Step 2: List Archive Contents

```bash
EarthTool.CLI wd list path/to/archive.wd
```

Example output:
```
Archive: Data01.wd
Files: 1234
Total Size: 450.2 MB

FileName                    Compressed  Decompressed  Ratio
models/unit.msh            45.2 KB     120.5 KB      37.5%
textures/unit.tex          234.1 KB    1.2 MB        19.5%
...
```

### Step 3: Extract Files

**Extract entire archive:**
```bash
EarthTool.CLI wd extract archive.wd -o ./output
```

**Extract specific files (by pattern):**
```bash
EarthTool.CLI wd extract archive.wd --filter "*.msh" -o ./models
```

**Extract from multiple archives:**
```bash
EarthTool.CLI wd extract *.wd -o ./all_files
```

### Step 4: Create an Archive

```bash
EarthTool.CLI wd create my_archive.wd -i ./my_files -r
```

Options:
- `-i, --input`: Input directory or file
- `-r, --recursive`: Include subdirectories
- `--no-compress`: Don't compress files

### Step 5: Modify an Archive

**Add files:**
```bash
EarthTool.CLI wd add archive.wd file1.msh file2.tex
```

**Remove files:**
```bash
EarthTool.CLI wd remove archive.wd --filter "*.tmp"
```

**Get detailed info:**
```bash
EarthTool.CLI wd info archive.wd
```

---

## Common Tasks

### Task 1: Extract All Earth 2150 Archives

**GUI:**
1. Navigate to Earth 2150 directory
2. Open each .wd file
3. Extract all to separate folders

**CLI:**
```bash
cd "C:\Program Files (x86)\Earth 2150"
for file in *.wd; do
  EarthTool.CLI wd extract "$file" -o "./extracted_$(basename $file .wd)"
done
```

### Task 2: Create a Texture Mod

**CLI:**
```bash
# 1. Extract original textures
EarthTool.CLI wd extract Data01.wd --filter "*.tex" -o ./textures

# 2. Convert to PNG for editing
for file in ./textures/*.tex; do
  EarthTool.CLI tex "$file" -o "${file%.tex}.png"
done

# 3. Edit PNG files in your image editor

# 4. Create new archive with modified textures
EarthTool.CLI wd create TextureMod.wd -i ./modified_textures -r
```

### Task 3: Export Models for Viewing

```bash
# Extract and convert MSH to DAE
EarthTool.CLI wd extract archive.wd --filter "*.msh" -o ./models
for file in ./models/*.msh; do
  EarthTool.CLI msh "$file" -o "${file%.msh}.dae"
done

# Now open .dae files in Blender or other 3D software
```

### Task 4: Backup Game Files

**GUI:**
1. Open archive
2. Extract all to backup location
3. Archive info is preserved

**CLI:**
```bash
# Create organized backup
mkdir -p ./backup/$(date +%Y%m%d)
EarthTool.CLI wd extract *.wd -o ./backup/$(date +%Y%m%d)
```

### Task 5: Analyze Archive Contents

```bash
# Get detailed statistics
EarthTool.CLI wd info archive.wd

# Output includes:
# - File count
# - Total sizes
# - Compression ratios
# - File type breakdown
# - Largest files
# - Archive metadata
```

---

## Tips & Tricks

### GUI Tips

1. **Keyboard shortcuts**: 
   - `Ctrl+O`: Open archive
   - `Ctrl+S`: Save archive
   - `Ctrl+E`: Extract all
   - `Del`: Remove selected file

2. **Sort by clicking**: Click column headers to sort

3. **Info panel**: Shows archive statistics and metadata

4. **Unsaved changes**: Window title shows `*` when modified

### CLI Tips

1. **Help for any command**:
   ```bash
   EarthTool.CLI wd extract --help
   ```

2. **Use wildcards** for batch operations:
   ```bash
   EarthTool.CLI wd extract Data*.wd -o ./output
   ```

3. **Verbose output** (if available):
   ```bash
   EarthTool.CLI wd extract archive.wd -o ./out --verbose
   ```

4. **Redirect output** for logging:
   ```bash
   EarthTool.CLI wd list archive.wd > archive_contents.txt
   ```

---

## Common Issues

### ❌ "Archive cannot be opened"

**Solution**: Verify the file is a valid WD archive from Earth 2150.

### ❌ "Permission denied"

**Solution**: 
- Ensure you have read permissions on the archive
- For creation, ensure write permissions on output directory

### ❌ "Out of memory"

**Solution**: 
- Close other applications
- For very large archives, extract in chunks using filters

### ❌ GUI not responding

**Solution**:
- Wait for long operations to complete
- Check progress bar
- For very large archives, use CLI instead

---

## Next Steps

### Learn More

- **CLI Users**: Read [CLI User Guide](user-guide-cli.md) for all commands
- **GUI Users**: Read [GUI User Guide](user-guide-gui.md) for all features
- **Developers**: Check [Development Guide](development/README.md)
- **Modders**: See [WD Format Specification](WD_FORMAT.md)

### Advanced Usage

- **WD Commands**: [Complete command reference](WD_COMMANDS.md)
- **Format Conversions**: [Converting between formats](conversions.md)
- **Troubleshooting**: [Common issues and solutions](troubleshooting.md)

### Get Involved

- **Report Issues**: [GitHub Issues](https://github.com/Arkezar/EarthTool/issues)
- **Ask Questions**: [GitHub Discussions](https://github.com/Arkezar/EarthTool/discussions)
- **Contribute**: [Contributing Guide](../CONTRIBUTING.md)

---

## Quick Reference Card

### GUI Shortcuts

| Action | Shortcut |
|--------|----------|
| Open Archive | `Ctrl+O` |
| New Archive | `Ctrl+N` |
| Save | `Ctrl+S` |
| Save As | `Ctrl+Shift+S` |
| Extract All | `Ctrl+E` |
| Add Files | `Ctrl+A` |
| Remove File | `Del` |

### CLI Commands

| Command | Purpose |
|---------|---------|
| `wd list <archive>` | List contents |
| `wd extract <archive>` | Extract files |
| `wd create <archive>` | Create new archive |
| `wd add <archive> <files>` | Add files |
| `wd remove <archive>` | Remove files |
| `wd info <archive>` | Show details |
| `msh <file>` | Convert MSH to DAE |
| `tex <file>` | Convert TEX to PNG |
| `par convert <file>` | Convert PAR to JSON |

---

**🎉 Congratulations!** You're now ready to use EarthTool!

Need help? Check the [FAQ](faq.md) or [Troubleshooting Guide](troubleshooting.md).
