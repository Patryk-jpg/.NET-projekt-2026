using NBomber.Contracts;
using NBomber.CSharp;

var baseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5187";
var endpoint = $"{baseUrl}/api/visits/active";
using var httpClient = new HttpClient();

var scenario = Scenario.Create("active_visits_endpoint", async context =>
{
    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
    request.Headers.Accept.ParseAdd("application/json");

    var response = await httpClient.SendAsync(request, context.ScenarioCancellationToken);
    return response.IsSuccessStatusCode
        ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
        : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
})
.WithoutWarmUp()
.WithLoadSimulations(
    Simulation.Inject(
        rate: 50,
        interval: TimeSpan.FromSeconds(1),
        during: TimeSpan.FromSeconds(2)));

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder("nbomber-report")
    .WithReportFileName("active-visits")
    .Run();
