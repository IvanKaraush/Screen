using System.Windows;
using Screen.Services;

namespace Screen;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        StartupCheckBox.IsChecked = StartupManager.IsEnabled;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        if (StartupCheckBox.IsChecked == true)
        {
            StartupManager.Enable();
        }
        else
        {
            StartupManager.Disable();
        }
    }
}