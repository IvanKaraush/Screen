using Screen.Services;

namespace Screen;

public partial class SettingsWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        StartupCheckBox.IsChecked = StartupManager.IsAddedToStartup;
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