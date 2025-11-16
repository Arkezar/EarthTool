# WD Archive Commands

Complete set of commands for managing WD archive files in EarthTool.

## Available Commands

| Command | Description | Usage Example |
|---------|-------------|---------------|
| `list` | Display archive contents | `earthtool wd list data.wd --detailed` |
| `extract` | Extract files from archive(s) | `earthtool wd extract *.wd -o ./output` |
| `create` | Create new archive | `earthtool wd create data.wd -i ./files` |
| `add` | Add files to archive | `earthtool wd add data.wd file.msh` |
| `remove` | Remove files from archive | `earthtool wd remove data.wd --filter "*.tmp"` |
| `info` | Display archive details | `earthtool wd info data.wd` |

## Architecture

### Files Structure
```
Commands/WD/
├── WdSettings.cs         - Settings classes for all WD commands
├── WdCommandBase.cs      - Base class for WD commands
├── ListCommand.cs        - List archive contents
├── ExtractCommand.cs     - Extract files (with filtering)
├── CreateCommand.cs      - Create new archive
├── AddCommand.cs         - Add files to archive
├── RemoveCommand.cs      - Remove files from archive
└── InfoCommand.cs        - Display archive information
```

### Key Features

1. **Multiple Archive Support** - Extract command supports processing multiple archives at once
2. **Filtering Support** - Pattern-based file filtering in list, extract, and remove commands
3. **Selective Operations** - Extract/remove specific files using `--list` option
4. **Compression Control** - Optional compression with `--no-compress` flag
5. **Safety Confirmations** - User prompts before destructive operations
6. **Rich Console Output** - Spectre.Console for beautiful formatted output
7. **Detailed Statistics** - Compression ratios, file sizes, type breakdowns
8. **Batch Processing** - Process multiple archives with a single command

### Dependencies

- `IArchiver` - Core archive manipulation service
- `Spectre.Console` - Rich terminal UI
- Settings classes inherit from `CommandSettings` (Spectre.Console.Cli)

## Full API Utilization

These commands fully utilize the `IArchiver` interface:

- ✅ `OpenArchive(string)` - Used by all commands
- ✅ `CreateArchive()` - Used by create command
- ✅ `AddFile(IArchive, string, bool)` - Used by create/add commands
- ✅ `SaveArchive(IArchive, string)` - Used by create/add/remove commands
- ✅ `Extract(IArchiveItem, string)` - Used by extract command
- ✅ `ExtractAll(IArchive, string)` - Could be used, but selective extract is more flexible
- ✅ `IArchive.Items` - Used by list/info/extract/remove for browsing
- ✅ `IArchive.RemoveItem(IArchiveItem)` - Used by add (replace) and remove commands
- ✅ `IArchive.Header` - Used by info command
- ✅ `IArchive.LastModification` - Used by list/info commands

For detailed usage examples and command documentation, see: `/docs/WD_COMMANDS.md`
