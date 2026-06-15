# ClinicManager

System zarzadzania przychodnia medyczna przygotowany jako projekt zaliczeniowy w ASP.NET Core 10.

## Technologie

- ASP.NET Core 10, Blazor Server
- Entity Framework Core, Code First, SQL Server
- ASP.NET Identity
- Role: Admin, Lekarz, Rejestratorka
- QuestPDF
- NLog
- NBomber
- xUnit
- GitHub Actions
- Docker

## Struktura rozwiazania

| Projekt / katalog | Opis |
|-------------------|------|
| `ClinicManager` | Aplikacja webowa Blazor, endpointy, konfiguracja, BackgroundService, PDF. |
| `ClinicManager.Core` | Modele domenowe, DTO, interfejsy, stale i enumy. |
| `ClinicManager.Infrastructure` | EF Core `ApplicationDbContext`, migracje, mappery i serwisy biznesowe. |
| `ClinicManager.Tests` | Testy jednostkowe xUnit. |
| `PerformanceTests` | Reczny test wydajnosciowy NBomber dla endpointu API. |
| `.github/workflows` | Pipeline CI/CD GitHub Actions. |

## Wymagania lokalne

- .NET 10 SDK
- SQL Server Express albo inny lokalny SQL Server
- Opcjonalnie: Rider, Visual Studio albo SSMS do podgladu bazy

Domyslny connection string jest ustawiony pod SQL Server Express:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ClinicManager;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Jesli uzywasz innej instancji SQL Server, ustaw wlasny connection string w `ClinicManager/appsettings.Development.json`.

## Uruchomienie

1. Przygotuj SQL Server, np. lokalna instancje `SQLEXPRESS`.
2. W katalogu repozytorium wykonaj:

```powershell
dotnet restore .\ClinicManager.sln
dotnet build .\ClinicManager.sln
dotnet run --project .\ClinicManager\ClinicManager.csproj
```

3. Otworz aplikacje:

```text
http://localhost:5187
```

4. Zaloguj sie jednym z kont testowych z sekcji ponizej.

## Baza danych i migracje

Aplikacja uzywa EF Core Code First i SQL Server.

Przy starcie aplikacji `IdentitySeeder.SeedAsync(...)` wykonuje:

- `Database.MigrateAsync(...)`, czyli automatyczne zastosowanie migracji,
- utworzenie rol Identity,
- utworzenie kont testowych,
- uzupelnienie danych demonstracyjnych.

Reczne komendy przydatne przy pracy lokalnej:

```powershell
dotnet ef database update --project .\ClinicManager.Infrastructure --startup-project .\ClinicManager
dotnet ef migrations add NazwaMigracji --project .\ClinicManager.Infrastructure --startup-project .\ClinicManager
```

Glowne tabele domenowe:

- `Patients`
- `Doctors`
- `MedicalRecords`
- `Visits`
- `ProceduresPerformed`
- `Medications`
- `PrescribedMedications`
- `ClinicalNotes`

## Konta testowe

| Email | Haslo | Rola |
|-------|-------|------|
| `admin@clinic.local` | `Test123!` | Admin |
| `lekarz@clinic.local` | `Test123!` | Lekarz |
| `kardiolog@clinic.local` | `Test123!` | Lekarz |
| `rejestratorka@clinic.local` | `Test123!` | Rejestratorka |

## Role i uprawnienia

| Funkcja | Admin | Lekarz | Rejestratorka |
|---------|-------|--------|---------------|
| Zarzadzanie lekarzami | tak | nie | nie |
| CRUD pacjentow | tak | nie | tak |
| Przegladanie pacjentow | tak | tak | tak |
| Kartoteka medyczna i upload | tak | podglad | tak |
| CRUD wizyt | tak | nie | tak |
| Zmiana statusu wizyty | tak | nie | tak |
| Przegladanie wizyt | tak | tak | tak |
| Procedury, leki, notatki | tak | tak | tak |
| Raport kosztow | tak | nie | nie |
| Katalog lekow | tak | nie | tak |

## Dane demonstracyjne

Seed danych demonstracyjnych jest idempotentny, czyli kolejne uruchomienia aplikacji nie powinny tworzyc duplikatow.

Seed obejmuje:

- pacjentow testowych,
- lekarzy,
- kartoteki medyczne,
- leki,
- procedury,
- recepty,
- notatki kliniczne,
- wizyty w statusach `Planned`, `InProgress`, `Completed`, `Cancelled`.

Po uruchomieniu aplikacji mozna od razu przetestowac listy pacjentow, wizyty, kartoteke, raport kosztow, PDF wizyty i endpoint API.

## Panel admina lekarzy

Administrator ma dostep do panelu:

```text
http://localhost:5187/admin/doctors
```

Panel pozwala:

- wyswietlic liste lekarzy,
- dodac lekarza razem z kontem Identity i rola `Lekarz`,
- edytowac imie, nazwisko i specjalizacje lekarza,
- dezaktywowac konto lekarza bez usuwania rekordu `Doctor` z bazy.

Dezaktywacja jest celowo miekka: historia wizyt, notatki i raporty nadal moga wskazywac lekarza, ale konto nie moze sie logowac.

## Testy

Testy jednostkowe sa w projekcie `ClinicManager.Tests`.

Uruchomienie:

```powershell
dotnet test .\ClinicManager.sln --no-restore
```

Zakres testow obejmuje m.in.:

- wyszukiwanie pacjentow,
- soft delete pacjentow,
- tworzenie i edycje wizyt,
- walidacje statusow wizyt,
- procedury, leki i notatki,
- raport kosztow,
- generowanie PDF,
- indeksy EF Core,
- endpoint API.

## Raporty PDF i artefakty

W repozytorium sa przygotowane raporty wymagane do oddania:

| Plik | Opis |
|------|------|
| `raport-indeksy.pdf` | Opis indeksow EF Core i ich uzasadnienie. |
| `raport-sql-profiler.pdf` | Opis logowania zapytan SQL / profilowania EF Core. |
| `nbomber-report.pdf` | Opis scenariusza NBomber i metryk testu wydajnosciowego. |

Aplikacja generuje tez pliki runtime:

- `ClinicManager/reports/upcoming_visits.pdf` w trybie testowym BackgroundService,
- `nbomber-report/` po recznym uruchomieniu NBomber,
- `logs/errors.log` dla logow NLog.

Katalogi runtime sa lokalnymi plikami i nie musza byc commitowane.

## Indeksy i profiler SQL

W `ApplicationDbContext` skonfigurowano m.in.:

- unikalny indeks `Patients.Pesel`,
- unikalny indeks `Doctors.UserId`,
- indeks zlozony `Visits.DoctorId, Visits.ScheduledAt`.

Logowanie SQL jest wlaczone przez:

```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

Dzieki temu zapytania EF Core widac w logach aplikacji i mozna pokazac ich dzialanie przy prezentacji.

## NLog

Projekt uzywa `NLog.Web.AspNetCore` i pliku `ClinicManager/nlog.config`.

Najwazniejsze ustawienia:

- bledy trafiaja do `logs/errors.log`,
- logi techniczne sa wypisywane na konsole,
- kod korzysta ze standardowego `ILogger<T>`,
- NLog jest podpiety w `Program.cs` przez `builder.Host.UseNLog()`.

Przyklad uzycia znajduje sie w `CostReportPdfService`: blad generowania PDF raportu kosztow jest logowany przez `logger.LogError(...)`.

## BackgroundService raportu wizyt

`UpcomingVisitsReportBackgroundService` generuje raport jutrzejszych wizyt.

Domyslnie:

- `Enabled=true`,
- `RunOnStartup=true`,
- `IntervalHours=24`,
- `Smtp.Enabled=false`.

W trybie testowym PDF jest zapisywany jako:

```text
ClinicManager/reports/upcoming_visits.pdf
```

Po ustawieniu `UpcomingVisitsReport:Smtp:Enabled=true` oraz danych SMTP usluga wysylalby e-mail z zalacznikiem `upcoming_visits.pdf`.

## Endpoint API i OpenAPI

Endpoint:

```text
GET /api/visits/active
```

Zwraca aktywne wizyty (`Planned`, `InProgress`) razem z danymi pacjenta i lekarza.

OpenAPI:

```text
http://localhost:5187/openapi/v1.json
```

W pliku OpenAPI powinien byc widoczny endpoint `/api/visits/active`.

## NBomber

Projekt `PerformanceTests` zawiera scenariusz testu wydajnosciowego endpointu `GET /api/visits/active`.

Uruchom aplikacje:

```powershell
dotnet run --project .\ClinicManager\ClinicManager.csproj
```

W drugim terminalu uruchom test:

```powershell
dotnet run --project .\PerformanceTests\PerformanceTests.csproj -- http://localhost:5187
```

Scenariusz:

- 50 requestow na sekunde,
- 2 sekundy,
- okolo 100 requestow lacznie.

W recznym tescie wykonanym lokalnie endpoint odpowiedzial:

- `ok count: 100`,
- `fail count: 0`,
- `RPS: 50`,
- status HTTP `200` dla 100 requestow,
- srednia latencja ok. `8.5 ms`,
- p95 ok. `25.84 ms`.

## CI/CD

Pipeline znajduje sie w:

```text
.github/workflows/dotnet-ci.yml
```

Uruchamia sie dla `push` i `pull_request`.

Kroki:

1. Checkout repozytorium.
2. Instalacja .NET 10.
3. `dotnet restore ClinicManager.sln`.
4. `dotnet build ClinicManager.sln --configuration Release --no-restore`.
5. `dotnet test ClinicManager.sln --configuration Release --no-build --verbosity normal`.
6. `docker build --file ClinicManager/Dockerfile --tag clinicmanager:ci .`.

## Docker

Obraz aplikacji jest budowany z:

```text
ClinicManager/Dockerfile
```

Sprawdzenie lokalne:

```powershell
docker build --file .\ClinicManager\Dockerfile --tag clinicmanager:local .
```

## Trello

Plan prac jest opisany w `ClinicManager/TrelloPlan.md`. Plik zawiera karty US-01 do US-23, checklisty, DoD, etapy prac, konwencje branchy i opis minimalnego planu na zaliczenie.



