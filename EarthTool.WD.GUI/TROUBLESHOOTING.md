# Troubleshooting - DataGrid i Panel Information puste po otwarciu archiwum

## Problem
Status bar i tytuł okna pokazują poprawne informacje, ale:
- DataGrid (Archive Contents) jest pusty
- Panel Information jest pusty lub niewidoczny

## Diagnostyka krok po kroku

### 1. Sprawdź czy dane są ładowane

Uruchom aplikację z logowaniem:
```bash
cd /home/arkezar/Source/EarthTool/EarthTool.WD.GUI
dotnet run --verbosity detailed > app.log 2>&1
```

Otwórz archiwum WD, potem sprawdź `app.log`:

**Szukaj tych linii:**
```
MainWindowViewModel constructed
LoadArchiveItems called. Archive is null: False
Archive has X items
Added X items to ArchiveItems collection
```

**Jeśli widzisz te logi** → dane SĄ ładowane, problem jest w bindingu UI  
**Jeśli NIE widzisz tych logów** → problem jest w ładowaniu danych

### 2. Sprawdź DataContext

Dodaj do MainWindow.axaml.cs w konstruktorze:
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

Uruchom aplikację - powinieneś zobaczyć:
```
DataContext type: MainWindowViewModel
ArchiveItems count: 0
```

### 3. Test binding z testowymi danymi

Tymczasowo dodaj testowe dane w MainWindowViewModel konstruktorze:
```csharp
// DEBUG: Add test data
ArchiveItems.Add(new ArchiveItemViewModel(
    new TestArchiveItem("test.txt", 100, 200)));
```

Jeśli po tym widzisz "test.txt" w DataGrid → binding działa, problem jest w ładowaniu prawdziwych danych

### 4. Sprawdź czy ObservableCollection wysyła notyfikacje

Dodaj event handler do ArchiveItems:
```csharp
ArchiveItems.CollectionChanged += (s, e) =>
{
    _logger.LogInformation("ArchiveItems changed: {Action}, NewItems: {Count}", 
        e.Action, e.NewItems?.Count ?? 0);
};
```

### 5. Sprawdź panel IsVisible binding

W XAML panel używa:
```xml
<StackPanel IsVisible="{Binding IsArchiveOpen}">
```

Dodaj log gdy `IsArchiveOpen` się zmienia:
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

## Możliwe przyczyny i rozwiązania

### Przyczyna 1: Thread safety issue
**Objawy**: Exception w logach o cross-thread operation

**Rozwiązanie**: Użyj Dispatcher dla operacji na kolekcji (już zaimplementowane)

### Przyczyna 2: Compiled bindings nie działają
**Objawy**: Brak błędów, ale UI nie aktualizuje się

**Rozwiązanie**: Tymczasowo usuń `x:DataType` z XAML i zobacz czy pomaga

### Przyczyna 3: DataContext nie jest ustawiony
**Objawy**: DataContext jest null

**Rozwiązanie**: Sprawdź App.axaml.cs czy `desktop.MainWindow = new MainWindow { DataContext = mainViewModel };` jest wykonywane

### Przyczyna 4: Collection nie jest obserwowana
**Objawy**: Dane są dodawane ale UI się nie odświeża

**Rozwiązanie**: Upewnij się że używasz `ObservableCollection` a nie `List`

### Przyczyna 5: Timing issue
**Objawy**: Dane się pojawiają po chwili lub po resize okna

**Rozwiązanie**: Wymuś refresh UI:
```csharp
Dispatcher.UIThread.Post(() =>
{
    this.RaisePropertyChanged(nameof(ArchiveItems));
}, DispatcherPriority.Render);
```

## Quick fixes do wypróbowania

### Fix 1: Force UI refresh po załadowaniu
```csharp
private void LoadArchiveItemsCore()
{
    // ... existing code ...
    
    // Force refresh
    this.RaisePropertyChanged(nameof(ArchiveItems));
    this.RaisePropertyChanged(nameof(ArchiveInfo));
}
```

### Fix 2: Używaj Post zamiast Schedule
```csharp
Dispatcher.UIThread.Post(() => LoadArchiveItemsCore(), DispatcherPriority.Normal);
```

### Fix 3: Delay refresh
```csharp
await Task.Delay(100); // Give UI time to process
this.RaisePropertyChanged(nameof(ArchiveItems));
```

## Weryfikacja ostateczna

Po otwarciu archiwum sprawdź:

1. ✅ Tytuł okna zmienił się → ViewModel działa
2. ✅ Status bar pokazuje "Loaded X file(s)" → LoadArchiveItems zostało wywołane  
3. ❌ DataGrid jest pusty → Problem z bindingiem lub notyfikacją
4. ❌ Panel jest niewidoczny → Problem z IsArchiveOpen binding

Jeśli 1 i 2 działają ale 3 i 4 nie - to problem jest w UI bindingu, nie w logice backendu.

## Dodatkowe narzędzia diagnostyczne

### Avalonia DevTools
Uruchom z DevTools:
```bash
dotnet run
# W aplikacji naciśnij F12
```

W DevTools sprawdź:
- Visual Tree → znajdź DataGrid → Properties → ItemsSource
- Jeśli ItemsSource jest null → binding nie działa
- Jeśli ItemsSource ma elementy ale są niewidoczne → problem z renderowaniem

### Binding diagnostics w XAML
Dodaj tymczasowo:
```xml
<TextBlock Text="{Binding ArchiveItems.Count}" />
```

Jeśli pokazuje się prawidłowa liczba → ArchiveItems binding działa, problem jest w DataGrid

## Kontakt

Jeśli żaden z powyższych kroków nie pomaga, dołącz do zgłoszenia:
1. Pełny log z `dotnet run --verbosity detailed`
2. Screenshot aplikacji po otwarciu archiwum
3. Wynik DevTools inspection (jeśli możliwy)
