# Przewodnik użytkownika - EarthTool WD Archive Manager

## Spis treści

1. [Wprowadzenie](#wprowadzenie)
2. [Pierwsze kroki](#pierwsze-kroki)
3. [Podstawowe operacje](#podstawowe-operacje)
4. [Zaawansowane funkcje](#zaawansowane-funkcje)
5. [Rozwiązywanie problemów](#rozwiązywanie-problemów)
6. [FAQ](#faq)

## Wprowadzenie

EarthTool WD Archive Manager to narzędzie graficzne do zarządzania archiwami WD używanymi przez grę Earth 2150. Aplikacja umożliwia przeglądanie, ekstraktowanie i modyfikowanie zawartości tych archiwów.

### Czym są pliki WD?

Pliki z rozszerzeniem `.WD` to spakowane archiwa zawierające zasoby gry Earth 2150 (modele, tekstury, dźwięki, skrypty itp.). Format WD używa kompresji do zmniejszenia rozmiaru plików.

## Pierwsze kroki

### Uruchomienie aplikacji

1. Uruchom `EarthTool.WD.GUI.exe`
2. Zobaczysz główne okno aplikacji z pustą listą plików
3. Pasek statusu na dole pokazuje "Ready"

### Interfejs użytkownika

```
┌──────────────────────────────────────────────────────────────┐
│  File  Archive  Help                           [Menu Bar]    │
├──────────────────────────────────────────────────────────────┤
│  📂 Open  💾 Save │ 📤 Extract  📦 All │ ➕ Add  🗑️ Remove  │
│                                           [Toolbar]           │
├──────────────────────────────────┬───────────────────────────┤
│                                  │  Archive Information      │
│  Archive Contents                │                           │
│  ┌────────────────────────────┐ │  File: [path]            │
│  │ FileName │ Size │ Ratio │  │ │  Modified: [date]        │
│  ├──────────┼──────┼───────┤  │ │  Files: [count]          │
│  │ file1.msh│ 12KB │  45% │  │ │  Size: [total]           │
│  │ file2.tex│ 34KB │  67% │  │ │                           │
│  └────────────────────────────┘ │                           │
│                                  │                           │
│           [Main Area]            │      [Info Panel]         │
├──────────────────────────────────┴───────────────────────────┤
│  Status: Ready                           Items: 0            │
│                                         [Status Bar]          │
└──────────────────────────────────────────────────────────────┘
```

## Podstawowe operacje

### 1. Otwieranie archiwum

**Metoda 1: Menu**
1. Kliknij `File → Open Archive...`
2. Wybierz plik `.WD` z dysku
3. Kliknij "Open"

**Metoda 2: Skrót klawiszowy**
- Naciśnij `Ctrl+O`
- Wybierz plik
- Kliknij "Open"

**Metoda 3: Toolbar**
- Kliknij przycisk 📂 "Open" na pasku narzędzi

**Co się dzieje:**
- Lista plików zostanie zapełniona zawartością archiwum
- Panel informacyjny po prawej pokazuje szczegóły archiwum
- Pasek statusu pokazuje liczbę załadowanych plików

### 2. Przeglądanie zawartości

**Lista plików pokazuje:**
- **File Name** - Nazwa pliku z archiwum (może zawierać ścieżkę)
- **Compressed** - Rozmiar skompresowany w archiwum
- **Decompressed** - Rzeczywisty rozmiar po rozpakowaniu
- **Ratio** - Współczynnik kompresji w procentach
- **Flags** - Flagi pliku (Compressed, Named, Text, etc.)

**Panel informacyjny pokazuje:**
- Ścieżka do otwartego archiwum
- Data ostatniej modyfikacji
- Całkowita liczba plików
- Łączny rozmiar (skompresowany i nieskompresowany)
- Ogólny współczynnik kompresji

**Sortowanie:**
- Kliknij na nagłówek kolumny aby posortować
- Kliknij ponownie aby odwrócić kolejność

### 3. Ekstraktowanie plików

#### Pojedynczy plik

1. **Zaznacz plik** w tabeli (kliknij na wiersz)
2. **Wybierz akcję ekstraktacji:**
   - Menu: `Archive → Extract Selected...`
   - Toolbar: Kliknij 📤 "Extract"
   - Kontekst menu: Prawy przycisk → "Extract..."
3. **Wybierz folder docelowy**
4. Kliknij "Select Folder"

**Rezultat:**
- Plik zostanie wyekstraktowany do wybranego folderu
- Jeśli plik był skompresowany, zostanie automatycznie rozpakowany
- Komunikat sukcesu pojawi się w statusie
- Plik zachowa swoją oryginalną nazwę

#### Wszystkie pliki

1. **Wybierz akcję:**
   - Menu: `Archive → Extract All...`
   - Toolbar: Kliknij 📦 "Extract All"
   - Skrót: `Ctrl+E`
2. **Wybierz folder docelowy**
3. Kliknij "Select Folder"

**Rezultat:**
- Wszystkie pliki zostaną wyekstraktowane
- Struktura katalogów z archiwum zostanie zachowana
- Pasek postępu pokazuje operację w trakcie
- Po zakończeniu zobaczysz komunikat z liczbą wyekstraktowanych plików

### 4. Tworzenie nowego archiwum

1. **Utwórz archiwum:**
   - Menu: `File → New Archive`
   - Skrót: `Ctrl+N`

2. **Dodaj pliki:**
   - Menu: `Archive → Add Files...`
   - Toolbar: Kliknij ➕ "Add"
   - Skrót: `Ctrl+A`
   - Wybierz jeden lub więcej plików
   - Kliknij "Open"

3. **Zapisz archiwum:**
   - Menu: `File → Save Archive As...`
   - Skrót: `Ctrl+Shift+S`
   - Wybierz nazwę i lokalizację
   - Kliknij "Save"

**Wskazówki:**
- Nowe archiwum jest początkowo puste
- Możesz dodać wiele plików naraz
- Pliki są automatycznie kompresowane podczas dodawania
- Tytuł okna pokazuje gwiazdkę (*) jeśli są niezapisane zmiany

### 5. Modyfikowanie istniejącego archiwum

#### Dodawanie plików

1. Otwórz istniejące archiwum
2. Kliknij `Archive → Add Files...` lub `Ctrl+A`
3. Wybierz pliki do dodania
4. Kliknij "Open"
5. Zapisz zmiany: `Ctrl+S`

**Uwagi:**
- Nowe pliki pojawią się w liście
- Struktura katalogów jest zachowana na podstawie lokalizacji plików
- Duplikaty nazw są dozwolone (nazwa z pełną ścieżką)

#### Usuwanie plików

1. Zaznacz plik w tabeli
2. **Wybierz akcję usunięcia:**
   - Menu: `Archive → Remove Selected`
   - Toolbar: Kliknij 🗑️ "Remove"
   - Skrót: `Delete` lub `Del`
   - Kontekst menu: Prawy przycisk → "Remove"
3. Potwierdź usunięcie w dialogu
4. Zapisz zmiany: `Ctrl+S`

**Ostrzeżenie:**
- Usunięcie jest trwałe po zapisaniu archiwum
- Zawsze pojawia się dialog potwierdzenia
- Możesz anulować przed zapisaniem (zamknij bez zapisu)

### 6. Zapisywanie zmian

#### Save (Zapisz)
- Menu: `File → Save Archive`
- Skrót: `Ctrl+S`
- Zapisuje do oryginalnego pliku
- Dostępne tylko gdy są niezapisane zmiany

#### Save As (Zapisz jako)
- Menu: `File → Save Archive As...`
- Skrót: `Ctrl+Shift+S`
- Zapisuje do nowego pliku
- Oryginalny plik pozostaje niezmieniony

### 7. Zamykanie archiwum

1. Kliknij `File → Close Archive`
2. Jeśli są niezapisane zmiany, pojawi się dialog:
   - **Yes** - Zapisz i zamknij
   - **No** - Zamknij bez zapisywania
   - **Cancel** - Anuluj zamykanie

## Zaawansowane funkcje

### Skróty klawiszowe

| Skrót | Akcja |
|-------|-------|
| `Ctrl+O` | Otwórz archiwum |
| `Ctrl+N` | Nowe archiwum |
| `Ctrl+S` | Zapisz |
| `Ctrl+Shift+S` | Zapisz jako... |
| `Ctrl+E` | Ekstraktuj wszystko |
| `Ctrl+A` | Dodaj pliki |
| `Delete` / `Del` | Usuń zaznaczony plik |
| `F5` | Odśwież (przyszła funkcja) |

### Kontekstowe menu

Kliknij prawym przyciskiem myszy na plik w tabeli aby otworzyć menu kontekstowe:
- **Extract...** - Ekstraktuj wybrany plik
- **Remove** - Usuń plik z archiwum

### Status bar

Dolny pasek pokazuje:
- **Po lewej:** Komunikaty statusu (Ready, Loading, Error, Success)
- **W środku:** Pasek postępu dla długich operacji
- **Po prawej:** Liczba plików w archiwum

### Panel informacyjny

Prawy panel zawiera:
- **File:** Pełna ścieżka do otwartego archiwum
- **Last Modified:** Data ostatniej modyfikacji archiwum
- **Files:** Liczba plików w archiwum
- **Total Compressed Size:** Łączny rozmiar w archiwum
- **Total Decompressed Size:** Rzeczywisty rozmiar wszystkich plików
- **Overall Compression:** Średni współczynnik kompresji

## Rozwiązywanie problemów

### Problem: "Nie mogę otworzyć archiwum"

**Możliwe przyczyny:**
1. Plik nie jest prawidłowym archiwum WD
2. Plik jest uszkodzony
3. Brak uprawnień do odczytu pliku

**Rozwiązanie:**
- Sprawdź czy plik ma rozszerzenie `.WD`
- Spróbuj otworzyć inny plik WD aby sprawdzić czy aplikacja działa
- Sprawdź uprawnienia do pliku (kliknij prawym → Properties)
- Zobacz komunikat błędu w status bar lub message box

### Problem: "Ekstraktacja kończy się błędem"

**Możliwe przyczyny:**
1. Brak uprawnień do zapisu w folderze docelowym
2. Brak miejsca na dysku
3. Plik w archiwum jest uszkodzony

**Rozwiązanie:**
- Wybierz inny folder docelowy (np. Desktop)
- Sprawdź wolne miejsce na dysku
- Spróbuj wyekstraktować inny plik
- Sprawdź logi w konsoli (jeśli dostępne)

### Problem: "Nie mogę zapisać archiwum"

**Możliwe przyczyny:**
1. Brak uprawnień do zapisu
2. Plik jest otwarty w innym programie
3. Brak miejsca na dysku

**Rozwiązanie:**
- Użyj "Save As" do zapisania w innej lokalizacji
- Zamknij inne aplikacje które mogą używać pliku
- Sprawdź wolne miejsce na dysku
- Uruchom aplikację jako administrator (jeśli potrzebne)

### Problem: "Aplikacja się zawiesza podczas operacji"

**Możliwe przyczyny:**
1. Bardzo duże archiwum
2. Powolny dysk
3. Brak pamięci RAM

**Rozwiązanie:**
- Poczekaj - operacje na dużych archiwach mogą trwać
- Sprawdź pasek postępu - jeśli się porusza, operacja trwa
- Zamknij inne aplikacje aby zwolnić pamięć
- Dla bardzo dużych archiwów rozważ użycie wersji CLI

### Problem: "Niezapisane zmiany zostały utracone"

**Zapobieganie:**
- Zawsze zapisuj zmiany przed zamknięciem: `Ctrl+S`
- Aplikacja ostrzega o niezapisanych zmianach przed zamknięciem
- Tytuł okna pokazuje `*` gdy są niezapisane zmiany

## FAQ

### Czy mogę otworzyć wiele archiwów jednocześnie?

Obecnie aplikacja obsługuje tylko jedno archiwum na raz. Wsparcie dla zakładek jest planowane w przyszłej wersji.

### Czy mogę zaznaczyć wiele plików do ekstraktacji?

Obecnie obsługiwana jest tylko pojedyncza selekcja. Możesz jednak użyć "Extract All" aby wyekstraktować wszystkie pliki naraz. Multi-selection jest planowane.

### Czy aplikacja modyfikuje oryginalne pliki?

Nie, dopóki nie zapiszesz zmian. Wszystkie modyfikacje są w pamięci do momentu kliknięcia "Save". Używając "Save As" możesz zachować oryginał nienaruszony.

### Jakie formaty plików są wspierane?

Aplikacja obsługuje wyłącznie format archiwów WD z gry Earth 2150. Pliki wewnątrz archiwum mogą być dowolnego typu (MSH, TEX, PAR, etc.).

### Czy pliki są automatycznie kompresowane?

Tak, podczas dodawania plików do archiwum są one automatycznie kompresowane przy użyciu algorytmu stosowanego przez Earth 2150.

### Czy mogę podejrzeć zawartość pliku przed ekstraktacją?

Obecnie nie. Podgląd plików tekstowych jest planowany w przyszłej wersji.

### Jak mogę sprawdzić czy plik jest skompresowany?

Kolumna "Flags" pokazuje flagę "Compressed" dla skompresowanych plików. Dodatkowo kolumna "Ratio" pokazuje współczynnik kompresji.

### Czy aplikacja działa na Linuxie/Mac?

Tak! Avalonia UI wspiera cross-platform. Potrzebujesz tylko .NET 8.0 runtime. Zbuduj dla swojej platformy:

```bash
# Linux
dotnet publish -c Release -r linux-x64

# macOS
dotnet publish -c Release -r osx-x64
```

### Gdzie są zapisywane logi?

Logi są obecnie wypisywane do konsoli (jeśli uruchomiona z terminala). Wsparcie dla plików logów jest planowane.

### Jak zgłosić błąd?

Użyj systemu Issues w repozytorium GitHub projektu EarthTool. Dołącz:
- Opis problemu
- Kroki do reprodukcji
- Wersję aplikacji
- System operacyjny
- Jeśli możliwe - przykładowy plik WD

### Czy mogę używać aplikacji do modowania gry?

Tak! Aplikacja jest idealna do:
- Ekstraktowania zasobów gry
- Modyfikowania plików
- Tworzenia własnych archiwów WD
- Pakowania modów

**Ostrzeżenie:** Zawsze twórz backup oryginalnych plików gry przed modyfikacją!

### Czy mogę dodać pliki z różnych folderów?

Tak, możesz dodać pliki z dowolnych lokalizacji. Aplikacja zachowa względną strukturę katalogów na podstawie wspólnego katalogu nadrzędnego.

### Co się stanie jeśli dodam plik o tej samej nazwie?

Archiwum WD pozwala na duplikaty nazw jeśli pliki mają różne ścieżki. Jeśli dodasz plik o identycznej nazwie i ścieżce, oba będą w archiwum (format to pozwala).

### Jak mogę zobaczyć szczegóły pojedynczego pliku?

Kliknij na wiersz w tabeli - szczegóły są widoczne w kolumnach. Dedykowany panel szczegółów jest planowany w przyszłości.

## Wsparcie

Jeśli masz pytania lub problemy:

1. Sprawdź ten przewodnik
2. Zobacz README.md dla informacji technicznych
3. Zobacz ARCHITECTURE.md dla szczegółów implementacji
4. Zgłoś issue w GitHub

## Historia zmian

### Wersja 1.0.0
- Pierwsza publiczna wersja
- Wszystkie podstawowe funkcje implementowane
- Stabilny UI i backend integration
- Kompletna dokumentacja

---

**Miłego modowania gry Earth 2150!** 🚀
