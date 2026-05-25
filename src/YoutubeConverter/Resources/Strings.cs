namespace YoutubeConverter.Resources;

internal static class Strings
{
    // Initial status
    public const string ReadyPrompt = "Ready. Paste or drop a YouTube link.";
    public const string InstagramReadyPrompt = "Ready. Paste or drop an Instagram video or reel link.";
    public const string YoutubeUrlPlaceholder = "Paste YouTube link here...";
    public const string InstagramUrlPlaceholder = "Paste Instagram video or reel link here...";

    // Analyze flow
    public const string AnalyzingVideo = "Analyzing video…";
    public const string ReadyToExport = "Ready to export.";
    public const string CouldNotReadVideoFmt = "Could not read video: {0}";

    // Export flow
    public const string SaveAsDialogTitle = "Save as";
    public const string Mp3FilterText = "MP3 audio (*.mp3)|*.mp3";
    public const string Mp4FilterText = "MP4 video (*.mp4)|*.mp4";
    public const string Cancelled = "Cancelled.";
    public const string CancelledByUser = "Cancelled by user.";
    public const string TrimEndAfterStart = "Trim end must be after start.";
    public const string DownloadingAndTrimming = "Downloading and trimming…";
    public const string Downloading = "Downloading…";
    public const string DownloadingProgressFmt = "Downloading… {0:0}%";
    public const string DoneSavedFmt = "Done! Saved: {0}";
    public const string ErrorFmt = "Error: {0}";

    // History
    public const string FileNoLongerExists = "File no longer exists on disk.";

    // Updates popup
    public const string LatestVersionUnknown = "Unknown (click to check)";
    public const string LatestVersionUnavailable = "Could not check";
    public const string NewVersionAvailableFmt = "New version {0} available";
    public const string LatestVersionFmt = "{0} (latest)";

    // Default quality fallback
    public const string DefaultQuality = "Best";
}
