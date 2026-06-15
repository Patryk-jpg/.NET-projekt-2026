# PerformanceTests

Test NBomber dla endpointu `GET /api/visits/active`.

Uruchom aplikacje webowa, a potem w drugim terminalu:

```powershell
dotnet run --project .\PerformanceTests\PerformanceTests.csproj -- http://localhost:5187
```

Scenariusz wysyla okolo 100 zapytan: 50 requestow na sekunde przez 2 sekundy. Wyniki NBomber zapisuje do katalogu `nbomber-report`.
