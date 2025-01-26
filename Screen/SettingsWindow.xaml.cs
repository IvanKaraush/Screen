using System.Globalization;
using Screen.Resources;
using Screen.Services;
using Screen.Settings;

namespace Screen;

public partial class SettingsWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        StartupCheckBox.IsChecked = StartupManager.IsAddedToStartup;
        LanguageComboBox.Text = AppResources.Culture.Name switch
        {
            "en-GB" => "English",
            "ru-RU" => "Русский",
            _ => LanguageComboBox.Text
        };
    }

    private void SaveSettingsChanges(object sender, EventArgs e)
    {
        var selected = LanguageComboBox.Text;
        if (StartupCheckBox.IsChecked == true)
        {
            StartupManager.Enable();
        }
        else
        {
            StartupManager.Disable();
        }

        if (!string.IsNullOrWhiteSpace(selected))
        {
            AppResources.Culture = selected switch
            {
                "Русский" => CultureInfo.GetCultureInfo("ru-RU"),
                "English" => CultureInfo.GetCultureInfo("en-GB"),
                _ => AppResources.Culture
            };
        }

        var appSettings = SettingsManager.GetAppSettings();
        appSettings.SelectedCulture = AppResources.Culture.Name;
        SettingsManager.SaveChanges(appSettings);

        Application.Restart();
        System.Windows.Application.Current.Shutdown();
    }
}