using System.Text.Json;

namespace ProcessGuard;

public static class Storage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string AppDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".process_guard");

    public static string ConfigFile => Path.Combine(AppDir, "configs.json");

    public static AppState Load()
    {
        Directory.CreateDirectory(AppDir);
        if (!File.Exists(ConfigFile))
        {
            var fresh = new AppState();
            Save(fresh);
            return fresh;
        }
        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<AppState>(json, Options) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public static void Save(AppState state)
    {
        Directory.CreateDirectory(AppDir);
        var json = JsonSerializer.Serialize(state, Options);
        File.WriteAllText(ConfigFile, json);
    }

    public static void Export(AppState state, string path)
    {
        var json = JsonSerializer.Serialize(state, Options);
        File.WriteAllText(path, json);
    }

    public static AppState? Import(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppState>(json, Options);
    }
}
