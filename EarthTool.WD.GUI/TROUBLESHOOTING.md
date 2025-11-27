# Troubleshooting - DataGrid and Information Panel Empty After Opening Archive

## Problem
Status bar and window title show correct information, but:
- DataGrid (Archive Contents) is empty
- Information panel is empty or invisible

## Step-by-Step Diagnostics

### 1. Check if data is loading

Run application with logging:
```bash
cd /home/arkezar/Source/EarthTool/EarthTool.WD.GUI
dotnet run --verbosity detailed > app.log 2>&1
```

Open a WD archive, then check `app.log`:

**Look for these lines:**
```
MainWindowViewModel constructed
LoadArchiveItems called. Archive is null: False
Archive has X items
Added X items to ArchiveItems collection
```

**If you see these logs** → data IS loading, problem is in UI binding  
**If you DON'T see these logs** → problem is in data loading

### 2. Check DataContext

Add to MainWindow.axaml.cs in constructor:
```csharp
public MainWindow()
{
    InitializeComponent();
    this.Loaded += (s, e) =>
    {
        Console.WriteLine($"DataContext type: {DataContext?.GetType().Name}");
        if (DataContext is MainWindowViewModel vm)
        {
            Console.WriteLine($"ArchiveItems count: {vm.ArchiveItems.Count}");
        }
    };
}
```

Run application - you should see:
```
DataContext type: MainWindowViewModel
ArchiveItems count: 0
```

### 3. Test binding with test data

Temporarily add test data in MainWindowViewModel constructor:
```csharp
// DEBUG: Add test data
ArchiveItems.Add(new ArchiveItemViewModel(
    new TestArchiveItem("test.txt", 100, 200)));
```

If you see "test.txt" in DataGrid → binding works, problem is in loading real data

### 4. Check if ObservableCollection sends notifications

Add event handler to ArchiveItems:
```csharp
ArchiveItems.CollectionChanged += (s, e) =>
{
    _logger.LogInformation("ArchiveItems changed: {Action}, NewItems: {Count}", 
        e.Action, e.NewItems?.Count ?? 0);
};
```

### 5. Check panel IsVisible binding

In XAML panel uses:
```xml
<StackPanel IsVisible="{Binding IsArchiveOpen}">
```

Add log when `IsArchiveOpen` changes:
```csharp
public bool IsArchiveOpen
{
    get
    {
        var result = _currentArchive != null;
        _logger.LogInformation("IsArchiveOpen getter called, result: {Result}", result);
        return result;
    }
}
```

## Possible Causes and Solutions

### Cause 1: Thread safety issue
**Symptoms**: Exception in logs about cross-thread operation

**Solution**: Use Dispatcher for collection operations (already implemented)

### Cause 2: Compiled bindings don't work
**Symptoms**: No errors, but UI doesn't update

**Solution**: Temporarily remove `x:DataType` from XAML and see if it helps

### Cause 3: DataContext is not set
**Symptoms**: DataContext is null

**Solution**: Check App.axaml.cs if `desktop.MainWindow = new MainWindow { DataContext = mainViewModel };` is executed

### Cause 4: Collection is not observed
**Symptoms**: Data is added but UI doesn't refresh

**Solution**: Ensure you're using `ObservableCollection` not `List`

### Cause 5: Timing issue
**Symptoms**: Data appears after a while or after window resize

**Solution**: Force UI refresh:
```csharp
Dispatcher.UIThread.Post(() =>
{
    this.RaisePropertyChanged(nameof(ArchiveItems));
}, DispatcherPriority.Render);
```

## Quick Fixes to Try

### Fix 1: Force UI refresh after loading
```csharp
private void LoadArchiveItemsCore()
{
    // ... existing code ...
    
    // Force refresh
    this.RaisePropertyChanged(nameof(ArchiveItems));
    this.RaisePropertyChanged(nameof(ArchiveInfo));
}
```

### Fix 2: Use Post instead of Schedule
```csharp
Dispatcher.UIThread.Post(() => LoadArchiveItemsCore(), DispatcherPriority.Normal);
```

### Fix 3: Delay refresh
```csharp
await Task.Delay(100); // Give UI time to process
this.RaisePropertyChanged(nameof(ArchiveItems));
```

## Final Verification

After opening archive check:

1. ✅ Window title changed → ViewModel works
2. ✅ Status bar shows "Loaded X file(s)" → LoadArchiveItems was called  
3. ❌ DataGrid is empty → Problem with binding or notification
4. ❌ Panel is invisible → Problem with IsArchiveOpen binding

If 1 and 2 work but 3 and 4 don't - the problem is in UI binding, not backend logic.

## Additional Diagnostic Tools

### Avalonia DevTools
Run with DevTools:
```bash
dotnet run
# In application press F12
```

In DevTools check:
- Visual Tree → find DataGrid → Properties → ItemsSource
- If ItemsSource is null → binding doesn't work
- If ItemsSource has elements but they're invisible → rendering problem

### Binding diagnostics in XAML
Temporarily add:
```xml
<TextBlock Text="{Binding ArchiveItems.Count}" />
```

If it shows correct number → ArchiveItems binding works, problem is in DataGrid

## Contact

If none of the above steps help, include in your report:
1. Full log from `dotnet run --verbosity detailed`
2. Screenshot of application after opening archive
3. DevTools inspection result (if possible)
