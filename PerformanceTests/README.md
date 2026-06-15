# PerformanceTests

Test NBomber dla endpointu `GET /api/visits/active`.

Uruchom aplikacje webowa, a potem w drugim terminalu:

```powershell
dotnet run --project .\PerformanceTests\PerformanceTests.csproj -- http://localhost:5187
```

Scenariusz uruchamia 50 stalych wirtualnych uzytkownikow i wykonuje lacznie 100 zapytan HTTP. Wyniki NBomber zapisuje do katalogu `nbomber-report`.
