using System.IO;
using System.Text.Json;
using YoutubeConverter.Models;

namespace YoutubeConverter.Services;

public static class SettingsService
{
    private const int MaxHistory = 20;

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MediaConverter");
    private static readonly string File = Path.Combine(Dir, "settings.json");
    private static readonly string LegacyFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YoutubeConverter",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var file = System.IO.File.Exists(File) ? File : LegacyFile;
            if (!System.IO.File.Exists(file)) return new AppSettings();
            var json = System.IO.File.ReadAllText(file);
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
