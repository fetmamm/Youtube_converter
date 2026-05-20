using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace YoutubeConverter.Views.Popups;

public partial class InfoPopup : UserControl
{
    public InfoPopup() => InitializeComponent();

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (Parent is Popup popup)
            popup.IsOpen = false;
    }
}
