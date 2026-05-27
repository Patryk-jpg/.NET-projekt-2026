using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WeatherInformation;

var config = JsonNode.Parse(File.ReadAllText("appsettings.json"))!;

var ollamaBaseUrl = config["Ollama"]!["BaseUrl"]!.GetValue<string>();
var ollamaModel = config["Ollama"]!["Model"]!.GetValue<string>();

var mcpCommand = config["McpServer"]!["Command"]!.GetValue<string>();

var mcpArgs = config["McpServer"]!["Args"]!
    .AsArray()
    .Select(x => x!.GetValue<string>())
    .ToArray();

using var mcp = new McpStdioClient(mcpCommand, mcpArgs);

await mcp.StartAsync();
await mcp.InitializeAsync();

var mcpTools = await mcp.ListToolsAsync();

Console.WriteLine("Connected MCP tools:");

foreach (var tool in mcpTools)
{
    Console.WriteLine($"- {tool["name"]}");
}

var messages = new JsonArray
{
    new JsonObject
    {
        ["role"] = "system",
        ["content"] = "You are a helpful assistant. Use tools when weather data is needed."
    }
};

using var http = new HttpClient
{
    BaseAddress = new Uri(ollamaBaseUrl)
};

while (true)
{
    Console.Write("\nYou> ");

    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    messages.Add(new JsonObject
    {
        ["role"] = "user",
        ["content"] = input
    });

    while (true)
    {
        var response = await CallOllamaAsync(http, ollamaModel, messages, mcpTools);

        var message = response["message"]!.AsObject();

        messages.Add(message.DeepClone());

        var toolCalls = message["tool_calls"]?.AsArray();

        if (toolCalls is null || toolCalls.Count == 0)
        {
            Console.WriteLine($"Assistant> {message["content"]?.GetValue<string>()}");
            break;
        }

        foreach (var toolCall in toolCalls)
        {
            var function = toolCall!["function"]!.AsObject();

            var toolName = function["name"]!.GetValue<string>();

            var arguments = function["arguments"]!.AsObject();

            Console.WriteLine($"[tool call] {toolName} {arguments}");

            var toolResult = await mcp.CallToolAsync(toolName, arguments);

            var toolText = ExtractMcpText(toolResult);

            messages.Add(new JsonObject
            {
                ["role"] = "tool",
                ["tool_name"] = toolName,
                ["content"] = toolText
            });
        }
    }
}

static async Task<JsonObject> CallOllamaAsync(
    HttpClient http,
    string model,
    JsonArray messages,
    JsonArray tools)
{
    var payload = new JsonObject
    {
        ["model"] = model,
        ["messages"] = messages.DeepClone(),
        ["tools"] = ConvertMcpToolsToOllamaTools(tools),
        ["stream"] = false
    };

    var json = payload.ToJsonString(new JsonSerializerOptions
    {
        WriteIndented = false
    });

    using var content = new StringContent(json, Encoding.UTF8, "application/json");

    using var response = await http.PostAsync("/api/chat", content);

    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine(body);
        throw new Exception($"Ollama error {(int)response.StatusCode}");
    }

    return JsonNode.Parse(body)!.AsObject();
}

static JsonArray ConvertMcpToolsToOllamaTools(JsonArray mcpTools)
{
    var result = new JsonArray();

    foreach (var tool in mcpTools)
    {
        var t = tool!.AsObject();

        result.Add(new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = t["name"]!.GetValue<string>(),
                ["description"] = t["description"]?.GetValue<string>() ?? "",
                ["parameters"] = t["inputSchema"]?.DeepClone()
                    ?? new JsonObject { ["type"] = "object" }
            }
        });
    }

    return result;
}

static string ExtractMcpText(JsonObject toolResult)
{
    var content = toolResult["content"]?.AsArray();

    if (content is null)
        return toolResult.ToJsonString();

    var parts = new List<string>();

    foreach (var item in content)
    {
        var obj = item!.AsObject();

        if (obj["type"]?.GetValue<string>() == "text")
            parts.Add(obj["text"]?.GetValue<string>() ?? "");
    }

    return string.Join("\n", parts);
}