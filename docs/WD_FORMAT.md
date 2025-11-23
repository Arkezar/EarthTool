# WD Archive Format Specification

## Overview

WD (Working Directory) archives are the primary container format used in Earth 2150 for storing game assets. The format uses ZLib compression for both the archive header, file data, and the central directory.

**File Extension:** `.wd`

**Compression:** ZLib (RFC 1950)

**Encoding:** Configurable (typically Windows-1252 or UTF-8)

---

## File Structure

```
┌─────────────────────────────────────────────────────────┐
│ Compressed Archive Header (EarthInfo)                   │
├─────────────────────────────────────────────────────────┤
│ File Data Section                                       │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Item 1 Data (compressed or uncompressed)        │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ Item 2 Data (compressed or uncompressed)        │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ ...                                             │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ Item N Data                                     │   │
│  └─────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────┤
│ Central Directory (compressed with ZLib)                │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Last Modification Time (Int64 FileTime UTC)     │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ Item Count (Int16)                              │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ Item Entries (variable length)                  │   │
│  │  For each item:                                 │   │
│  │    - File Name (String)                         │   │
│  │    - Flags (Byte)                               │   │
│  │    - Data Offset (Int32)                        │   │
│  │    - Compressed Length (Int32)                  │   │
│  │    - Decompressed Size (Int32)                  │   │
│  │    - [Optional] Translation ID (String)         │   │
│  │    - [Optional] Resource Type (Int32)           │   │
│  │    - [Optional] GUID (16 bytes)                 │   │
│  └─────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────┤
│ Descriptor Length (Int32)                               │
│ (Size of compressed central directory + 4 bytes)        │
└─────────────────────────────────────────────────────────┘
```

---

## Detailed Field Descriptions

### 1. Archive Header
- **Type:** Compressed EarthInfo structure
- **Compression:** ZLib
- **Contents:**
  - `Flags` - Archive flags (typically `Compressed | Resource | Guid`)
  - `ResourceType` - Always `ResourceType.WdArchive` (0x2004457)
  - `GUID` - Unique archive identifier

### 2. File Data Section
Individual file data stored sequentially. Each file may be:
- **Compressed** (if `FileFlags.Compressed` is set in item header)
- **Uncompressed** (raw data)

Position and length of each file's data is stored in the Central Directory.

### 3. Central Directory (Compressed)

#### 3.1 Last Modification Time
- **Type:** `Int64`
- **Format:** Windows FileTime (UTC)
- **Description:** Timestamp of last archive modification

#### 3.2 Item Count
- **Type:** `Int16`
- **Range:** 0-32767
- **Description:** Number of files in the archive

#### 3.3 Item Entries

For each file in the archive:

| Field | Type | Description |
|-------|------|-------------|
| **File Name** | String | Relative path within archive (e.g., "textures/unit.tex") |
| **Flags** | Byte | FileFlags enum value |
| **Data Offset** | Int32 | Byte offset from start of file to item data |
| **Compressed Length** | Int32 | Size of data in archive (compressed or raw) |
| **Decompressed Size** | Int32 | Original uncompressed size |
| **Translation ID** | String | *(Optional)* Present if `FileFlags.Named` is set |
| **Resource Type** | Int32 | *(Optional)* Present if `FileFlags.Resource` is set |
| **GUID** | 16 bytes | *(Optional)* Present if `FileFlags.Guid` is set |

### 4. Descriptor Length
- **Type:** `Int32`
- **Location:** Last 4 bytes of file
- **Value:** Size of compressed central directory + 4
- **Purpose:** Allows reading archive from end backwards

---

## FileFlags Enumeration

```csharp
[Flags]
public enum FileFlags : byte
{
    None       = 0x00,
    Named      = 0x01,  // Item has TranslationID
    Resource   = 0x02,  // Item has ResourceType
    Guid       = 0x04,  // Item has GUID
    Compressed = 0x08   // Item data is ZLib compressed
}
```

Common combinations:
- `None` (0x00) - Simple file with no metadata
- `Compressed` (0x08) - Compressed file data
- `Resource | Guid | Compressed` (0x0E) - Full metadata with compression

---

## Reading Algorithm

```
1. Open archive file
2. Read and decompress archive header (EarthInfo)
3. Verify ResourceType == WdArchive
4. Seek to end of file - 4 bytes
5. Read descriptor length (Int32)
6. Seek to position: fileSize - descriptorLength
7. Read and decompress central directory
8. Parse central directory:
   - Read LastModification (Int64)
   - Read ItemCount (Int16)
   - For each item:
     - Read metadata based on flags
     - Store offset and length for lazy loading
9. Access individual files using stored offsets
```

---

## Writing Algorithm

```
1. Create archive header (EarthInfo)
2. Compress and write header to stream
3. For each file to add:
   - Record current stream position (offset)
   - Optionally compress file data
   - Write data to stream
   - Record length written
   - Store metadata (filename, flags, offset, length, etc.)
4. Build central directory:
   - Write LastModification timestamp
   - Write item count
   - For each item, write metadata
5. Compress central directory with ZLib
6. Write compressed central directory to stream
7. Calculate descriptor length (compressed size + 4)
8. Write descriptor length (Int32)
```

---

## Example: Reading a File from Archive

```csharp
// 1. Open archive
var archive = archiveFactory.OpenArchive("game.wd");

// 2. Find file
var item = archive.Items.FirstOrDefault(i => i.FileName == "units/ed_unit.msh");

// 3. Extract data
if (item.IsCompressed)
{
    var compressed = item.Data;
    var decompressed = decompressor.Decompress(compressed);
    // Use decompressed data
}
else
{
    var data = item.Data;
    // Use data directly
}
```

---

## Example: Creating a New Archive

```csharp
// 1. Create new archive
var archive = archiveFactory.NewArchive();

// 2. Add files
var item1 = ArchiveItem.FromFile(
    "unit.msh", 
    earthInfoFactory, 
    compressor, 
    compress: true);
archive.AddItem(item1);

var item2 = ArchiveItem.FromFile(
    "texture.tex", 
    earthInfoFactory, 
    compressor, 
    compress: true);
archive.AddItem(item2);

// 3. Save archive
var archiveData = archive.ToByteArray(compressor, encoding);
File.WriteAllBytes("new_archive.wd", archiveData);
```

---

## Performance Considerations

### Current Implementation
- **Memory:** Entire archive loaded into memory (`File.ReadAllBytes`)
- **Suitable for:** Archives < 100 MB
- **Limitation:** Large archives may cause OutOfMemoryException

### Recommended Optimization (Future)
Use `MemoryMappedFile` for large archives:
- OS manages memory paging
- Lazy loading of file data
- Supports archives larger than available RAM
- Better performance for random access

---

## Compression Details

### ZLib Configuration
- **Library:** `System.IO.Compression.ZLibStream`
- **Mode:** `CompressionMode.Compress` / `CompressionMode.Decompress`
- **Level:** Default (implementation-specific)

### What is Compressed?
1. ✅ Archive header (EarthInfo)
2. ✅ Central directory (metadata)
3. ⚠️ Individual file data (optional, per-file flag)

---

## Common Issues and Solutions

### Issue: "Unsupported archive type"
- **Cause:** Archive header ResourceType is not `WdArchive`
- **Solution:** Verify file is valid WD archive, check for corruption

### Issue: OutOfMemoryException
- **Cause:** Archive too large for current implementation
- **Solution:** Implement Memory-Mapped Files or process files individually

### Issue: Decompression fails
- **Cause:** Corrupted compressed data or wrong compression format
- **Solution:** Verify ZLib format (RFC 1950), check file integrity

### Issue: Invalid file paths in archive
- **Cause:** Path traversal attempt (e.g., `../../system32/file.exe`)
- **Solution:** Use `PathValidator.SanitizeFileName()` to clean paths

---

## File Format Version History

| Version | Changes | Notes |
|---------|---------|-------|
| 1.0 | Initial format | Used in Earth 2150 release |
| - | No known updates | Format remained stable |

---

## Related Formats

- **EarthInfo** - Header format used by all Earth 2150 resources
- **MSH** - 3D mesh format (often stored in WD archives)
- **TEX** - Texture format (often stored in WD archives)
- **PAR** - Parameters format (game configuration)

---

## Implementation Files

| File | Purpose |
|------|---------|
| `ArchiveFactory.cs` | Opening and creating archives |
| `Archive.cs` | Archive container model |
| `ArchiveItem.cs` | Individual file within archive |
| `ArchiverService.cs` | High-level archive operations |
| `CompressorService.cs` | ZLib compression |
| `DecompressorService.cs` | ZLib decompression |

---

## References

- Earth 2150 Game Files
- ZLib Specification: [RFC 1950](https://www.rfc-editor.org/rfc/rfc1950)
- .NET Documentation: `System.IO.Compression`

---

**Last Updated:** 2025-11-15

**Implementation:** EarthTool.WD v1.0
