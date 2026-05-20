using System.Diagnostics;
using System.Globalization;
using System.IO;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;

namespace YoutubeConverter.Services;

public sealed record VideoPreview(string Title, string Author, TimeSpan? Duration, string ThumbnailUrl);

public sealed class YoutubeDownloadService
{
    private readonly YoutubeClient _yt = new();

    public static IReadOnlyList<string> Qualities { get; } = new[] { "Bästa", "1080p", "720p", "480p", "360p" };

    private static string FfmpegPath
    {
        get
        {
            var baseDir = AppContext.BaseDirectory;
            var bundled = Path.Combine(baseDir, "ffmpeg", "ffmpeg.exe");
            return File.Exists(bundled) ? bundled : "ffmpeg";
        }
    }

    public async Task<VideoPreview> GetPreviewAsync(string url, CancellationToken ct = default)
    {
        var video = await _yt.Videos.GetAsync(url, ct);
        var thumb = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url ?? string.Empty;
        return new VideoPreview(video.Title, video.Author.ChannelTitle, video.Duration, thumb);
    }

    public async Task<string> GetVideoTitleAsync(string url, CancellationToken ct = default)
    {
        var video = await _yt.Videos.GetAsync(url, ct);
        return video.Title;
    }

    public async Task DownloadAsync(
        string url,
        string outputPath,
        bool audioOnly,
        string quality,
        TimeSpan? trimStart = null,
        TimeSpan? trimEnd = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var manifest = await _yt.Videos.Streams.GetManifestAsync(url, ct);

        IReadOnlyList<IStreamInfo> streams;
        if (audioOnly)
        {
            streams = new IStreamInfo[] { manifest.GetAudioStreams().GetWithHighestBitrate() };
        }
        else
        {
            var video = PickVideoStream(manifest, quality);
            var audio = manifest.GetAudioStreams().GetWithHighestBitrate();
            streams = new IStreamInfo[] { video, audio };
        }

        var container = audioOnly ? "mp3" : "mp4";
        var captions = Array.Empty<ClosedCaptionTrackInfo>();
        var willTrim = trimStart.HasValue || trimEnd.HasValue;

        string downloadTarget = outputPath;
        string? tempPath = null;
        if (willTrim)
        {
            var dir = Path.GetDirectoryName(outputPath) ?? Path.GetTempPath();
            tempPath = Path.Combine(dir, $"_yt_{Guid.NewGuid():N}.{container}");
            downloadTarget = tempPath;
        }

        var request = new ConversionRequestBuilder(downloadTarget)
            .SetFFmpegPath(FfmpegPath)
            .SetContainer(container)
            .SetPreset(ConversionPreset.Medium)
            .Build();

        IProgress<double>? downloadProgress = willTrim && progress != null
            ? new Progress<double>(p => progress.Report(p * 0.9))
            : progress;

        try
        {
            await _yt.Videos.DownloadAsync(streams, captions, request, downloadProgress, ct);

            if (willTrim)
            {
                await TrimAsync(tempPath!, outputPath, trimStart, trimEnd, ct);
                progress?.Report(1.0);
            }
        }
        finally
        {
            if (tempPath != null && File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }

    private static async Task TrimAsync(string input, string output, TimeSpan? start, TimeSpan? end, CancellationToken ct)
    {
        var args = new List<string>();
        if (start.HasValue)
            args.Add($"-ss {start.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
        args.Add($"-i \"{input}\"");
        if (end.HasValue)
        {
            var duration = end.Value - (start ?? TimeSpan.Zero);
            if (duration.TotalSeconds > 0)
                args.Add($"-t {duration.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
        }
        args.Add("-c copy");
        args.Add("-y");
        args.Add($"\"{output}\"");

        var psi = new ProcessStartInfo
        {
            FileName = FfmpegPath,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Kunde inte starta ffmpeg.");
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
        {
            var err = await stderrTask;
            throw new InvalidOperationException($"Trim misslyckades (ffmpeg exit {process.ExitCode}): {err}");
        }
    }

    public static TimeSpan? ParseTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var parts = s.Trim().Split(':');
        try
        {
            return parts.Length switch
            {
                1 => TimeSpan.FromSeconds(double.Parse(parts[0], CultureInfo.InvariantCulture)),
                2 => new TimeSpan(0, int.Parse(parts[0]), int.Parse(parts[1])),
                3 => new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2])),
                _ => null
            };
        }
        catch { return null; }
    }

    private static IVideoStreamInfo PickVideoStream(StreamManifest manifest, string quality)
    {
        var all = manifest.GetVideoStreams().OrderByDescending(s => s.VideoQuality.MaxHeight).ToList();
        if (quality == "Bästa" || string.IsNullOrEmpty(quality))
            return manifest.GetVideoStreams().GetWithHighestVideoQuality();

        if (int.TryParse(quality.TrimEnd('p'), out var targetHeight))
        {
            var match = all.Where(s => s.VideoQuality.MaxHeight <= targetHeight)
                           .OrderByDescending(s => s.VideoQuality.MaxHeight)
                           .FirstOrDefault();
            if (match != null) return match;
        }

        return manifest.GetVideoStreams().GetWithHighestVideoQuality();
    }
}
