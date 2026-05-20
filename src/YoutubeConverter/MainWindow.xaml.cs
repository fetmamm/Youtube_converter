using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Controls;
using YoutubeConverter.ViewModels;

namespace YoutubeConverter;

public partial class MainWindow : FluentWindow
{
    private const string DiscordUrl = "https://discord.gg/9BJcKrDmbG";

    public MainWindow()
    {
        InitializeComponent();
    }

    private void ContactButton_Click(object sender, RoutedEventArgs e)
    {
        ContactPopup.IsOpen = !ContactPopup.IsOpen;
    }

    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        HistoryPopup.IsOpen = !HistoryPopup.IsOpen;
    }

    private void UpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        UpdatesPopup.IsOpen = !UpdatesPopup.IsOpen;
    }

    private void DiscordButton_Click(object sender, RoutedEventArgs e)
    {
        ContactPopup.IsOpen = false;
        Process.Start(new ProcessStartInfo(DiscordUrl) { UseShellExecute = true });
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.UnicodeText)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        string? text = null;
        if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            text = e.Data.GetData(DataFormats.UnicodeText) as string;
        else if (e.Data.GetDataPresent(DataFormats.Text))
            text = e.Data.GetData(DataFormats.Text) as string;

        if (!string.IsNullOrWhiteSpace(text) && DataContext is MainViewModel vm)
            vm.HandleDroppedText(text);
    }
}
