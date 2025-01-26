using System.IO;
using System.Text.Json;

namespace Screen.Settings;

public class SettingsManager
{
    private const string ConfigFileName = "appsettings.json";

    public static AppSettings GetAppSettings()
    {
        var projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;
        if (string.IsNullOrEmpty(projectDirectory))
        {
            throw new InvalidOperationException();
        }

        var appSettingsFullPath = Path.Combine(projectDirectory, ConfigFileName);
        if (!File.Exists(appSettingsFullPath))
        {
            throw new FileNotFoundException(ConfigFileName);
        }

        var json = File.ReadAllText(appSettingsFullPath);
        return JsonSerializer.Deserialize<AppSettings>(json) ??
               throw new InvalidOperationException(); //todo: Не забыть про message
    }

    public static void SaveChanges(AppSettings appSettings)
    {
        var projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;
        if (string.IsNullOrEmpty(projectDirectory))
        {
            throw new InvalidOperationException();
        }

        var appSettingsFullPath = Path.Combine(projectDirectory, ConfigFileName);
        var json = JsonSerializer.Serialize(appSettings);
        File.WriteAllText(appSettingsFullPath, json);
    }
}