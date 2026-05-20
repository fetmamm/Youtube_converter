using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YoutubeConverter.Services;

namespace YoutubeConverter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly YoutubeDownloadService _service = new();
    private readonly AppSettings _settings = SettingsService.Load();
    private CancellationTokenSource? _previewCts;
    private CancellationTokenSource? _downloadCts;
    private string? _lastSavedFile;

    public ObservableCollection<string> Qualities { get; } = new(YoutubeDownloadService.Qualities);
    public ObservableCollection<HistoryEntry> History { get; } = new();

    public string AppVersion => $"v{UpdateService.CurrentVersion.ToString(3)}";
    public string GitHubRepoUrl => $"https://github.com/{UpdateService.GitHubOwner}/{UpdateService.GitHubRepo}";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    private bool _isMp3 = true;

    [ObservableProperty]
    private bool _isMp4;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusText = "Ready. Paste or drop a YouTube link.";

    [ObservableProperty]
    private string? _previewTitle;

    [ObservableProperty]
    private string? _previewAuthor;

    [ObservableProperty]
    private string? _previewDuration;

    [ObservableProperty]
    private string? _previewThumbnail;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private bool _hasPreview;

    [ObservableProperty]
    private bool _trimEnabled;

    [ObservableProperty]
    private string _trimStart = "0:00";

    [ObservableProperty]
    private string _trimEnd = string.Empty;

    [ObservableProperty]
    private string _selectedQuality = "Best";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowQualityPicker))]
    private bool _isQualityVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowOpenFolder))]
    private bool _hasLastFile;

    [ObservableProperty]
    private string? _updateAvailableText;

    [ObservableProperty]
    private string? _updateUrl;

    [ObservableProperty]
    private string _latestVersionText = "Unknown (click to check)";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckUpdatesCommand))]
    private bool _isCheckingUpdates;

    [ObservableProperty]
    private bool _updateAvailable;

    public bool ShowQualityPicker => IsMp4;
    public bool CanShowOpenFolder => HasLastFile && !IsBusy;

    public MainViewModel()
    {
        SelectedQuality = YoutubeDownloadService.Qualities.Contains(_settings.LastQuality)
            ? _settings.LastQuality
            : "Best";
        foreach (var h in _settings.History) History.Add(h);
        HasHistory = History.Count > 0;
        _ = CheckForUpdatesAsync();
    }

    [ObservableProperty]
    private bool _hasHistory;

    partial void OnUrlChanged(string value)
    {
        HasPreview = false;
        PreviewTitle = PreviewAuthor = PreviewDuration = PreviewThumbnail = null;
        AnalyzeCommand.NotifyCanExecuteChanged();
    }
    partial void OnSelectedQualityChanged(string value)
    {
        _settings.LastQuality = value;
        SettingsService.Save(_settings);
    }

    partial void OnIsMp3Changed(bool value)
    {
        if (value && IsMp4) IsMp4 = false;
        if (!value && !IsMp4) IsMp4 = true;
        OnPropertyChanged(nameof(ShowQualityPicker));
    }

    partial void OnIsMp4Changed(bool value)
    {
        if (value && IsMp3) IsMp3 = false;
        OnPropertyChanged(nameof(ShowQualityPicker));
    }

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        var url = Url.Trim();
        if (string.IsNullOrWhiteSpace(url)) return;

        _previewCts?.Cancel();
        var cts = _previewCts = new CancellationTokenSource();

        HasPreview = false;
        PreviewTitle = PreviewAuthor = PreviewDuration = PreviewThumbnail = null;
        StatusText = "Analyzing video…";

        try
        {
            var preview = await _service.GetPreviewAsync(url, cts.Token);
            if (cts.IsCancellationRequested) return;

            PreviewTitle = preview.Title;
            PreviewAuthor = preview.Author;
            PreviewDuration = preview.Duration is { } d
                ? d.ToString(d.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss")
                : null;
            PreviewThumbnail = preview.ThumbnailUrl;
            HasPreview = true;
            if (preview.Duration is { } dur)
                TrimEnd = dur.ToString(dur.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss");
            StatusText = "Ready to export.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText = $"Could not read video: {ex.Message}";
        }
    }

    private bool CanAnalyze() => !IsBusy && !string.IsNullOrWhiteSpace(Url);

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        var url = Url.Trim();
        var isAudio = IsMp3;
        var ext = isAudio ? "mp3" : "mp4";

        _downloadCts = new CancellationTokenSource();
        IsBusy = true;
        Progress = 0;
        HasLastFile = false;

        try
        {
            var title = PreviewTitle ?? await _service.GetVideoTitleAsync(url, _downloadCts.Token);
            var safeName = SanitizeFileName(title);

            var dialog = new SaveFileDialog
            {
                FileName = safeName,
                DefaultExt = ext,
                AddExtension = true,
                Filter = isAudio ? "MP3 audio (*.mp3)|*.mp3" : "MP4 video (*.mp4)|*.mp4",
                Title = "Save as"
            };
            if (!string.IsNullOrEmpty(_settings.LastFolder) && Directory.Exists(_settings.LastFolder))
                dialog.InitialDirectory = _settings.LastFolder;

            if (dialog.ShowDialog() != true)
            {
                StatusText = "Cancelled.";
                return;
            }

            _settings.LastFolder = Path.GetDirectoryName(dialog.FileName);
            SettingsService.Save(_settings);

            var progress = new Progress<double>(p =>
            {
                Progress = p;
                StatusText = $"Downloading… {p * 100:0}%";
            });

            TimeSpan? trimStart = null, trimEnd = null;
            if (TrimEnabled)
            {
                trimStart = YoutubeDownloadService.ParseTime(TrimStart);
                trimEnd = YoutubeDownloadService.ParseTime(TrimEnd);
                if (trimStart.HasValue && trimEnd.HasValue && trimEnd <= trimStart)
                {
                    StatusText = "Trim end must be after start.";
                    return;
                }
            }

            StatusText = TrimEnabled ? "Downloading and trimming…" : "Downloading…";
            await _service.DownloadAsync(url, dialog.FileName, isAudio, SelectedQuality, trimStart, trimEnd, progress, _downloadCts.Token);

            Progress = 1;
            _lastSavedFile = dialog.FileName;
            HasLastFile = true;
            StatusText = $"Done! Saved: {Path.GetFileName(dialog.FileName)}";

            var entry = new HistoryEntry
            {
                Title = title,
                Url = url,
                FilePath = dialog.FileName,
                Format = ext.ToUpperInvariant(),
                Timestamp = DateTime.Now
            };
            History.Insert(0, entry);
            _settings.History.Insert(0, entry);
            HasHistory = true;
            SettingsService.Save(_settings);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled by user.";
            Progress = 0;
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    private bool CanExport() => !IsBusy && HasPreview;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _downloadCts?.Cancel();
    }

    private bool CanCancel() => IsBusy;

    [RelayCommand]
    private void OpenLastFolder()
    {
        if (!string.IsNullOrEmpty(_lastSavedFile) && File.Exists(_lastSavedFile))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_lastSavedFile}\"") { UseShellExecute = true });
            return;
        }

        var folder = !string.IsNullOrEmpty(_settings.LastFolder) && Directory.Exists(_settings.LastFolder)
            ? _settings.LastFolder
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";

        if (!Directory.Exists(folder))
            folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    public void HandleDroppedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Url = text.Trim();
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    private async Task CheckForUpdatesAsync()
    {
        IsCheckingUpdates = true;
        try
        {
            var info = await UpdateService.CheckForUpdateAsync();
            if (info != null)
            {
                UpdateAvailable = true;
                UpdateAvailableText = $"New version {info.LatestVersion} available";
                UpdateUrl = info.ReleaseUrl;
                LatestVersionText = $"v{info.LatestVersion}";
            }
            else
            {
                UpdateAvailable = false;
                LatestVersionText = $"{AppVersion} (latest)";
            }
        }
        catch
        {
            LatestVersionText = "Could not check";
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCheckUpdates))]
    private Task CheckUpdates() => CheckForUpdatesAsync();
    private bool CanCheckUpdates() => !IsCheckingUpdates;

    [RelayCommand]
    private void OpenUpdate()
    {
        if (!string.IsNullOrEmpty(UpdateUrl))
            Process.Start(new ProcessStartInfo(UpdateUrl) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenRepo()
    {
        Process.Start(new ProcessStartInfo(GitHubRepoUrl) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenHistoryItem(HistoryEntry? entry)
    {
        if (entry == null) return;
        if (File.Exists(entry.FilePath))
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{entry.FilePath}\"") { UseShellExecute = true });
        else
            StatusText = "File no longer exists on disk.";
    }

    [RelayCommand]
    private void RemoveHistoryItem(HistoryEntry? entry)
    {
        if (entry == null) return;
        History.Remove(entry);
        _settings.History.RemoveAll(h => h.FilePath == entry.FilePath && h.Timestamp == entry.Timestamp);
        HasHistory = History.Count > 0;
        SettingsService.Save(_settings);
    }

    [RelayCommand]
    private void ClearHistory()
    {
        History.Clear();
        _settings.History.Clear();
        HasHistory = false;
        SettingsService.Save(_settings);
    }
}
