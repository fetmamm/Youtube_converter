using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YoutubeConverter.Models;
using YoutubeConverter.Resources;
using YoutubeConverter.Services;

namespace YoutubeConverter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly YoutubeDownloadService _youtubeService = new();
    private readonly InstagramDownloadService _instagramService = new();
    private readonly AppSettings _settings = SettingsService.Load();

    public ObservableCollection<string> Qualities { get; } = new(YoutubeDownloadService.Qualities);

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
    private string _statusText = Strings.ReadyPrompt;

    [ObservableProperty]
    private string _selectedQuality = Strings.DefaultQuality;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsYoutubeSelected))]
    [NotifyPropertyChangedFor(nameof(IsInstagramSelected))]
    [NotifyPropertyChangedFor(nameof(ShowFormatPicker))]
    [NotifyPropertyChangedFor(nameof(ShowQualityPicker))]
    [NotifyPropertyChangedFor(nameof(UrlPlaceholder))]
    private DownloadPlatform _selectedPlatform = DownloadPlatform.Youtube;

    public bool IsYoutubeSelected => SelectedPlatform == DownloadPlatform.Youtube;
    public bool IsInstagramSelected => SelectedPlatform == DownloadPlatform.Instagram;
    public bool ShowFormatPicker => true;
    public bool ShowQualityPicker => IsYoutubeSelected && IsMp4;
    public string UrlPlaceholder => IsInstagramSelected
        ? Strings.InstagramUrlPlaceholder
        : Strings.YoutubeUrlPlaceholder;

    public MainViewModel()
    {
        SelectedQuality = YoutubeDownloadService.Qualities.Contains(_settings.LastQuality)
            ? _settings.LastQuality
            : Strings.DefaultQuality;
        foreach (var h in _settings.History) History.Add(h);
        HasHistory = History.Count > 0;
        _ = CheckForUpdatesAsync();
    }

    partial void OnUrlChanged(string value)
    {
        ClearPreview();
        AnalyzeCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPlatformChanged(DownloadPlatform value)
    {
        _previewCts?.Cancel();
        ClearPreview();
        Url = string.Empty;
        StatusText = IsInstagramSelected ? Strings.InstagramReadyPrompt : Strings.ReadyPrompt;
        AnalyzeCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
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

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void SelectYoutube() => SelectedPlatform = DownloadPlatform.Youtube;

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void SelectInstagram() => SelectedPlatform = DownloadPlatform.Instagram;

    public void HandleDroppedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Url = text.Trim();
    }

    private void ClearPreview()
    {
        HasPreview = false;
        PreviewTitle = PreviewAuthor = PreviewDuration = PreviewThumbnail = null;
    }
}
