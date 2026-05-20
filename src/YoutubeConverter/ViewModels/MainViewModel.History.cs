using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeConverter.Models;
using YoutubeConverter.Resources;
using YoutubeConverter.Services;

namespace YoutubeConverter.ViewModels;

public partial class MainViewModel
{
    public ObservableCollection<HistoryEntry> History { get; } = new();

    [ObservableProperty]
    private bool _hasHistory;

    private void AddToHistory(HistoryEntry entry)
    {
        History.Insert(0, entry);
        _settings.History.Insert(0, entry);
        HasHistory = true;
        SettingsService.Save(_settings);
    }

    [RelayCommand]
    private void OpenHistoryItem(HistoryEntry? entry)
    {
        if (entry == null) return;
        if (System.IO.File.Exists(entry.FilePath))
            ExplorerLauncher.RevealFile(entry.FilePath);
        else
            StatusText = Strings.FileNoLongerExists;
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
