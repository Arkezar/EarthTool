# Debugging Guide - EarthTool.WD.GUI

## Problem: UI doesn't refresh after opening archive

### Cause
The problem resulted from incorrect `ObservableCollection` updates from a background thread.

### Solution
Added `RxApp.MainThreadScheduler` to force collection operations on the UI thread:

```csharp
private void LoadArchiveItems()
{
  // Ensure we're on the UI thread
  RxApp.MainThreadScheduler.Schedule(() =>
  {
    ArchiveItems.Clear();
    SelectedItem = null;
    // ... rest of the operations
  });
}
```

### Additional Fixes
1. **Moving `_currentArchive` assignment outside `Task.Run`**
   - I/O operations in background thread
   - Field assignment and notification in UI thread

2. **Explicit `RaisePropertyChanged(nameof(IsArchiveOpen))`**
   - Called right after `_currentArchive` changes
   - Ensures info panel visibility update

### Verification
To verify the problem is resolved:

1. Run application with Debug logging:
```bash
export DOTNET_ENVIRONMENT=Development
dotnet run --verbosity detailed
```

2. Open a WD archive

3. Check if console shows logs:
```
LoadArchiveItems called. Archive is null: False
Archive has X items
Added X items to ArchiveItems collection
```

4. Check UI:
   - DataGrid should show file list
   - "Archive Information" panel should be visible
   - All fields (File, Last Modified, Files, etc.) should have values

### Common Issues

#### Problem: "Cross-thread operation exception"
**Symptoms**: Exception when adding to `ObservableCollection`

**Solution**: Ensure collection operations are in `RxApp.MainThreadScheduler.Schedule()`

#### Problem: Info panel doesn't show
**Symptoms**: File list displays but right panel is empty

**Solution**: Check if `IsArchiveOpen` property is properly notified:
```csharp
this.RaisePropertyChanged(nameof(IsArchiveOpen));
```

#### Problem: Panel data is empty
**Symptoms**: Panel is visible but all fields show "N/A"

**Solution**: Check if `ArchiveInfo.UpdateFromArchive()` is called with non-null archive

### Debug Logging

Added debug logs in `LoadArchiveItems()`:
- Archive state (null/not null)
- Number of items in archive
- Number of items added to collection

To see them, set logging level to Debug in App.axaml.cs:
```csharp
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug); // Change from Information to Debug
});
```

### Performance Considerations

`RxApp.MainThreadScheduler.Schedule()` adds small delay, but:
- Ensures thread-safety
- Avoids cross-thread exceptions
- Standard practice in ReactiveUI

For very large archives (1000+ files) consider:
1. Virtualization in DataGrid
2. Batch loading with progress
3. Async initialization of ViewModels

### Test Cases

Test the following scenarios:

1. **Open small archive** (< 10 files)
   - All files should appear immediately
   - Info panel should have correct data

2. **Open large archive** (100+ files)
   - Brief delay may occur
   - IsBusy indicator should appear

3. **Open corrupted archive**
   - Error message should appear
   - UI should remain responsive

4. **Create new archive**
   - File list should be empty
   - Info panel should show "0 files"

5. **Add files to archive**
   - New files should appear in list
   - Counters in info panel should update

### Related Issues

- ReactiveUI threading: https://www.reactiveui.net/docs/handbook/scheduling/
- Avalonia threading: https://docs.avaloniaui.net/docs/concepts/services/dispatcher
