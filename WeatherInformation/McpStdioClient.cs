using System.Diagnostics;
using System.Text.Json.Nodes;

namespace WeatherInformation;

public sealed class McpStdioClient : IDisposable
{
    private readonly string _command;
    private readonly string[] _args;

    private Process? _process;

    private int _id = 1;

    public McpStdioClient(string command, string[] args)
    {
        _command = command;
        _args = args;
    }

    public Task StartAsync()
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _command,
                Arguments = string.Join(" ", _args),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _process.Start();

        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        await SendRequestAsync("initialize", new JsonObject
        {
            ["protocolVersion"] = "2024-11-05",
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject
            {
                ["name"] = "weather-console-client",
                ["version"] = "1.0.0"
            }
        });

        await SendNotificationAsync("notifications/initialized", new JsonObject());
    }

    public async Task<JsonArray> ListToolsAsync()
    {
        var result = await SendRequestAsync("tools/list", new JsonObject());

        return result["tools"]!.AsArray();
    }

    public async Task<JsonObject> CallToolAsync(string name, JsonObject arguments)
    {
        return await SendRequestAsync("tools/call", new JsonObject
        {
            ["name"] = name,
            ["arguments"] = arguments.DeepClone()
        });
    }

    private async Task<JsonObject> SendRequestAsync(string method, JsonObject parameters)
    {
        var id = _id++;

        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = parameters
        };

        await WriteLineAsync(request.ToJsonString());

        while (true)
        {
            var line = await _process!.StandardOutput.ReadLineAsync();

            var response = JsonNode.Parse(line!)!.AsObject();

            if (response["id"]?.GetValue<int>() != id)
                continue;

            return response["result"]!.AsObject();
        }
    }

    private async Task SendNotificationAsync(string method, JsonObject parameters)
    {
        var notification = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = parameters
        };

        await WriteLineAsync(notification.ToJsonString());
    }

    private async Task WriteLineAsync(string line)
    {
        await _process!.StandardInput.WriteLineAsync(line);

        await _process.StandardInput.FlushAsync();
    }

    public void Dispose()
    {
        if (_process is { HasExited: false })
            _process.Kill();

        _process?.Dispose();
    }
}