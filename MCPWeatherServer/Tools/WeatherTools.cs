public class WeatherTools
{
    [McpServerTool]
    [Description("Returns temperature in Celsius for a given place and date.")]
    public string GetTemperature(string location, string date)
    {
        var seed = Math.Abs(HashCode.Combine(location.ToLowerInvariant(), date));
        var temp = (seed % 30) - 5;
        return $"{temp}°C";
    }

    [McpServerTool]
    [Description("Returns information whether it will rain for a given place and date.")]
    public string WillItRain(string location, string date)
    {
        var seed = Math.Abs(HashCode.Combine("rain", location.ToLowerInvariant(), date));
        var rain = seed % 2 == 0;

        return rain
            ? $"Yes, rain is expected in {location} on {date}."
            : $"No, rain is not expected in {location} on {date}.";
    }
}