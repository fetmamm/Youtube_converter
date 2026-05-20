using System.IO;
using System.Text.Json;

namespace YoutubeConverter.Services;

public sealed class HistoryEntry
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Format { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public sealed class AppSettings
{
    public string? LastFolder { get; set; }
    public string LastQuality { get; set; } = "Bästa";
    public List<HistoryEntry> History { get; set; } = new();
}

public static class SettingsService
{
    private const int MaxHistory = 20;

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YoutubeConverter");
    private static readonly string File = Path.Combine(Dir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!System.IO.File.Exists(File)) return new AppSettings();
            var json = System.IO.File.ReadAllText(File);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            if (settings.History.Count > MaxHistory)
                settings.History = settings.History.Take(MaxHistory).ToList();
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(File, json);
        }
        catch { }
    }
}
