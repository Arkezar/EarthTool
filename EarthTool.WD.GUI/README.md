# EarthTool.WD.GUI - WD Archive Manager

Graphical user interface for managing WD archives from the Earth 2150 game.

## Overview

EarthTool.WD.GUI is a desktop application built with Avalonia UI that enables easy work with WD archive files used by the Earth 2150 game. The application offers an intuitive interface for browsing, extracting, and modifying archive contents.

## Features

### Basic Operations

- **Open Archives** - Load existing .WD files
- **Create New Archives** - Generate empty WD archives
- **Save Archives** - Save changes to file (Save/Save As)
- **Close Archives** - Safe closing with warning about unsaved changes

### File Management

- **Browse Contents** - List all files in the archive with detailed information
- **Extract Single File** - Extract selected file to a folder
- **Extract All Files** - Extract entire archive contents
- **Add Files** - Insert new files into the archive with compression
- **Remove Files** - Delete selected files from the archive

### Archive Information

- File name and path
- Last modification date
- Number of files
- Total size (compressed and uncompressed)
- Compression ratio
- Archive header information

### File Details

For each file in the archive, the following is displayed:
- File name
- Compressed size
- Uncompressed size
- Compression ratio
- File flags (Compressed, Archive, Text, Named, Resource, Guid)

## Architecture

### Design Patterns

- **MVVM (Model-View-ViewModel)** - Separation of business logic from user interface
- **Dependency Injection** - Using Microsoft.Extensions.DependencyInjection
- **Command Pattern** - ReactiveCommand with ReactiveUI for user actions
- **Observer Pattern** - ReactiveUI for property change notifications

### Project Structure

```
EarthTool.WD.GUI/
├── ViewModels/              # Business logic
│   ├── MainWindowViewModel.cs        # Main ViewModel
│   ├── ArchiveItemViewModel.cs       # Wrapper for IArchiveItem
│   ├── ArchiveInfoViewModel.cs       # Archive information
│   ├── AboutViewModel.cs             # "About" dialog
│   └── ViewModelBase.cs              # Base class for ViewModels
│
├── Views/                   # User interface (XAML)
│   ├── MainWindow.axaml              # Main application window
│   └── MainWindow.axaml.cs           # Code-behind
│
├── Services/                # UI Services
│   ├── IDialogService.cs             # Dialog interface
│   ├── DialogService.cs              # Dialog implementation
│   ├── INotificationService.cs       # Notification interface
│   └── NotificationService.cs        # Notification implementation
│
├── Converters/              # Value Converters for data binding
│   ├── BytesToHumanReadableConverter.cs
│   ├── FileFlagsToStringConverter.cs
│   └── BoolToVisibilityConverter.cs
│
├── App.axaml.cs             # Application configuration and DI
└── Program.cs               # Entry point
```

### Dependencies

- **Avalonia 11.3.9** - Cross-platform UI framework
- **ReactiveUI.Avalonia 11.3.8** - MVVM framework with Reactive Extensions
- **Microsoft.Extensions.DependencyInjection 8.0.0** - Dependency Injection
- **Microsoft.Extensions.Logging 8.0.0** - Logging abstractions
- **EarthTool.WD** - Backend for WD archive operations
- **EarthTool.Common** - Common interfaces and utilities

## Usage

### Running the Application

```bash
cd EarthTool.WD.GUI
dotnet run
```

### Opening an Archive

1. Click **File → Open Archive** or use **Ctrl+O**
2. Select a .WD file from disk
3. Archive contents will be displayed in the main table

### Extracting Files

**Single file:**
1. Select a file in the table
2. Click **Archive → Extract Selected** or the **Extract** button on the toolbar
3. Select destination folder

**All files:**
1. Click **Archive → Extract All** or **Ctrl+E**
2. Select destination folder

### Creating a New Archive

1. Click **File → New Archive** or **Ctrl+N**
2. Add files using **Archive → Add Files** or **Ctrl+A**
3. Save the archive using **File → Save Archive As**

### Modifying an Archive

1. Open an existing archive
2. Add new files: **Archive → Add Files**
3. Remove files: Select file → **Archive → Remove Selected** or **Del**
4. Save changes: **File → Save Archive** or **Ctrl+S**

## Keyboard Shortcuts

| Shortcut | Action |
|-------|-------|
| `Ctrl+O` | Open archive |
| `Ctrl+N` | New archive |
| `Ctrl+S` | Save archive |
| `Ctrl+Shift+S` | Save as... |
| `Ctrl+E` | Extract all |
| `Ctrl+A` | Add files |
| `Del` | Remove selected file |

## Dependency Injection

The application uses Microsoft.Extensions.DependencyInjection for dependency management.

### Service Configuration (App.axaml.cs)

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Logging
    services.AddLogging(builder => 
        builder.SetMinimumLevel(LogLevel.Information));

    // Common services (Encoding, EarthInfoFactory)
    services.AddCommonServices();

    // WD services (ArchiverService, ArchiveFactory, Compressor, Decompressor)
    services.AddWdServices();

    // GUI services
    services.AddSingleton<IDialogService, DialogService>();
    services.AddSingleton<INotificationService, NotificationService>();

    // ViewModels
    services.AddTransient<MainWindowViewModel>();
}
```

### Lifetimes

- **Singleton** - DialogService, NotificationService (shared across the entire application)
- **Transient** - ViewModels (new instance for each window)
- **Scoped** - Services from EarthTool.WD (per operation)

## Error Handling

The application implements comprehensive error handling:

### Central Handling in ViewModels

All asynchronous operations are wrapped in try-catch blocks:

```csharp
try
{
    IsBusy = true;
    StatusMessage = "Operation in progress...";
    await PerformOperationAsync();
    _notificationService.ShowSuccess("Operation completed");
}
catch (Exception ex)
{
    _notificationService.ShowError("Operation failed", ex);
    StatusMessage = "Error";
}
finally
{
    IsBusy = false;
}
```

### NotificationService

All errors are logged and displayed to the user through `INotificationService`:

- **ShowError** - Displays error and logs exception
- **ShowWarning** - Warning for the user
- **ShowSuccess** - Confirmation of successful operation
- **ShowInfo** - General information

### StatusBar

Current operation status is always displayed in the bottom bar:
- Operation progress messages
- Error information
- Completion confirmations
- Progress bar for long-running operations

## Extending Functionality

### Adding a New Command

1. Define property in MainWindowViewModel:
```csharp
public ReactiveCommand<Unit, Unit> MyCommand { get; private set; } = null!;
```

2. Initialize in `InitializeCommands()`:
```csharp
var canExecute = this.WhenAnyValue(x => x.SomeCondition);
MyCommand = ReactiveCommand.CreateFromTask(MyMethodAsync, canExecute);
```

3. Implement method:
```csharp
private async Task MyMethodAsync()
{
    try
    {
        // Logic
    }
    catch (Exception ex)
    {
        _notificationService.ShowError("Error", ex);
    }
}
```

4. Connect in XAML:
```xml
<MenuItem Header="My Action" Command="{Binding MyCommand}"/>
```

### Adding a New Value Converter

1. Create a class implementing `IValueConverter`:
```csharp
public class MyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, 
        object? parameter, CultureInfo culture)
    {
        // Conversion
    }
}
```

2. Add to Resources in XAML:
```xml
<Window.Resources>
    <converters:MyConverter x:Key="MyConverter"/>
</Window.Resources>
```

3. Use in binding:
```xml
<TextBlock Text="{Binding Value, Converter={StaticResource MyConverter}}"/>
```

## Testing

### Manual Test Scenarios

1. **Archive Opening Test**
   - Open a valid .WD file
   - Check if the file list is displayed
   - Check archive information in the right panel

2. **Extraction Test**
   - Select a file
   - Extract to folder
   - Verify that the file was created

3. **Archive Creation Test**
   - Create a new archive
   - Add several files
   - Save as a new .WD file
   - Open the saved file and verify contents

4. **Modification Test**
   - Open an archive
   - Add new files
   - Remove some files
   - Save and reopen

5. **Error Handling Test**
   - Try to open an invalid file
   - Try to save to a protected folder
   - Verify error messages

## Known Limitations

- **Single Selection** - Currently only single file selection is supported (multiple selection planned for future version)
- **No Progress for Long Operations** - Large archive extraction shows only indeterminate indicator (detailed progress planned)
- **No File Preview** - Text file content preview planned for future version

## Roadmap

### Future Features

- [ ] Multi-selection in DataGrid
- [ ] Detailed progress bar for long operations
- [ ] Text file preview
- [ ] Drag & Drop for adding files
- [ ] Recently opened files history
- [ ] File search in archive
- [ ] File list sorting and filtering
- [ ] Export file list to CSV
- [ ] Compare two archives
- [ ] Batch operations (operations on multiple archives)

### UI Improvements

- [ ] Icons instead of emoji in menu
- [ ] Themes (Light/Dark mode)
- [ ] Configurable colors
- [ ] Tabs for multiple open archives
- [ ] Statusbar with additional information

## License

Part of the EarthTool project. See the main LICENSE file in the root directory of the repository.

## Contact and Support

To report bugs or suggest new features, use the Issues system in the GitHub repository.

## Acknowledgments

- Earth 2150 community for WD format documentation
- Avalonia UI team for the excellent framework
- ReactiveUI authors for the MVVM framework
