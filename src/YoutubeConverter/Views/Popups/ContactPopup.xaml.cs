using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace YoutubeConverter.Views.Popups;

public partial class ContactPopup : UserControl
{
    private const string DiscordUrl = "https://discord.gg/9BJcKrDmbG";

    public ContactPopup() => InitializeComponent();

    private void DiscordButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(DiscordUrl) { UseShellExecute = true });
    }
}
