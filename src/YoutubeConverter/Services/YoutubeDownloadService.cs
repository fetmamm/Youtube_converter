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
        var request = new ConversionRequestBuilder(outputPath)
            .SetFFmpegPath(FfmpegPath)
            .SetContainer(container)
            .SetPreset(ConversionPreset.Medium)
            .Build();

        var captions = Array.Empty<ClosedCaptionTrackInfo>();
        await _yt.Videos.DownloadAsync(streams, captions, request, progress, ct);
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
