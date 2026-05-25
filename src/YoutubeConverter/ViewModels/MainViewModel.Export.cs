using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YoutubeConverter.Helpers;
using YoutubeConverter.Models;
using YoutubeConverter.Resources;
using YoutubeConverter.Services;

namespace YoutubeConverter.ViewModels;

public partial class MainViewModel
{
    private CancellationTokenSource? _downloadCts;
    private string? _lastSavedFile;

    [ObservableProperty]
    private bool _trimEnabled;

    [ObservableProperty]
    private string _trimStart = "0:00";

    [ObservableProperty]
    private string _trimEnd = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowOpenFolder))]
    private bool _hasLastFile;

    public bool CanShowOpenFolder => HasLastFile && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        var url = Url.Trim();
        var isInstagram = IsInstagramSelected;
        var isAudio = IsMp3;
        var ext = isAudio ? "mp3" : "mp4";

        _downloadCts = new CancellationTokenSource();
        IsBusy = true;
        Progress = 0;
        HasLastFile = false;
        string? overwriteTempFile = null;

        try
        {
            var title = PreviewTitle ?? (isInstagram
                ? await _instagramService.GetVideoTitleAsync(url, _downloadCts.Token)
                : await _youtubeService.GetVideoTitleAsync(url, _downloadCts.Token));
            var safeName = PathHelpers.SanitizeFileName(title);

            var dialog = new SaveFileDialog
            {
                FileName = safeName,
                DefaultExt = ext,
                AddExtension = true,
                Filter = isAudio ? Strings.Mp3FilterText : Strings.Mp4FilterText,
                OverwritePrompt = true,
                Title = Strings.SaveAsDialogTitle
            };
            if (!string.IsNullOrEmpty(_settings.LastFolder) && Directory.Exists(_settings.LastFolder))
                dialog.InitialDirectory = _settings.LastFolder;

            if (dialog.ShowDialog() != true)
            {
                StatusText = Strings.Cancelled;
                return;
            }

            var finalFileName = dialog.FileName;
            var downloadFileName = finalFileName;
            if (File.Exists(finalFileName))
            {
                var dir = Path.GetDirectoryName(finalFileName) ?? Path.GetTempPath();
                overwriteTempFile = Path.Combine(dir, $"_overwrite_{Guid.NewGuid():N}.{ext}");
                downloadFileName = overwriteTempFile;
            }

            _settings.LastFolder = Path.GetDirectoryName(finalFileName);
            SettingsService.Save(_settings);

            var progress = new Progress<double>(p =>
            {
                Progress = p;
                if (p < 1)
                    StatusText = string.Format(Strings.DownloadingProgressFmt, p * 100);
            });

            TimeSpan? trimStart = null, trimEnd = null;
            if (TrimEnabled)
            {
                trimStart = YoutubeDownloadService.ParseTime(TrimStart);
                trimEnd = YoutubeDownloadService.ParseTime(TrimEnd);
                if (trimStart.HasValue && trimEnd.HasValue && trimEnd <= trimStart)
                {
                    StatusText = Strings.TrimEndAfterStart;
                    return;
                }
            }

            StatusText = TrimEnabled ? Strings.DownloadingAndTrimming : Strings.Downloading;
            if (isInstagram)
            {
                await _instagramService.DownloadAsync(url, downloadFileName, isAudio, trimStart, trimEnd, progress, _downloadCts.Token);
            }
            else
            {
                await _youtubeService.DownloadAsync(url, downloadFileName, isAudio, SelectedQuality, trimStart, trimEnd, progress, _downloadCts.Token);
            }

            if (overwriteTempFile != null)
            {
                File.Copy(overwriteTempFile, finalFileName, true);
                File.Delete(overwriteTempFile);
                overwriteTempFile = null;
            }

            Progress = 1;
            _lastSavedFile = finalFileName;
            HasLastFile = true;
            StatusText = string.Format(Strings.DoneSavedFmt, Path.GetFileName(finalFileName));

            AddToHistory(new HistoryEntry
            {
                Title = title,
                Url = url,
                FilePath = finalFileName,
                Format = ext.ToUpperInvariant(),
                Platform = isInstagram ? DownloadPlatform.Instagram : DownloadPlatform.Youtube,
                Timestamp = DateTime.Now
            });
        }
        catch (OperationCanceledException)
        {
            StatusText = Strings.CancelledByUser;
            Progress = 0;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Strings.ErrorFmt, ex.Message);
        }
        finally
        {
            if (overwriteTempFile != null && File.Exists(overwriteTempFile))
            {
                try { File.Delete(overwriteTempFile); } catch { }
            }
            IsBusy = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    private bool CanExport() => !IsBusy && HasPreview;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _downloadCts?.Cancel();
    private bool CanCancel() => IsBusy;

    [RelayCommand]
    private void OpenLastFolder()
    {
        if (!string.IsNullOrEmpty(_lastSavedFile) && File.Exists(_lastSavedFile))
        {
            ExplorerLauncher.RevealFile(_lastSavedFile);
            return;
        }

        var folder = !string.IsNullOrEmpty(_settings.LastFolder) && Directory.Exists(_settings.LastFolder)
            ? _settings.LastFolder
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        if (!Directory.Exists(folder))
            folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        ExplorerLauncher.OpenFolder(folder);
    }
}
