namespace YoutubeConverter.Models;

public sealed record VideoPreview(string Title, string Author, TimeSpan? Duration, string ThumbnailUrl);
