using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using YoutubeConverter.Models;

namespace YoutubeConverter.Services;

public sealed class InstagramDownloadService
{
    private static readonly Regex ProgressRegex = new(@"\[download\]\s+(\d+(?:\.\d+)?)%", RegexOptions.Compiled);

    private static string YtDlpPath
    {
        get
        {
            var bundled = Path.Combine(AppContext.BaseDirectory, "yt-dlp", "yt-dlp.exe");
            return File.Exists(bundled) ? bundled : "yt-dlp";
        }
    }

    private static string FfmpegPath
    {
        get
        {
            var bundled = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe");
            return File.Exists(bundled) ? bundled : "ffmpeg";
        }
    }

    public async Task<VideoPreview> GetPreviewAsync(string url, CancellationToken ct = default)
    {
        ValidateUrl(url);

        var result = await RunYtDlpAsync(new[]
        {
            "--dump-single-json",
            "--skip-download",
            "--no-playlist",
            "--no-warnings",
            url
        }, null, ct);

        using var json = JsonDocument.Parse(result.Stdout);
        var root = json.RootElement;

        var title = GetString(root, "title") ?? "Instagram video";
        var author = GetString(root, "uploader") ?? GetString(root, "channel") ?? "Instagram";
        var thumbnail = GetString(root, "thumbnail") ?? string.Empty;
        TimeSpan? duration = null;
        if (root.TryGetProperty("duration", out var durationElement) && durationElement.ValueKind == JsonValueKind.Number)
            duration = TimeSpan.FromSeconds(durationElement.GetDouble());

        return new VideoPreview(title, author, duration, thumbnail);
    }

    public async Task<string> GetVideoTitleAsync(string url, CancellationToken ct = default)
    {
        var preview = await GetPreviewAsync(url, ct);
        return preview.Title;
    }

    public async Task DownloadAsync(
        string url,
        string outputPath,
        bool audioOnly,
        TimeSpan? trimStart = null,
        TimeSpan? trimEnd = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        ValidateUrl(url);

        var willTrim = trimStart.HasValue || trimEnd.HasValue;
        var downloadTarget = outputPath;
        string? tempPath = null;
        if (willTrim)
        {
            var dir = Path.GetDirectoryName(outputPath) ?? Path.GetTempPath();
            var ext = audioOnly ? "mp3" : "mp4";
            tempPath = Path.Combine(dir, $"_ig_{Guid.NewGuid():N}.{ext}");
            downloadTarget = tempPath;
        }

        var downloadProgress = willTrim && progress != null
            ? new Progress<double>(p => progress.Report(p * 0.9))
            : progress;

        try
        {
            var arguments = audioOnly
                ? new[]
                {
                    "--newline",
                    "--progress",
                    "--progress-delta",
                    "0.2",
                    "--no-playlist",
                    "--no-warnings",
                    "--extract-audio",
                    "--audio-format",
                    "mp3",
                    "--audio-quality",
                    "0",
                    "--ffmpeg-location",
                    FfmpegPath,
                    "-o",
                    downloadTarget,
                    url
                }
                : new[]
                {
                    "--newline",
                    "--progress",
                    "--progress-delta",
                    "0.2",
                    "--no-playlist",
                    "--no-warnings",
                    "--format",
                    "bv*+ba/b",
                    "--merge-output-format",
                    "mp4",
                    "--ffmpeg-location",
                    FfmpegPath,
                    "-o",
                    downloadTarget,
                    url
                };

            await RunYtDlpAsync(arguments, downloadProgress, ct);

            if (willTrim)
            {
                await TrimAsync(tempPath!, outputPath, trimStart, trimEnd, ct);
                progress?.Report(1.0);
            }
            else
            {
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

    private static void ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (!uri.Host.Equals("instagram.com", StringComparison.OrdinalIgnoreCase) &&
             !uri.Host.EndsWith(".instagram.com", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Paste a valid Instagram video or reel link.");
        }

        var path = uri.AbsolutePath.Trim('/').ToLowerInvariant();
        if (path.StartsWith("stories/"))
            throw new InvalidOperationException("Instagram stories are not supported yet.");

        if (!path.StartsWith("reel/") && !path.StartsWith("p/") && !path.StartsWith("tv/"))
            throw new InvalidOperationException("Only Instagram videos and reels are supported.");
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static async Task<(string Stdout, string Stderr)> RunYtDlpAsync(
        IEnumerable<string> arguments,
        IProgress<double>? progress,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = YtDlpPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start yt-dlp.");
            var stdoutTask = ReadOutputAsync(process.StandardOutput, progress, ct);
            var stderrTask = ReadOutputAsync(process.StandardError, progress, ct);

            await process.WaitForExitAsync(ct);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
                throw new InvalidOperationException(GetFriendlyError(stderr));

            return (stdout, stderr);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException("yt-dlp.exe was not found. Make sure it is included in the yt-dlp folder.", ex);
        }
    }

    private static async Task<string> ReadOutputAsync(TextReader reader, IProgress<double>? progress, CancellationToken ct)
    {
        var lines = new List<string>();
        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            lines.Add(line);

            var match = ProgressRegex.Match(line);
            if (match.Success &&
                double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            {
                progress?.Report(Math.Clamp(percent / 100.0, 0, 1));
            }
            else if (line.StartsWith("[Merger]", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("[ExtractAudio]", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("[VideoConvertor]", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report(0.95);
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string GetFriendlyError(string stderr)
    {
        if (stderr.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            stderr.Contains("private", StringComparison.OrdinalIgnoreCase))
        {
            return "Instagram could not access this video. Private or login-required posts are not supported.";
        }

        if (stderr.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase))
            return "Only Instagram videos and reels are supported.";

        return string.IsNullOrWhiteSpace(stderr) ? "Instagram download failed." : stderr.Trim();
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

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start ffmpeg.");
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
        {
            var err = await stderrTask;
            throw new InvalidOperationException($"Trim failed (ffmpeg exit {process.ExitCode}): {err}");
        }
    }
}
