using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeConverter.Helpers;
using YoutubeConverter.Resources;

namespace YoutubeConverter.ViewModels;

public partial class MainViewModel
{
    private CancellationTokenSource? _previewCts;

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

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        var url = Url.Trim();
        if (string.IsNullOrWhiteSpace(url)) return;

        _previewCts?.Cancel();
        var cts = _previewCts = new CancellationTokenSource();

        HasPreview = false;
        PreviewTitle = PreviewAuthor = PreviewDuration = PreviewThumbnail = null;
        StatusText = Strings.AnalyzingVideo;

        try
        {
            var preview = await _service.GetPreviewAsync(url, cts.Token);
            if (cts.IsCancellationRequested) return;

            PreviewTitle = preview.Title;
            PreviewAuthor = preview.Author;
            PreviewDuration = preview.Duration is { } d ? PathHelpers.FormatDuration(d) : null;
            PreviewThumbnail = preview.ThumbnailUrl;
            HasPreview = true;
            if (preview.Duration is { } dur)
                TrimEnd = PathHelpers.FormatDuration(dur);
            StatusText = Strings.ReadyToExport;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText = string.Format(Strings.CouldNotReadVideoFmt, ex.Message);
        }
    }

    private bool CanAnalyze() => !IsBusy && !string.IsNullOrWhiteSpace(Url);
}
