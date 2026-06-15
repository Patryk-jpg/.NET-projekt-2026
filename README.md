# ClinicManager

System zarządzania przychodnią medyczną — projekt zaliczeniowy ASP.NET Core 10.

## Technologie

- ASP.NET Core 10 (Blazor Server)
- Entity Framework Core — Code First, SQL Server
- ASP.NET Identity (role: Admin, Lekarz, Rejestratorka)
- Mapperly (mapowanie DTO ↔ encje)
- Bootstrap 5

## Uruchomienie lokalne

### Wymagania

- .NET 10 SDK
- SQL Server (lokalny lub Docker)

### Kroki

1. Sklonuj repozytorium

2. (Opcjonalne, jest też domyślnie ustawione pod SQLEXPRESS) Ustaw connection string w `ClinicManager/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=ClinicManager;Trusted_Connection=True;TrustServerCertificate=True"
     }
   }
   ```
3. Uruchom aplikację:
   ```
   cd ClinicManager
   dotnet run
   ```
4. Otwórz http://localhost:5187

Baza danych i migracje są aplikowane automatycznie przy starcie (`MigrateAsync`).

## Konta testowe

| Email | Hasło | Rola |
|-------|-------|------|
| admin@clinic.local | Test123! | Admin |
| lekarz@clinic.local | Test123! | Lekarz |
| rejestratorka@clinic.local | Test123! | Rejestratorka |

## Role i uprawnienia

| Funkcja | Admin | Lekarz | Rejestratorka |
|---------|-------|--------|---------------|
| CRUD pacjentow | tak | nie | tak |
| Przegladanie pacjentow | tak | tak | tak |
| Kartoteka + upload | tak | podglad | tak |
| CRUD wizyt | tak | nie | tak |
| Zmiana statusu wizyty | tak | nie | tak |
| Przegladanie wizyt | tak | tak | tak |
| Katalog lekow | tak | nie | tak |

## CI/CD — GitHub Actions

Plik: `.github/workflows/dotnet-ci.yml`

Pipeline uruchamia się automatycznie przy każdym **push** i **pull request** na dowolną gałąź.

### Kroki pipeline

1. `checkout` - pobiera kod z repozytorium
2. `setup .NET 10` - instaluje SDK
3. `dotnet restore` - pobiera paczki NuGet
4. `dotnet build` - kompiluje solution w trybie Release
5. `dotnet test` - uruchamia testy jednostkowe (xUnit)
6. `docker build` - sprawdza budowe obrazu z `ClinicManager/Dockerfile`

Jeśli którykolwiek krok się nie powiedzie, pipeline jest oznaczony jako failed i blokuje merge PR-a.

### Konfiguracja

```yaml
on:
  push:
  pull_request:

jobs:
  build-and-test:
    runs-on: ubuntu-latest
  docker-build:
    runs-on: ubuntu-latest
    needs: build-and-test
```

## NLog

Projekt uzywa `NLog.Web.AspNetCore` i konfiguracji `ClinicManager/nlog.config`.

Najwazniejsze ustawienia:

- bledy aplikacji trafiaja do `logs/errors.log`,
- logi techniczne sa wypisywane takze na konsole,
- katalog `logs/` jest ignorowany przez git,
- w kodzie uzywamy standardowego `ILogger<T>`, a NLog jest providerem podlaczonym w `Program.cs`.

Przyklad uzycia znajduje sie w `CostReportPdfService`: blad generowania PDF raportu kosztow jest logowany przez `logger.LogError(...)`, bez dopisywania danych wrazliwych pacjenta do komunikatu.

Pipeline działa na `ubuntu-latest` — środowisko czyste przy każdym uruchomieniu.

## BackgroundService raportu wizyt

`UpcomingVisitsReportBackgroundService` generuje raport jutrzejszych wizyt. Domyslnie usluga uruchamia sie przy starcie aplikacji, a potem co 24 godziny.

Konfiguracja znajduje sie w sekcji `UpcomingVisitsReport` w `ClinicManager/appsettings.json`. W trybie testowym (`Smtp.Enabled=false`) PDF jest zapisywany lokalnie jako `reports/upcoming_visits.pdf`. Po ustawieniu `Smtp.Enabled=true` oraz danych serwera SMTP usluga wysyla e-mail do administratora z zalacznikiem PDF.

## Endpoint API i test NBomber

Endpoint `GET /api/visits/active` zwraca aktywne wizyty (`Planned`, `InProgress`) razem z danymi pacjenta i lekarza. OpenAPI jest dostepne pod adresem `/openapi/v1.json`.

Test wydajnosciowy znajduje sie w `PerformanceTests`. Najpierw uruchom aplikacje, potem w drugim terminalu wykonaj `dotnet run --project .\PerformanceTests\PerformanceTests.csproj -- http://localhost:5187`. Scenariusz wysyla okolo 100 zapytan: 50 requestow na sekunde przez 2 sekundy, a raport NBomber zapisuje w katalogu `nbomber-report`.
