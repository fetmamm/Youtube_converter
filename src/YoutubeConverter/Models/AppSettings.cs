namespace YoutubeConverter.Models;

public sealed class AppSettings
{
    public string? LastFolder { get; set; }
    public string LastQuality { get; set; } = "Best";
    public List<HistoryEntry> History { get; set; } = new();
}
