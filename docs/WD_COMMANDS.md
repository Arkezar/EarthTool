# WD Archive Commands

This document describes all available commands for working with WD archive files.

## Commands Overview

- `wd list` - Display archive contents
- `wd extract` - Extract files from archive
- `wd create` - Create new archive
- `wd add` - Add files to existing archive
- `wd remove` - Remove files from archive
- `wd info` - Display detailed archive information

---

## wd list

Display the contents of a WD archive.

### Usage
```bash
earthtool wd list <ArchivePath> [options]
```

### Arguments
- `<ArchivePath>` - Path to the WD archive file

### Options
- `-d, --detailed` - Show detailed information (size, compression ratio, flags)
- `-f, --filter <Pattern>` - Filter files by pattern (e.g., `*.msh`, `*.tex`)

### Examples
```bash
# List all files in archive
earthtool wd list data.wd

# List with detailed information
earthtool wd list data.wd --detailed

# List only MSH files
earthtool wd list data.wd --filter "*.msh"
```

---

## wd extract

Extract files from one or more WD archives.

### Usage
```bash
earthtool wd extract <ArchivePath...> [options]
```

### Arguments
- `<ArchivePath...>` - Path(s) to the WD archive file(s) (supports multiple archives)

### Options
- `-o, --output <OutputPath>` - Output directory (default: archive directory)
- `-f, --filter <Pattern>` - Extract only files matching pattern
- `-l, --list <Files>` - Comma-separated list of specific files to extract

### Examples
```bash
# Extract all files from single archive
earthtool wd extract data.wd

# Extract from multiple archives at once
earthtool wd extract data1.wd data2.wd data3.wd

# Extract all WD files in directory
earthtool wd extract *.wd

# Extract to specific directory
earthtool wd extract data.wd -o ./output

# Extract only MSH files from multiple archives
earthtool wd extract *.wd --filter "*.msh"

# Extract specific files
earthtool wd extract data.wd --list "model.msh,texture.tex"

# Combine filter and output
earthtool wd extract data.wd --filter "*.tex" -o ./textures

# Extract from multiple archives with filtering
earthtool wd extract archive1.wd archive2.wd --filter "*.msh" -o ./models
```

---

## wd create

Create a new WD archive from files or directory.

### Usage
```bash
earthtool wd create <ArchivePath> [options]
```

### Arguments
- `<ArchivePath>` - Path for the new archive file

### Options
- `-i, --input <InputPath>` - Input directory or file (required)
- `--no-compress` - Do not compress files in the archive
- `-r, --recursive` - Include subdirectories recursively

### Examples
```bash
# Create archive from directory (top-level only)
earthtool wd create data.wd --input ./files

# Create archive with all subdirectories
earthtool wd create data.wd --input ./files --recursive

# Create archive without compression
earthtool wd create data.wd --input ./files --no-compress

# Create archive from single file
earthtool wd create data.wd --input model.msh
```

---

## wd add

Add files to an existing WD archive.

### Usage
```bash
earthtool wd add <ArchivePath> <Files...> [options]
```

### Arguments
- `<ArchivePath>` - Path to the existing archive file
- `<Files...>` - One or more files to add

### Options
- `--no-compress` - Do not compress files being added
- `-o, --output <OutputPath>` - Save to different file (default: overwrites original)

### Examples
```bash
# Add single file
earthtool wd add data.wd model.msh

# Add multiple files
earthtool wd add data.wd model1.msh model2.msh texture.tex

# Add files without compression
earthtool wd add data.wd model.msh --no-compress

# Add files and save to new archive
earthtool wd add data.wd model.msh -o data_new.wd
```

**Note:** If a file with the same name already exists in the archive, you will be prompted to confirm replacement.

---

## wd remove

Remove files from a WD archive.

### Usage
```bash
earthtool wd remove <ArchivePath> [options]
```

### Arguments
- `<ArchivePath>` - Path to the archive file

### Options
- `-f, --filter <Pattern>` - Remove files matching pattern
- `-l, --list <Files>` - Comma-separated list of files to remove
- `-o, --output <OutputPath>` - Save to different file (default: overwrites original)

**Note:** Either `--filter` or `--list` must be specified.

### Examples
```bash
# Remove files by pattern
earthtool wd remove data.wd --filter "*.tmp"

# Remove specific files
earthtool wd remove data.wd --list "old_model.msh,backup.tex"

# Remove and save to new archive
earthtool wd remove data.wd --filter "*.bak" -o data_cleaned.wd
```

**Note:** You will be asked to confirm before files are removed.

---

## wd info

Display detailed information about a WD archive.

### Usage
```bash
earthtool wd info <ArchivePath>
```

### Arguments
- `<ArchivePath>` - Path to the WD archive file

### Examples
```bash
# Show detailed archive information
earthtool wd info data.wd
```

### Information Displayed
- File information (path, size, dates)
- Archive header (modification time, flags, GUID, resource type)
- Content statistics (file count, compression stats)
- File type breakdown (top 10 extensions)
- Largest files (top 5)

---

## Tips and Best Practices

### Pattern Matching
Patterns support standard wildcards:
- `*` - Match any characters
- `?` - Match single character
- Examples: `*.msh`, `model?.tex`, `data_*.wd`

### Compression
- Compression is enabled by default for `create` and `add` commands
- Use `--no-compress` if files are already compressed or compression is not needed
- Check compression ratios with `wd list --detailed`

### Safety
- `add` and `remove` commands modify the original archive unless `-o` is specified
- You will be prompted to confirm before:
  - Overwriting existing archives (create)
  - Replacing files with same name (add)
  - Removing files (remove)

### Performance
- Use filters to work with subsets of files
- Recursive archive creation may take time for large directory trees
- Consider compression trade-offs for performance-critical scenarios
