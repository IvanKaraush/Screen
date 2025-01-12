using Microsoft.Win32;

namespace Screen.Services;

public static class StartupManager
{
    private const string AppName = "Screen";
    private static readonly string ExecutablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    private static readonly string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled
    {
        get
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
            {
                return key?.GetValue(AppName) as string == ExecutablePath;
            }
        }
    }

    public static void Enable()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
        {
            key?.SetValue(AppName, ExecutablePath);
        }
    }

    public static void Disable()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
        {
            key?.DeleteValue(AppName, false);
        }
    }
}