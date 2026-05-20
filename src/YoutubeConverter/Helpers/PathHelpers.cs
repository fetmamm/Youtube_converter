using System.IO;

namespace YoutubeConverter.Helpers;

public static class PathHelpers
{
    public static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    public static string FormatDuration(TimeSpan duration) =>
        duration.ToString(duration.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss");
}
