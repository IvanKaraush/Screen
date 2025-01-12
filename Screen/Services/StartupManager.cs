using Microsoft.Win32;

namespace Screen.Services;

public static class StartupManager
{
    private const string AppName = "Screen";
    private const string SilentArgument = "--silent";
    private static readonly string ExecutablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    private static readonly string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Проверяет, добавлено ли приложение в автозагрузку.
    /// </summary>
    public static bool IsEnabled
    {
        get
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
            {
                return key?.GetValue(AppName)?.ToString() == GetStartupCommand();
            }
        }
    }
    
    /// <summary>
    /// Проверяет, был ли указан аргумент "--silent" при запуске.
    /// </summary>
    public static bool IsSilentMode
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            return args.Contains(SilentArgument, StringComparer.OrdinalIgnoreCase);
        }
    }

    public static void Enable()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
        {
            key?.SetValue(AppName, GetStartupCommand());
        }
    }

    public static void Disable()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
        {
            key?.DeleteValue(AppName, false);
        }
    }
    
    private static string GetStartupCommand() => $"\"{ExecutablePath}\" {SilentArgument}";
}