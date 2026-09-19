using System.Text.Json;

namespace DeviceCfg;

/// <summary>
/// Reads the connection string out of appsettings.json, which sits next to
/// the compiled program. Kept deliberately simple (System.Text.Json, no
/// extra configuration framework) since the app only needs one value out
/// of one file.
/// </summary>
public static class ConfigLoader
{
    public static string LoadConnectionString()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(path))
        {
            throw new DeviceConfigException(
                $"Config file not found: {path}\n" +
                "Make sure appsettings.json is in the same folder as the program.");
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            throw new DeviceConfigException($"Could not read config file {path}: {ex.Message}", ex);
        }

        using JsonDocument doc = ParseJson(json);

        if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings) ||
            !connStrings.TryGetProperty("ICAS_Test", out var connStringElement))
        {
            throw new DeviceConfigException(
                "appsettings.json is missing the ConnectionStrings:ICAS_Test setting. Please check the file.");
        }

        string? connectionString = connStringElement.GetString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new DeviceConfigException("ConnectionStrings:ICAS_Test is empty. Please provide a valid connection string.");
        }

        return connectionString;
    }

    private static JsonDocument ParseJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new DeviceConfigException($"appsettings.json is not valid JSON: {ex.Message}", ex);
        }
    }
}
