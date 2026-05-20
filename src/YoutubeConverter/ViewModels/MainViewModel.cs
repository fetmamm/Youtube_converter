using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    private bool _isMp3 = true;

    [ObservableProperty]
    private bool _isMp4;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(PasteCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusText = "Redo. Klistra in eller dra en YouTube-länk.";

    [ObservableProperty]
    private string? _previewTitle;

    [ObservableProperty]
    private string? _previewAuthor;

    [ObservableProperty]
    private string? _previewDuration;

    [ObservableProperty]
    private string? _previewThumbnail;

    [ObservableProperty]
    private bool _hasPreview;

    [ObservableProperty]
    private string _selectedQuality = "Bästa";

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

    public bool ShowQualityPicker => IsMp4;
    public bool CanShowOpenFolder => HasLastFile && !IsBusy;

    public MainViewModel()
    {
        SelectedQuality = _settings.LastQuality;
        foreach (var h in _settings.History) History.Add(h);
        HasHistory = History.Count > 0;
        _ = CheckForUpdatesAsync();
    }

    [ObservableProperty]
    private bool _hasHistory;

    partial void OnUrlChanged(string value) => _ = LoadPreviewAsync(value);
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

    private async Task LoadPreviewAsync(string url)
    {
        _previewCts?.Cancel();
        var cts = _previewCts = new CancellationTokenSource();

        HasPreview = false;
        PreviewTitle = PreviewAuthor = PreviewDuration = PreviewThumbnail = null;

        if (string.IsNullOrWhiteSpace(url) || !LooksLikeYoutubeUrl(url)) return;

        try
        {
            await Task.Delay(400, cts.Token);
            var preview = await _service.GetPreviewAsync(url, cts.Token);
            if (cts.IsCancellationRequested) return;

            PreviewTitle = preview.Title;
            PreviewAuthor = preview.Author;
            PreviewDuration = preview.Duration is { } d ? d.ToString(d.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss") : null;
            PreviewThumbnail = preview.ThumbnailUrl;
            HasPreview = true;
            StatusText = "Klar att exportera.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText = $"Kunde inte läsa video: {ex.Message}";
        }
    }

    private static bool LooksLikeYoutubeUrl(string url) =>
        url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);

    [RelayCommand(CanExecute = nameof(CanPaste))]
    private void Paste()
    {
        if (Clipboard.ContainsText())
            Url = Clipboard.GetText().Trim();
    }

    private bool CanPaste() => !IsBusy;

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
                Filter = isAudio ? "MP3 ljud (*.mp3)|*.mp3" : "MP4 video (*.mp4)|*.mp4",
                Title = "Spara som"
            };
            if (!string.IsNullOrEmpty(_settings.LastFolder) && Directory.Exists(_settings.LastFolder))
                dialog.InitialDirectory = _settings.LastFolder;

            if (dialog.ShowDialog() != true)
            {
                StatusText = "Avbruten.";
                return;
            }

            _settings.LastFolder = Path.GetDirectoryName(dialog.FileName);
            SettingsService.Save(_settings);

            var progress = new Progress<double>(p =>
            {
                Progress = p;
                StatusText = $"Laddar ner… {p * 100:0}%";
            });

            StatusText = "Laddar ner…";
            await _service.DownloadAsync(url, dialog.FileName, isAudio, SelectedQuality, progress, _downloadCts.Token);

            Progress = 1;
            _lastSavedFile = dialog.FileName;
            HasLastFile = true;
            StatusText = $"Klart! Sparat: {Path.GetFileName(dialog.FileName)}";

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
            StatusText = "Avbruten av användaren.";
            Progress = 0;
        }
        catch (Exception ex)
        {
            StatusText = $"Fel: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    private bool CanExport() => !IsBusy && !string.IsNullOrWhiteSpace(Url);

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _downloadCts?.Cancel();
    }

    private bool CanCancel() => IsBusy;

    [RelayCommand]
    private void OpenLastFolder()
    {
        if (string.IsNullOrEmpty(_lastSavedFile) || !File.Exists(_lastSavedFile)) return;
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_lastSavedFile}\"") { UseShellExecute = true });
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
        var info = await UpdateService.CheckForUpdateAsync();
        if (info != null)
        {
            UpdateAvailableText = $"Ny version {info.LatestVersion} tillgänglig";
            UpdateUrl = info.ReleaseUrl;
        }
    }

    [RelayCommand]
    private void OpenUpdate()
    {
        if (!string.IsNullOrEmpty(UpdateUrl))
            Process.Start(new ProcessStartInfo(UpdateUrl) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenHistoryItem(HistoryEntry? entry)
    {
        if (entry == null) return;
        if (File.Exists(entry.FilePath))
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{entry.FilePath}\"") { UseShellExecute = true });
        else
            StatusText = "Filen finns inte längre på disk.";
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
