# EarthTool.WD.GUI - WD Archive Manager

Graficzny interfejs użytkownika do zarządzania archiwami WD z gry Earth 2150.

## Przegląd

EarthTool.WD.GUI to aplikacja desktopowa zbudowana w Avalonia UI, która umożliwia łatwą pracę z plikami archiwów WD używanymi przez grę Earth 2150. Aplikacja oferuje intuicyjny interfejs do przeglądania, ekstraktowania i modyfikowania zawartości archiwów.

## Funkcjonalności

### Podstawowe operacje

- **Otwieranie archiwów** - Wczytywanie istniejących plików .WD
- **Tworzenie nowych archiwów** - Generowanie pustych archiwów WD
- **Zapisywanie archiwów** - Zapis zmian do pliku (Save/Save As)
- **Zamykanie archiwów** - Bezpieczne zamykanie z ostrzeżeniem o niezapisanych zmianach

### Zarządzanie plikami

- **Przeglądanie zawartości** - Lista wszystkich plików w archiwum z szczegółowymi informacjami
- **Ekstrakcja pojedynczego pliku** - Wyodrębnienie wybranego pliku do folderu
- **Ekstrakcja wszystkich plików** - Wyodrębnienie całej zawartości archiwum
- **Dodawanie plików** - Wstawianie nowych plików do archiwum z kompresją
- **Usuwanie plików** - Usuwanie wybranych plików z archiwum

### Informacje o archiwum

- Nazwa i ścieżka pliku
- Data ostatniej modyfikacji
- Liczba plików
- Całkowity rozmiar (skompresowany i nieskompresowany)
- Współczynnik kompresji
- Informacje o nagłówku archiwum

### Szczegóły plików

Dla każdego pliku w archiwum wyświetlane są:
- Nazwa pliku
- Rozmiar skompresowany
- Rozmiar nieskompresowany
- Współczynnik kompresji
- Flagi pliku (Compressed, Archive, Text, Named, Resource, Guid)

## Architektura

### Wzorce projektowe

- **MVVM (Model-View-ViewModel)** - Separacja logiki biznesowej od interfejsu użytkownika
- **Dependency Injection** - Użycie Microsoft.Extensions.DependencyInjection
- **Command Pattern** - ReactiveCommand z ReactiveUI dla akcji użytkownika
- **Observer Pattern** - ReactiveUI dla powiadomień o zmianach właściwości

### Struktura projektu

```
EarthTool.WD.GUI/
├── ViewModels/              # Logika biznesowa
│   ├── MainWindowViewModel.cs        # Główny ViewModel
│   ├── ArchiveItemViewModel.cs       # Wrapper dla IArchiveItem
│   ├── ArchiveInfoViewModel.cs       # Informacje o archiwum
│   ├── AboutViewModel.cs             # Dialog "O programie"
│   └── ViewModelBase.cs              # Bazowa klasa ViewModels
│
├── Views/                   # Interfejs użytkownika (XAML)
│   ├── MainWindow.axaml              # Główne okno aplikacji
│   └── MainWindow.axaml.cs           # Code-behind
│
├── Services/                # Serwisy UI
│   ├── IDialogService.cs             # Interfejs dialogów
│   ├── DialogService.cs              # Implementacja dialogów
│   ├── INotificationService.cs       # Interfejs powiadomień
│   └── NotificationService.cs        # Implementacja powiadomień
│
├── Converters/              # Value Converters dla data binding
│   ├── BytesToHumanReadableConverter.cs
│   ├── FileFlagsToStringConverter.cs
│   └── BoolToVisibilityConverter.cs
│
├── App.axaml.cs             # Konfiguracja aplikacji i DI
└── Program.cs               # Entry point
```

### Zależności

- **Avalonia 11.3.9** - Cross-platform UI framework
- **ReactiveUI.Avalonia 11.3.8** - MVVM framework z Reactive Extensions
- **Microsoft.Extensions.DependencyInjection 8.0.0** - Dependency Injection
- **Microsoft.Extensions.Logging 8.0.0** - Logging abstractions
- **EarthTool.WD** - Backend dla operacji na archiwach WD
- **EarthTool.Common** - Wspólne interfejsy i narzędzia

## Użycie

### Uruchomienie aplikacji

```bash
cd EarthTool.WD.GUI
dotnet run
```

### Otwieranie archiwum

1. Kliknij **File → Open Archive** lub użyj **Ctrl+O**
2. Wybierz plik .WD z dysku
3. Zawartość archiwum zostanie wyświetlona w głównej tabeli

### Ekstraktowanie plików

**Pojedynczy plik:**
1. Zaznacz plik w tabeli
2. Kliknij **Archive → Extract Selected** lub przycisk **Extract** na pasku narzędzi
3. Wybierz folder docelowy

**Wszystkie pliki:**
1. Kliknij **Archive → Extract All** lub **Ctrl+E**
2. Wybierz folder docelowy

### Tworzenie nowego archiwum

1. Kliknij **File → New Archive** lub **Ctrl+N**
2. Dodaj pliki używając **Archive → Add Files** lub **Ctrl+A**
3. Zapisz archiwum używając **File → Save Archive As**

### Modyfikowanie archiwum

1. Otwórz istniejące archiwum
2. Dodaj nowe pliki: **Archive → Add Files**
3. Usuń pliki: Zaznacz plik → **Archive → Remove Selected** lub **Del**
4. Zapisz zmiany: **File → Save Archive** lub **Ctrl+S**

## Skróty klawiszowe

| Skrót | Akcja |
|-------|-------|
| `Ctrl+O` | Otwórz archiwum |
| `Ctrl+N` | Nowe archiwum |
| `Ctrl+S` | Zapisz archiwum |
| `Ctrl+Shift+S` | Zapisz jako... |
| `Ctrl+E` | Ekstraktuj wszystko |
| `Ctrl+A` | Dodaj pliki |
| `Del` | Usuń zaznaczony plik |

## Dependency Injection

Aplikacja używa Microsoft.Extensions.DependencyInjection do zarządzania zależnościami.

### Konfiguracja serwisów (App.axaml.cs)

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

- **Singleton** - DialogService, NotificationService (współdzielone przez całą aplikację)
- **Transient** - ViewModels (nowa instancja dla każdego okna)
- **Scoped** - Services z EarthTool.WD (per operacja)

## Obsługa błędów

Aplikacja implementuje kompleksową obsługę błędów:

### Centralna obsługa w ViewModels

Wszystkie operacje asynchroniczne są opakowane w bloki try-catch:

```csharp
try
{
    IsBusy = true;
    StatusMessage = "Operacja w toku...";
    await PerformOperationAsync();
    _notificationService.ShowSuccess("Operacja zakończona");
}
catch (Exception ex)
{
    _notificationService.ShowError("Operacja nie powiodła się", ex);
    StatusMessage = "Błąd";
}
finally
{
    IsBusy = false;
}
```

### NotificationService

Wszystkie błędy są logowane i wyświetlane użytkownikowi przez `INotificationService`:

- **ShowError** - Wyświetla błąd i loguje exception
- **ShowWarning** - Ostrzeżenie dla użytkownika
- **ShowSuccess** - Potwierdzenie pomyślnej operacji
- **ShowInfo** - Informacje ogólne

### StatusBar

Status bieżącej operacji jest zawsze wyświetlany w dolnym pasku:
- Komunikaty o postępie operacji
- Informacje o błędach
- Potwierdzenia zakończonych działań
- Pasek postępu dla operacji długotrwałych

## Rozszerzanie funkcjonalności

### Dodawanie nowego Command

1. Zdefiniuj property w MainWindowViewModel:
```csharp
public ReactiveCommand<Unit, Unit> MyCommand { get; private set; } = null!;
```

2. Zainicjalizuj w `InitializeCommands()`:
```csharp
var canExecute = this.WhenAnyValue(x => x.SomeCondition);
MyCommand = ReactiveCommand.CreateFromTask(MyMethodAsync, canExecute);
```

3. Implementuj metodę:
```csharp
private async Task MyMethodAsync()
{
    try
    {
        // Logika
    }
    catch (Exception ex)
    {
        _notificationService.ShowError("Błąd", ex);
    }
}
```

4. Podłącz w XAML:
```xml
<MenuItem Header="My Action" Command="{Binding MyCommand}"/>
```

### Dodawanie nowego Value Converter

1. Utwórz klasę implementującą `IValueConverter`:
```csharp
public class MyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, 
        object? parameter, CultureInfo culture)
    {
        // Konwersja
    }
}
```

2. Dodaj do Resources w XAML:
```xml
<Window.Resources>
    <converters:MyConverter x:Key="MyConverter"/>
</Window.Resources>
```

3. Użyj w binding:
```xml
<TextBlock Text="{Binding Value, Converter={StaticResource MyConverter}}"/>
```

## Testowanie

### Manualne scenariusze testowe

1. **Test otwierania archiwum**
   - Otwórz prawidłowy plik .WD
   - Sprawdź czy lista plików jest wyświetlona
   - Sprawdź informacje o archiwum w panelu prawym

2. **Test ekstraktacji**
   - Zaznacz plik
   - Ekstraktuj do folderu
   - Zweryfikuj że plik został utworzony

3. **Test tworzenia archiwum**
   - Utwórz nowe archiwum
   - Dodaj kilka plików
   - Zapisz jako nowy plik .WD
   - Otwórz zapisany plik i zweryfikuj zawartość

4. **Test modyfikacji**
   - Otwórz archiwum
   - Dodaj nowe pliki
   - Usuń niektóre pliki
   - Zapisz i ponownie otwórz

5. **Test obsługi błędów**
   - Spróbuj otworzyć nieprawidłowy plik
   - Spróbuj zapisać do chronionego folderu
   - Zweryfikuj komunikaty błędów

## Znane ograniczenia

- **Selekcja pojedyncza** - Obecnie obsługiwana jest tylko pojedyncza selekcja plików (planowane wielokrotne zaznaczanie w przyszłej wersji)
- **Brak Progress dla długich operacji** - Ekstraktacja dużych archiwów pokazuje tylko indicator indeterminate (planowany szczegółowy progress)
- **Brak Preview plików** - Podgląd zawartości plików tekstowych planowany w przyszłej wersji

## Roadmap

### Przyszłe funkcjonalności

- [ ] Multi-selection w DataGrid
- [ ] Szczegółowy progress bar dla długich operacji
- [ ] Podgląd plików tekstowych
- [ ] Drag & Drop dla dodawania plików
- [ ] Historia ostatnio otwieranych plików
- [ ] Wyszukiwanie plików w archiwum
- [ ] Sortowanie i filtrowanie listy plików
- [ ] Eksport listy plików do CSV
- [ ] Porównywanie dwóch archiwów
- [ ] Batch operations (operacje na wielu archiwach)

### Ulepszenia UI

- [ ] Ikony zamiast emoji w menu
- [ ] Motywy (Light/Dark mode)
- [ ] Konfigurowalne kolory
- [ ] Zakładki dla wielu otwartych archiwów
- [ ] Statusbar z dodatkowymi informacjami

## Licencja

Część projektu EarthTool. Zobacz główny plik LICENSE w katalogu głównym repozytorium.

## Kontakt i wsparcie

Aby zgłosić błędy lub zaproponować nowe funkcje, użyj systemu Issues w repozytorium GitHub.

## Podziękowania

- Społeczność Earth 2150 za dokumentację formatu WD
- Zespół Avalonia UI za doskonały framework
- Autorzy ReactiveUI za MVVM framework
