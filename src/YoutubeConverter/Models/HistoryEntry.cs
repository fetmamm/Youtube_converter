namespace YoutubeConverter.Models;

public sealed class HistoryEntry
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Format { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
