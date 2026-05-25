namespace YoutubeConverter.Models;

public sealed class HistoryEntry
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Format { get; set; } = "";
    public DownloadPlatform? Platform { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public DownloadPlatform DisplayPlatform
    {
        get
        {
            if (Platform.HasValue) return Platform.Value;
            return Url.Contains("instagram.com", StringComparison.OrdinalIgnoreCase)
                ? DownloadPlatform.Instagram
                : DownloadPlatform.Youtube;
        }
    }

    public string PlatformIcon => DisplayPlatform == DownloadPlatform.Instagram ? "IG" : "YT";
    public string PlatformName => DisplayPlatform == DownloadPlatform.Instagram ? "Instagram" : "YouTube";
}
