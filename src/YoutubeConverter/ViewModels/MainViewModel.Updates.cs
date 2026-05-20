using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeConverter.Resources;
using YoutubeConverter.Services;

namespace YoutubeConverter.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty]
    private string? _updateAvailableText;

    [ObservableProperty]
    private string? _updateUrl;

    [ObservableProperty]
    private string _latestVersionText = Strings.LatestVersionUnknown;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckUpdatesCommand))]
    private bool _isCheckingUpdates;

    [ObservableProperty]
    private bool _updateAvailable;

    private async Task CheckForUpdatesAsync()
    {
        IsCheckingUpdates = true;
        try
        {
            var info = await UpdateService.CheckForUpdateAsync();
            if (info != null)
            {
                UpdateAvailable = true;
                UpdateAvailableText = string.Format(Strings.NewVersionAvailableFmt, info.LatestVersion);
                UpdateUrl = info.ReleaseUrl;
                LatestVersionText = $"v{info.LatestVersion}";
            }
            else
            {
                UpdateAvailable = false;
                LatestVersionText = string.Format(Strings.LatestVersionFmt, AppVersion);
            }
        }
        catch
        {
            LatestVersionText = Strings.LatestVersionUnavailable;
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
}
