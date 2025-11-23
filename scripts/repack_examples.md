# EarthTool WD Archive Repack Script - Examples

## Basic Usage

### Repack with default settings (no compression)
```bash
./scripts/repack.sh "dotnet run --project EarthTool.CLI/EarthTool.CLI.csproj -c Release --" ./WDFiles_Original
```

### Repack with compression
```bash
./scripts/repack.sh "dotnet run --project EarthTool.CLI/EarthTool.CLI.csproj -c Release --" ./WDFiles_Original --compress
```

## Timestamp Preservation

### Preserve original timestamps (useful for tests)
```bash
./scripts/repack.sh "dotnet run..." ./WDFiles_Original --compress --preserve-timestamps
```

This will preserve BOTH:
1. **Internal archive timestamp** (`Archive.LastModification`) - used by game for load order
2. **File system timestamp** - for sorting in file browser

**Why this matters:**
- The game loads WD archives based on `Archive.LastModification` 
- Preserving timestamps ensures correct load order in game
- Essential for regression testing and maintaining game compatibility

### With compression and preserved timestamps
```bash
./scripts/repack.sh "dotnet run..." ./WDFiles_Original --compress --preserve-timestamps
```

## CLI: Creating Archive with Custom Timestamp

You can also set custom timestamps directly using the CLI:

### Using datetime format
```bash
dotnet run --project EarthTool.CLI/EarthTool.CLI.csproj -c Release -- \
  wd create my_archive.wd -i ./data -r --timestamp "2000-08-20 12:00:00"
```

### Using unix epoch
```bash
dotnet run --project EarthTool.CLI/EarthTool.CLI.csproj -c Release -- \
  wd create my_archive.wd -i ./data -r --timestamp "966859200"
```

## Examples

**Default behavior:**
- Uses current time for Archive.LastModification
- Fast execution (~2s per archive)
```
test1.wd: Archive.LastModification = 2025-11-18 19:00:00
test2.wd: Archive.LastModification = 2025-11-18 19:00:02
```

**With --preserve-timestamps:**
- Maintains exact original Archive.LastModification
- Maintains file system timestamp
- Useful for regression tests and maintaining game load order
```
Original test1.wd: 
  - Archive.LastModification = 2000-08-20 12:00:00
  - File timestamp = 2000-08-20 12:00:00

Repacked test1.wd:
  - Archive.LastModification = 2000-08-20 12:00:00 ✓
  - File timestamp = 2000-08-20 12:00:00 ✓
```

## Testing

### Verify archive internal timestamp
```bash
dotnet run --project EarthTool.CLI/EarthTool.CLI.csproj -c Release -- wd list archive.wd
```

Look for "Last modified" field in the output - this is `Archive.LastModification`.
