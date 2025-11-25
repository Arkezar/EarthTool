# Debugging Guide - EarthTool.WD.GUI

## Problem: UI nie odświeża się po otwarciu archiwum

### Przyczyna
Problem wynikał z nieprawidłowej aktualizacji `ObservableCollection` z wątku background.

### Rozwiązanie
Dodano `RxApp.MainThreadScheduler` aby wymusić wykonanie operacji na kolekcji w wątku UI:

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

### Dodatkowe poprawki
1. **Przeniesienie przypisania `_currentArchive` poza `Task.Run`**
   - Operacje I/O w background thread
   - Przypisanie do pola i notification w UI thread

2. **Explicit `RaisePropertyChanged(nameof(IsArchiveOpen))`**
   - Wywoływane zaraz po zmianie `_currentArchive`
   - Zapewnia aktualizację visibility panelu informacyjnego

### Weryfikacja
Aby sprawdzić czy problem jest rozwiązany:

1. Uruchom aplikację z logowaniem Debug:
```bash
export DOTNET_ENVIRONMENT=Development
dotnet run --verbosity detailed
```

2. Otwórz archiwum WD

3. Sprawdź czy w konsoli pojawiają się logi:
```
LoadArchiveItems called. Archive is null: False
Archive has X items
Added X items to ArchiveItems collection
```

4. Sprawdź UI:
   - DataGrid powinien pokazywać listę plików
   - Panel "Archive Information" powinien być widoczny
   - Wszystkie pola (File, Last Modified, Files, etc.) powinny mieć wartości

### Typowe problemy

#### Problem: "Cross-thread operation exception"
**Objawy**: Wyjątek podczas dodawania do `ObservableCollection`

**Rozwiązanie**: Upewnij się że operacje na kolekcji są w `RxApp.MainThreadScheduler.Schedule()`

#### Problem: Panel informacyjny nie pokazuje się
**Objawy**: Lista plików się wyświetla ale prawy panel jest pusty

**Rozwiązanie**: Sprawdź czy `IsArchiveOpen` property jest prawidłowo powiadamiane:
```csharp
this.RaisePropertyChanged(nameof(IsArchiveOpen));
```

#### Problem: Dane w panelu są puste
**Objawy**: Panel jest widoczny ale wszystkie pola pokazują "N/A"

**Rozwiązanie**: Sprawdź czy `ArchiveInfo.UpdateFromArchive()` jest wywoływane z niepustym archiwum

### Debug logging

Dodano logi debug w `LoadArchiveItems()`:
- Stan archiwum (null/not null)
- Liczba elementów w archiwum
- Liczba dodanych elementów do kolekcji

Aby je zobaczyć, ustaw poziom logowania na Debug w App.axaml.cs:
```csharp
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug); // Zmień z Information na Debug
});
```

### Performance considerations

`RxApp.MainThreadScheduler.Schedule()` dodaje małe opóźnienie, ale:
- Zapewnia thread-safety
- Unika cross-thread exceptions
- Standardowa praktyka w ReactiveUI

Dla bardzo dużych archiwów (1000+ plików) rozważ:
1. Virtualization w DataGrid
2. Batch loading z progressem
3. Async initialization ViewModels

### Test cases

Przetestuj następujące scenariusze:

1. **Otwarcie małego archiwum** (< 10 plików)
   - Wszystkie pliki powinny się pokazać natychmiast
   - Info panel powinien mieć poprawne dane

2. **Otwarcie dużego archiwum** (100+ plików)
   - Może pojawić się krótkie opóźnienie
   - IsBusy indicator powinien się pokazać

3. **Otwarcie uszkodzonego archiwum**
   - Error message powinien się pokazać
   - UI powinno pozostać responsywne

4. **Utworzenie nowego archiwum**
   - Lista plików powinna być pusta
   - Info panel powinien pokazać "0 files"

5. **Dodanie plików do archiwum**
   - Nowe pliki powinny się pokazać w liście
   - Liczniki w info panel powinny się zaktualizować

### Related issues

- ReactiveUI threading: https://www.reactiveui.net/docs/handbook/scheduling/
- Avalonia threading: https://docs.avaloniaui.net/docs/concepts/services/dispatcher
