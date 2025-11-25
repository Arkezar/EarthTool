# EarthTool.WD.GUI - Architektura

## Przegląd

EarthTool.WD.GUI to aplikacja desktopowa zbudowana w oparciu o wzorzec MVVM (Model-View-ViewModel) wykorzystująca Avalonia UI i ReactiveUI.

## Wzorce architektoniczne

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

#### Separacja odpowiedzialności

**View (Views/)**
- Wyłącznie UI w XAML
- Minimalny code-behind (tylko inicjalizacja)
- Data binding do ViewModel
- Nie zawiera logiki biznesowej

**ViewModel (ViewModels/)**
- Logika prezentacji i biznesowa
- Commands dla akcji użytkownika
- Observable properties dla data binding
- Interakcja z Services
- Nie zna konkretnych View

**Model (EarthTool.WD/)**
- Logika domenowa (zarządzanie archiwami)
- Dostęp do danych (pliki WD)
- Niezależna od UI

### Dependency Injection

Aplikacja używa **Microsoft.Extensions.DependencyInjection** do zarządzania zależnościami.

#### Konfiguracja (App.axaml.cs)

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

| Service | Lifetime | Uzasadnienie |
|---------|----------|--------------|
| IDialogService | Singleton | Współdzielony dostęp do okien dialogowych |
| INotificationService | Singleton | Centralna obsługa powiadomień |
| MainWindowViewModel | Transient | Nowa instancja dla każdego okna |
| IArchiver | Scoped | Per operacja na archiwum |
| IArchiveFactory | Scoped | Per operacja tworzenia |

### Reactive Extensions (ReactiveUI)

#### ReactiveCommand

Wszystkie akcje użytkownika są implementowane jako `ReactiveCommand`:

```csharp
// Definicja
public ReactiveCommand<Unit, Unit> OpenArchiveCommand { get; private set; }

// Inicjalizacja z CanExecute
var canOpen = this.WhenAnyValue(x => x.IsReady);
OpenArchiveCommand = ReactiveCommand.CreateFromTask(OpenArchiveAsync, canOpen);

// Implementacja
private async Task OpenArchiveAsync()
{
    // Logika otwarcia archiwum
}
```

#### Observable Properties

Właściwości powiadamiają o zmianach automatycznie:

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

## Przepływ danych

### Otwieranie archiwum

```
User Click → Command → ViewModel → Service → Backend → Update Properties → UI Update
```

Szczegółowo:

1. Użytkownik klika "Open Archive"
2. `OpenArchiveCommand.Execute()` jest wywołane
3. `MainWindowViewModel.OpenArchiveAsync()` wykonuje:
   - Wywołuje `IDialogService.ShowOpenFileDialogAsync()`
   - Otrzymuje ścieżkę pliku
   - Wywołuje `IArchiver.OpenArchive(filePath)`
   - Otrzymuje `IArchive` z backendu
   - Konwertuje `IArchiveItem[]` na `ArchiveItemViewModel[]`
   - Aktualizuje `ArchiveItems` ObservableCollection
   - Aktualizuje `ArchiveInfo`
4. Data binding automatycznie aktualizuje UI (DataGrid, Info Panel)
5. StatusBar pokazuje komunikat sukcesu

### Ekstraktacja pliku

```
User Selection → Command → ViewModel → Dialog → Backend → Notification → Status Update
```

1. Użytkownik zaznacza plik w DataGrid
2. `SelectedItem` property jest aktualizowane (two-way binding)
3. `ExtractSelectedCommand.CanExecute` zmienia się na `true`
4. Użytkownik klika "Extract"
5. `ExtractSelectedAsync()` wykonuje:
   - Wywołuje `IDialogService.ShowFolderBrowserDialogAsync()`
   - Otrzymuje folder docelowy
   - Ustawia `IsBusy = true` (UI pokazuje progress)
   - Wywołuje `IArchiver.Extract(item, folder)` w `Task.Run`
   - Po zakończeniu wywołuje `INotificationService.ShowSuccess()`
   - Aktualizuje `StatusMessage`
   - Ustawia `IsBusy = false`

## Komponenty

### ViewModels

#### MainWindowViewModel

**Odpowiedzialności:**
- Orkiestracja głównych operacji aplikacji
- Zarządzanie stanem archiwum
- Obsługa komend użytkownika
- Koordynacja z Services

**Kluczowe properties:**
- `ArchiveItems` - Lista plików w archiwum
- `SelectedItem` - Aktualnie zaznaczony plik
- `ArchiveInfo` - Metadane archiwum
- `IsBusy` - Czy trwa operacja
- `StatusMessage` - Komunikat dla użytkownika
- `HasUnsavedChanges` - Czy są niezapisane zmiany

**Kluczowe commands:**
- `OpenArchiveCommand` - Otwórz archiwum
- `SaveArchiveCommand` - Zapisz zmiany
- `ExtractSelectedCommand` - Ekstraktuj plik
- `ExtractAllCommand` - Ekstraktuj wszystko
- `AddFilesCommand` - Dodaj pliki
- `RemoveSelectedCommand` - Usuń plik

#### ArchiveItemViewModel

**Odpowiedzialności:**
- Wrapper dla `IArchiveItem`
- Formatowanie danych dla UI
- Computed properties (ratio, formatted sizes)

**Kluczowe properties:**
- `Item` - Underlying IArchiveItem
- `FileName`, `CompressedSize`, `DecompressedSize`
- `CompressionRatio` - Obliczony współczynnik
- `FormattedCompressedSize` - Sformatowany rozmiar
- `Flags` - FileFlags enum

#### ArchiveInfoViewModel

**Odpowiedzialności:**
- Agregacja informacji o archiwum
- Formatowanie metadanych
- Obliczanie statystyk

**Kluczowe properties:**
- `FilePath`, `LastModification`, `ItemCount`
- `TotalCompressedSize`, `TotalDecompressedSize`
- `FormattedCompressionRatio` - Ogólny współczynnik

**Kluczowe metody:**
- `UpdateFromArchive(IArchive)` - Aktualizuj z archiwum
- `Clear()` - Wyczyść dane

### Services

#### IDialogService

**Odpowiedzialności:**
- Abstrakcja nad systemowymi dialogami
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

**Implementacja:**
- Używa `StorageProvider` API z Avalonia 11.x
- File filters dla plików .WD
- Centrowanie względem głównego okna

#### INotificationService

**Odpowiedzialności:**
- Centralna obsługa powiadomień
- Logging błędów
- Event-driven notifications

**API:**
```csharp
void ShowError(string message, Exception? exception);
void ShowWarning(string message);
void ShowSuccess(string message);
void ShowInfo(string message);
event EventHandler<NotificationEventArgs> NotificationRaised;
```

**Implementacja:**
- Integracja z `ILogger<T>`
- Event emission dla subskrypcji w ViewModels
- Różne poziomy logowania

### Converters

#### BytesToHumanReadableConverter

Konwertuje liczby bajtów na czytelny format (B, KB, MB, GB).

```csharp
1024 → "1.00 KB"
1048576 → "1.00 MB"
```

#### FileFlagsToStringConverter

Konwertuje `FileFlags` enum na string z opisem flag.

```csharp
FileFlags.Compressed | FileFlags.Named → "Compressed, Named"
```

#### BoolToVisibilityConverter

Konwertuje bool na visibility (true/false binding).

```csharp
true → Visible
false → Collapsed
```

## Error Handling Strategy

### Trzy poziomy obsługi

1. **Try-Catch w ViewModel methods**
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

2. **NotificationService agregacja**
   - Wszystkie błędy przez jeden service
   - Spójne logowanie
   - Centralne formatowanie komunikatów

3. **UI Feedback**
   - StatusMessage w StatusBar
   - MessageBox dla krytycznych błędów
   - IsBusy indicator podczas operacji

### Wzorzec Try-Finally dla IsBusy

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
    IsBusy = false;  // ZAWSZE resetuj
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

Wszystkie I/O operacje są asynchroniczne:

```csharp
await Task.Run(() => _archiver.OpenArchive(filePath));
```

Korzyści:
- UI pozostaje responsywny
- Brak blokowania głównego wątku
- Progress indicators mogą być wyświetlane

### ObservableCollection Updates

Aktualizacje list są batched gdzie możliwe:

```csharp
ArchiveItems.Clear();
foreach (var item in archive.Items)
{
    ArchiveItems.Add(new ArchiveItemViewModel(item));
}
```

### Memory Management

- `IArchive` implementuje `IDisposable` (memory-mapped files)
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

Unit testy z mockowanymi services:

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

Testy end-to-end z rzeczywistymi services i testowymi archiwami.

### Manual Testing

Scenariusze testowe opisane w README.md.

## Deployment

### Single File Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Cross-Platform

Avalonia wspiera:
- Windows (win-x64, win-arm64)
- Linux (linux-x64, linux-arm, linux-arm64)
- macOS (osx-x64, osx-arm64)

## Future Architecture Improvements

### Planned Enhancements

1. **Multi-selection support** - Refaktoryzacja SelectedItem → SelectedItems collection
2. **Background operations** - Queue dla długich operacji
3. **Plugin system** - Extensibility dla custom file handlers
4. **Undo/Redo** - Command pattern z history
5. **Async initialization** - Lazy loading dla ViewModels

### Technical Debt

- [ ] Dodać unit testy dla ViewModels
- [ ] Dodać integration testy
- [ ] Refaktor DialogService na używanie behaviors
- [ ] Implementacja Attached Properties dla SelectedItems binding
- [ ] Progress reporting dla długich operacji

## Diagram klas

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

## Konkluzja

Architektura EarthTool.WD.GUI jest zaprojektowana z myślą o:

- **Separacji odpowiedzialności** - MVVM pattern
- **Testowalności** - Dependency Injection
- **Maintainability** - Jasna struktura folderów i klasy Single Responsibility
- **Extensibility** - Service abstractions i plugin-ready architecture
- **User Experience** - Reactive UI z feedback w czasie rzeczywistym
