using System.Globalization;
using Screen.Resources;
using Screen.Settings;

namespace Screen;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public App()
    {
        var appSettings = SettingsManager.GetAppSettings();
        AppResources.Culture = CultureInfo.GetCultureInfo(appSettings.SelectedCulture);
    }
}