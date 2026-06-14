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

Pipeline działa na `ubuntu-latest` — środowisko czyste przy każdym uruchomieniu.
