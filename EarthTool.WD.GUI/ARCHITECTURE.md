# EarthTool.WD.GUI - Architecture

## Overview

EarthTool.WD.GUI is a desktop application built on the MVVM (Model-View-ViewModel) pattern using Avalonia UI and ReactiveUI.

## Architectural Patterns

### MVVM (Model-View-ViewModel)

```
┌─────────────┐         ┌──────────────────┐         ┌──────────────┐
│    View     │◄───────►│   ViewModel      │◄───────►│    Model     │
│   (XAML)    │ Binding │ (Business Logic) │  Uses   │  (Backend)   │
└─────────────┘         └──────────────────┘         └──────────────┘
     │                           │                           │
     │                           │                           │
     ▼                           ▼                           ▼
MainWindow.axaml      MainWindowViewModel.cs         IArchiver
                                                      IArchive
                                                      IArchiveItem
```

#### Separation of Concerns

**View (Views/)**
- UI only in XAML
- Minimal code-behind (initialization only)
- Data binding to ViewModel
- Contains no business logic

**ViewModel (ViewModels/)**
- Presentation and business logic
- Commands for user actions
- Observable properties for data binding
- Interaction with Services
- No knowledge of specific Views

**Model (EarthTool.WD/)**
- Domain logic (archive management)
- Data access (WD files)
- UI independent

### Dependency Injection

The application uses **Microsoft.Extensions.DependencyInjection** for dependency management.

#### Configuration (App.axaml.cs)

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Logging
    services.AddLogging();
    
    // Backend services
    services.AddCommonServices();  // Encoding, EarthInfoFactory
    services.AddWdServices();       // ArchiverService, ArchiveFactory, etc.
    
    // UI services
    services.AddSingleton<IDialogService, DialogService>();
    services.AddSingleton<INotificationService, NotificationService>();
    
    // ViewModels
    services.AddTransient<MainWindowViewModel>();
}
```

#### Service Lifetimes

| Service | Lifetime | Rationale |
|---------|----------|-----------|
| IDialogService | Singleton | Shared access to dialog windows |
| INotificationService | Singleton | Central notification handling |
| MainWindowViewModel | Transient | New instance for each window |
| IArchiver | Scoped | Per archive operation |
| IArchiveFactory | Scoped | Per creation operation |

### Reactive Extensions (ReactiveUI)

#### ReactiveCommand

All user actions are implemented as `ReactiveCommand`:

```csharp
// Definition
public ReactiveCommand<Unit, Unit> OpenArchiveCommand { get; private set; }

// Initialization with CanExecute
var canOpen = this.WhenAnyValue(x => x.IsReady);
OpenArchiveCommand = ReactiveCommand.CreateFromTask(OpenArchiveAsync, canOpen);

// Implementation
private async Task OpenArchiveAsync()
{
    // Archive opening logic
}
```

#### Observable Properties

Properties notify changes automatically:

```csharp
private bool _isBusy;
public bool IsBusy
{
    get => _isBusy;
    set => this.RaiseAndSetIfChanged(ref _isBusy, value);
}
```

#### Computed Properties

```csharp
this.WhenAnyValue(
    x => x.CompressedSize,
    x => x.DecompressedSize,
    (compressed, decompressed) => CalculateRatio(compressed, decompressed))
.ToProperty(this, x => x.CompressionRatio, out _compressionRatio);
```

## Data Flow

### Opening an Archive

```
User Click → Command → ViewModel → Service → Backend → Update Properties → UI Update
```

In detail:

1. User clicks "Open Archive"
2. `OpenArchiveCommand.Execute()` is invoked
3. `MainWindowViewModel.OpenArchiveAsync()` executes:
   - Calls `IDialogService.ShowOpenFileDialogAsync()`
   - Receives file path
   - Calls `IArchiver.OpenArchive(filePath)`
   - Receives `IArchive` from backend
   - Converts `IArchiveItem[]` to `ArchiveItemViewModel[]`
   - Updates `ArchiveItems` ObservableCollection
   - Updates `ArchiveInfo`
4. Data binding automatically updates UI (DataGrid, Info Panel)
5. StatusBar shows success message

### Extracting a File

```
User Selection → Command → ViewModel → Dialog → Backend → Notification → Status Update
```

1. User selects file in DataGrid
2. `SelectedItem` property is updated (two-way binding)
3. `ExtractSelectedCommand.CanExecute` changes to `true`
4. User clicks "Extract"
5. `ExtractSelectedAsync()` executes:
   - Calls `IDialogService.ShowFolderBrowserDialogAsync()`
   - Receives destination folder
   - Sets `IsBusy = true` (UI shows progress)
   - Calls `IArchiver.Extract(item, folder)` in `Task.Run`
   - After completion calls `INotificationService.ShowSuccess()`
   - Updates `StatusMessage`
   - Sets `IsBusy = false`

## Components

### ViewModels

#### MainWindowViewModel

**Responsibilities:**
- Orchestration of main application operations
- Archive state management
- User command handling
- Coordination with Services

**Key properties:**
- `ArchiveItems` - List of files in archive
- `SelectedItem` - Currently selected file
- `ArchiveInfo` - Archive metadata
- `IsBusy` - Whether operation is in progress
- `StatusMessage` - Message for user
- `HasUnsavedChanges` - Whether there are unsaved changes

**Key commands:**
- `OpenArchiveCommand` - Open archive
- `SaveArchiveCommand` - Save changes
- `ExtractSelectedCommand` - Extract file
- `ExtractAllCommand` - Extract everything
- `AddFilesCommand` - Add files
- `RemoveSelectedCommand` - Remove file

#### ArchiveItemViewModel

**Responsibilities:**
- Wrapper for `IArchiveItem`
- Data formatting for UI
- Computed properties (ratio, formatted sizes)

**Key properties:**
- `Item` - Underlying IArchiveItem
- `FileName`, `CompressedSize`, `DecompressedSize`
- `CompressionRatio` - Calculated ratio
- `FormattedCompressedSize` - Formatted size
- `Flags` - FileFlags enum

#### ArchiveInfoViewModel

**Responsibilities:**
- Aggregation of archive information
- Metadata formatting
- Statistics calculation

**Key properties:**
- `FilePath`, `LastModification`, `ItemCount`
- `TotalCompressedSize`, `TotalDecompressedSize`
- `FormattedCompressionRatio` - Overall ratio

**Key methods:**
- `UpdateFromArchive(IArchive)` - Update from archive
- `Clear()` - Clear data

### Services

#### IDialogService

**Responsibilities:**
- Abstraction over system dialogs
- File pickers (Open, Save, Folder)
- Message boxes

**API:**
```csharp
Task<string?> ShowOpenFileDialogAsync();
Task<string?> ShowSaveFileDialogAsync(string? defaultFileName);
Task<string?> ShowFolderBrowserDialogAsync();
Task<IReadOnlyList<string>> ShowOpenFilesDialogAsync();
Task<MessageBoxResult> ShowMessageBoxAsync(string message, string title, MessageBoxType type);
```

**Implementation:**
- Uses `StorageProvider` API from Avalonia 11.x
- File filters for .WD files
- Centering relative to main window

#### INotificationService

**Responsibilities:**
- Central notification handling
- Error logging
- Event-driven notifications

**API:**
```csharp
void ShowError(string message, Exception? exception);
void ShowWarning(string message);
void ShowSuccess(string message);
void ShowInfo(string message);
event EventHandler<NotificationEventArgs> NotificationRaised;
```

**Implementation:**
- Integration with `ILogger<T>`
- Event emission for ViewModel subscriptions
- Different logging levels

### Converters

#### BytesToHumanReadableConverter

Converts byte numbers to human-readable format (B, KB, MB, GB).

```csharp
1024 → "1.00 KB"
1048576 → "1.00 MB"
```

#### FileFlagsToStringConverter

Converts `FileFlags` enum to string with flag descriptions.

```csharp
FileFlags.Compressed | FileFlags.Named → "Compressed, Named"
```

#### BoolToVisibilityConverter

Converts bool to visibility (true/false binding).

```csharp
true → Visible
false → Collapsed
```

## Error Handling Strategy

### Three Levels of Handling

1. **Try-Catch in ViewModel methods**
   ```csharp
   try
   {
       await Operation();
   }
   catch (Exception ex)
   {
       _notificationService.ShowError("Error", ex);
       _logger.LogError(ex, "Operation failed");
   }
   ```

2. **NotificationService aggregation**
   - All errors through one service
   - Consistent logging
   - Central message formatting

3. **UI Feedback**
   - StatusMessage in StatusBar
   - MessageBox for critical errors
   - IsBusy indicator during operations

### Try-Finally Pattern for IsBusy

```csharp
try
{
    IsBusy = true;
    StatusMessage = "Processing...";
    await LongOperation();
    StatusMessage = "Done";
}
catch (Exception ex)
{
    _notificationService.ShowError("Failed", ex);
    StatusMessage = "Error";
}
finally
{
    IsBusy = false;  // ALWAYS reset
}
```

## Data Binding Patterns

### One-Way Binding (ViewModel → View)

```xml
<TextBlock Text="{Binding StatusMessage}"/>
<DataGrid ItemsSource="{Binding ArchiveItems}"/>
```

### Two-Way Binding (View ↔ ViewModel)

```xml
<DataGrid SelectedItem="{Binding SelectedItem}"/>
```

### Command Binding

```xml
<Button Command="{Binding OpenArchiveCommand}"/>
<MenuItem Command="{Binding SaveArchiveCommand}" HotKey="Ctrl+S"/>
```

### Value Converter Binding

```xml
<TextBlock Text="{Binding CompressedSize, Converter={StaticResource BytesToHumanReadable}}"/>
```

## Performance Considerations

### Async Operations

All I/O operations are asynchronous:

```csharp
await Task.Run(() => _archiver.OpenArchive(filePath));
```

Benefits:
- UI remains responsive
- No main thread blocking
- Progress indicators can be displayed

### ObservableCollection Updates

List updates are batched where possible:

```csharp
ArchiveItems.Clear();
foreach (var item in archive.Items)
{
    ArchiveItems.Add(new ArchiveItemViewModel(item));
}
```

### Memory Management

- `IArchive` implements `IDisposable` (memory-mapped files)
- `MainWindowViewModel` Dispose pattern:
  ```csharp
  public void Dispose()
  {
      _currentArchive?.Dispose();
      _notificationService.NotificationRaised -= OnNotificationRaised;
  }
  ```

## Testing Strategy

### ViewModels

Unit tests with mocked services:

```csharp
var mockArchiver = new Mock<IArchiver>();
var mockDialog = new Mock<IDialogService>();
var mockNotification = new Mock<INotificationService>();
var mockLogger = new Mock<ILogger<MainWindowViewModel>>();

var viewModel = new MainWindowViewModel(
    mockArchiver.Object,
    mockDialog.Object,
    mockNotification.Object,
    mockLogger.Object
);

// Test command execution
await viewModel.OpenArchiveCommand.Execute();
```

### Integration Tests

End-to-end tests with real services and test archives.

### Manual Testing

Test scenarios described in README.md.

## Deployment

### Single File Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Cross-Platform

Avalonia supports:
- Windows (win-x64, win-arm64)
- Linux (linux-x64, linux-arm, linux-arm64)
- macOS (osx-x64, osx-arm64)

## Future Architecture Improvements

### Planned Enhancements

1. **Multi-selection support** - Refactoring SelectedItem → SelectedItems collection
2. **Background operations** - Queue for long operations
3. **Plugin system** - Extensibility for custom file handlers
4. **Undo/Redo** - Command pattern with history
5. **Async initialization** - Lazy loading for ViewModels

### Technical Debt

- [ ] Add unit tests for ViewModels
- [ ] Add integration tests
- [ ] Refactor DialogService to use behaviors
- [ ] Implement Attached Properties for SelectedItems binding
- [ ] Progress reporting for long operations

## Class Diagram

```
┌────────────────────────────────────────────────────────────────┐
│                      MainWindowViewModel                        │
├────────────────────────────────────────────────────────────────┤
│ - _archiver: IArchiver                                         │
│ - _dialogService: IDialogService                               │
│ - _notificationService: INotificationService                   │
│ - _logger: ILogger                                             │
│ + ArchiveItems: ObservableCollection<ArchiveItemViewModel>    │
│ + SelectedItem: ArchiveItemViewModel?                          │
│ + ArchiveInfo: ArchiveInfoViewModel                            │
│ + IsBusy: bool                                                 │
│ + StatusMessage: string                                        │
├────────────────────────────────────────────────────────────────┤
│ + OpenArchiveCommand: ReactiveCommand                          │
│ + SaveArchiveCommand: ReactiveCommand                          │
│ + ExtractSelectedCommand: ReactiveCommand                      │
│ + AddFilesCommand: ReactiveCommand                             │
└────────────────────────────────────────────────────────────────┘
           │                           │
           │ uses                      │ uses
           ▼                           ▼
┌──────────────────────┐    ┌────────────────────────┐
│   IDialogService     │    │ INotificationService   │
├──────────────────────┤    ├────────────────────────┤
│ + ShowOpenFile()     │    │ + ShowError()          │
│ + ShowSaveFile()     │    │ + ShowSuccess()        │
│ + ShowFolder()       │    │ + ShowWarning()        │
│ + ShowMessageBox()   │    │ + ShowInfo()           │
└──────────────────────┘    └────────────────────────┘
```

## Conclusion

The EarthTool.WD.GUI architecture is designed with focus on:

- **Separation of concerns** - MVVM pattern
- **Testability** - Dependency Injection
- **Maintainability** - Clear folder structure and Single Responsibility classes
- **Extensibility** - Service abstractions and plugin-ready architecture
- **User Experience** - Reactive UI with real-time feedback
