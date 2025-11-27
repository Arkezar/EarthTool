# User Guide - EarthTool WD Archive Manager

## Table of Contents

1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Basic Operations](#basic-operations)
4. [Advanced Features](#advanced-features)
5. [Troubleshooting](#troubleshooting)
6. [FAQ](#faq)

## Introduction

EarthTool WD Archive Manager is a graphical tool for managing WD archives used by the Earth 2150 game. The application enables browsing, extracting, and modifying archive contents.

### What are WD files?

Files with `.WD` extension are packed archives containing Earth 2150 game resources (models, textures, sounds, scripts, etc.). The WD format uses compression to reduce file size.

## Getting Started

### Running the Application

1. Run `EarthTool.WD.GUI.exe`
2. You'll see the main application window with an empty file list
3. Status bar at the bottom shows "Ready"

### User Interface

```
┌──────────────────────────────────────────────────────────────┐
│  File  Archive  Help                           [Menu Bar]    │
├──────────────────────────────────────────────────────────────┤
│  📂 Open  💾 Save │ 📤 Extract  📦 All │ ➕ Add  🗑️ Remove  │
│                                           [Toolbar]           │
├──────────────────────────────────┬───────────────────────────┤
│                                  │  Archive Information      │
│  Archive Contents                │                           │
│  ┌────────────────────────────┐ │  File: [path]            │
│  │ FileName │ Size │ Ratio │  │ │  Modified: [date]        │
│  ├──────────┼──────┼───────┤  │ │  Files: [count]          │
│  │ file1.msh│ 12KB │  45% │  │ │  Size: [total]           │
│  │ file2.tex│ 34KB │  67% │  │ │                           │
│  └────────────────────────────┘ │                           │
│                                  │                           │
│           [Main Area]            │      [Info Panel]         │
├──────────────────────────────────┴───────────────────────────┤
│  Status: Ready                           Items: 0            │
│                                         [Status Bar]          │
└──────────────────────────────────────────────────────────────┘
```

## Basic Operations

### 1. Opening an Archive

**Method 1: Menu**
1. Click `File → Open Archive...`
2. Select a `.WD` file from disk
3. Click "Open"

**Method 2: Keyboard Shortcut**
- Press `Ctrl+O`
- Select file
- Click "Open"

**Method 3: Toolbar**
- Click 📂 "Open" button on toolbar

**What happens:**
- File list will be populated with archive contents
- Info panel on the right shows archive details
- Status bar shows number of loaded files

### 2. Browsing Contents

**File list shows:**
- **File Name** - File name from archive (may include path)
- **Compressed** - Compressed size in archive
- **Decompressed** - Actual size after decompression
- **Ratio** - Compression ratio in percentage
- **Flags** - File flags (Compressed, Named, Text, etc.)

**Info panel shows:**
- Path to opened archive
- Last modification date
- Total number of files
- Total size (compressed and decompressed)
- Overall compression ratio

**Sorting:**
- Click on column header to sort
- Click again to reverse order

### 3. Extracting Files

#### Single File

1. **Select file** in table (click on row)
2. **Choose extraction action:**
   - Menu: `Archive → Extract Selected...`
   - Toolbar: Click 📤 "Extract"
   - Context menu: Right click → "Extract..."
3. **Select destination folder**
4. Click "Select Folder"

**Result:**
- File will be extracted to selected folder
- If file was compressed, it will be automatically decompressed
- Success message appears in status
- File retains its original name

#### All Files

1. **Choose action:**
   - Menu: `Archive → Extract All...`
   - Toolbar: Click 📦 "Extract All"
   - Shortcut: `Ctrl+E`
2. **Select destination folder**
3. Click "Select Folder"

**Result:**
- All files will be extracted
- Directory structure from archive is preserved
- Progress bar shows ongoing operation
- After completion you'll see message with number of extracted files

### 4. Creating a New Archive

1. **Create archive:**
   - Menu: `File → New Archive`
   - Shortcut: `Ctrl+N`

2. **Add files:**
   - Menu: `Archive → Add Files...`
   - Toolbar: Click ➕ "Add"
   - Shortcut: `Ctrl+A`
   - Select one or more files
   - Click "Open"

3. **Save archive:**
   - Menu: `File → Save Archive As...`
   - Shortcut: `Ctrl+Shift+S`
   - Choose name and location
   - Click "Save"

**Tips:**
- New archive is initially empty
- You can add multiple files at once
- Files are automatically compressed when added
- Window title shows asterisk (*) if there are unsaved changes

### 5. Modifying an Existing Archive

#### Adding Files

1. Open existing archive
2. Click `Archive → Add Files...` or `Ctrl+A`
3. Select files to add
4. Click "Open"
5. Save changes: `Ctrl+S`

**Notes:**
- New files will appear in list
- Directory structure is preserved based on file locations
- Duplicate names are allowed (name with full path)

#### Removing Files

1. Select file in table
2. **Choose removal action:**
   - Menu: `Archive → Remove Selected`
   - Toolbar: Click 🗑️ "Remove"
   - Shortcut: `Delete` or `Del`
   - Context menu: Right click → "Remove"
3. Confirm deletion in dialog
4. Save changes: `Ctrl+S`

**Warning:**
- Deletion is permanent after saving archive
- Confirmation dialog always appears
- You can cancel before saving (close without saving)

### 6. Saving Changes

#### Save
- Menu: `File → Save Archive`
- Shortcut: `Ctrl+S`
- Saves to original file
- Available only when there are unsaved changes

#### Save As
- Menu: `File → Save Archive As...`
- Shortcut: `Ctrl+Shift+S`
- Saves to new file
- Original file remains unchanged

### 7. Closing Archive

1. Click `File → Close Archive`
2. If there are unsaved changes, dialog appears:
   - **Yes** - Save and close
   - **No** - Close without saving
   - **Cancel** - Cancel closing

## Advanced Features

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open archive |
| `Ctrl+N` | New archive |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save as... |
| `Ctrl+E` | Extract all |
| `Ctrl+A` | Add files |
| `Delete` / `Del` | Remove selected file |
| `F5` | Refresh (future feature) |

### Context Menu

Right-click on a file in table to open context menu:
- **Extract...** - Extract selected file
- **Remove** - Remove file from archive

### Status Bar

Bottom bar shows:
- **Left:** Status messages (Ready, Loading, Error, Success)
- **Middle:** Progress bar for long operations
- **Right:** Number of files in archive

### Information Panel

Right panel contains:
- **File:** Full path to opened archive
- **Last Modified:** Archive last modification date
- **Files:** Number of files in archive
- **Total Compressed Size:** Total size in archive
- **Total Decompressed Size:** Actual size of all files
- **Overall Compression:** Average compression ratio

## Troubleshooting

### Problem: "Can't open archive"

**Possible causes:**
1. File is not a valid WD archive
2. File is corrupted
3. No read permissions for file

**Solution:**
- Check if file has `.WD` extension
- Try opening another WD file to check if application works
- Check file permissions (right click → Properties)
- See error message in status bar or message box

### Problem: "Extraction fails with error"

**Possible causes:**
1. No write permissions in destination folder
2. Not enough disk space
3. File in archive is corrupted

**Solution:**
- Choose different destination folder (e.g., Desktop)
- Check free disk space
- Try extracting different file
- Check logs in console (if available)

### Problem: "Can't save archive"

**Possible causes:**
1. No write permissions
2. File is open in another program
3. Not enough disk space

**Solution:**
- Use "Save As" to save in different location
- Close other applications that might be using the file
- Check free disk space
- Run application as administrator (if needed)

### Problem: "Application hangs during operation"

**Possible causes:**
1. Very large archive
2. Slow disk
3. Not enough RAM

**Solution:**
- Wait - operations on large archives may take time
- Check progress bar - if it's moving, operation is ongoing
- Close other applications to free memory
- For very large archives consider using CLI version

### Problem: "Unsaved changes were lost"

**Prevention:**
- Always save changes before closing: `Ctrl+S`
- Application warns about unsaved changes before closing
- Window title shows `*` when there are unsaved changes

## FAQ

### Can I open multiple archives simultaneously?

Currently the application supports only one archive at a time. Tab support is planned for future version.

### Can I select multiple files for extraction?

Currently only single selection is supported. However, you can use "Extract All" to extract all files at once. Multi-selection is planned.

### Does the application modify original files?

No, not until you save changes. All modifications are in memory until you click "Save". Using "Save As" allows you to keep original untouched.

### What file formats are supported?

The application supports only WD archive format from Earth 2150 game. Files inside archive can be of any type (MSH, TEX, PAR, etc.).

### Are files automatically compressed?

Yes, when adding files to archive they are automatically compressed using the algorithm used by Earth 2150.

### Can I preview file contents before extraction?

Not currently. Text file preview is planned for future version.

### How can I check if a file is compressed?

The "Flags" column shows "Compressed" flag for compressed files. Additionally, the "Ratio" column shows compression ratio.

### Does the application work on Linux/Mac?

Yes! Avalonia UI supports cross-platform. You only need .NET 8.0 runtime. Build for your platform:

```bash
# Linux
dotnet publish -c Release -r linux-x64

# macOS
dotnet publish -c Release -r osx-x64
```

### Where are logs saved?

Logs are currently written to console (if launched from terminal). Log file support is planned.

### How to report a bug?

Use the Issues system in the EarthTool GitHub repository. Include:
- Problem description
- Steps to reproduce
- Application version
- Operating system
- If possible - sample WD file

### Can I use the application for game modding?

Yes! The application is ideal for:
- Extracting game resources
- Modifying files
- Creating custom WD archives
- Packaging mods

**Warning:** Always create backup of original game files before modification!

### Can I add files from different folders?

Yes, you can add files from any locations. The application preserves relative directory structure based on common parent directory.

### What happens if I add a file with the same name?

WD archive allows duplicate names if files have different paths. If you add a file with identical name and path, both will be in archive (format allows this).

### How can I see details of a single file?

Click on row in table - details are visible in columns. Dedicated details panel is planned for future.

## Support

If you have questions or problems:

1. Check this guide
2. See README.md for technical information
3. See ARCHITECTURE.md for implementation details
4. Report issue on GitHub

## Changelog

### Version 1.0.0
- First public release
- All basic features implemented
- Stable UI and backend integration
- Complete documentation

---

**Happy modding Earth 2150!** 🚀
