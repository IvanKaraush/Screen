using Microsoft.Win32;
using Screen.Primitives;

namespace Screen.Services;

public static class StartupManager
{
    private static readonly string ExecutablePath;

    static StartupManager()
    {
        try
        {
            var mainModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            if (mainModule == null || string.IsNullOrEmpty(mainModule.FileName))
            {
                throw new InvalidOperationException("Не удалось определить путь исполняемого файла.");
            }

            ExecutablePath = mainModule.FileName;
        }
        catch (Exception ex)
        {
            ExecutablePath = string.Empty;
            throw new InvalidOperationException("Ошибка инициализации пути исполняемого файла.", ex);
        }
    }

    /// <summary>
    /// Проверяет, добавлено ли приложение в автозагрузку.
    /// </summary>
    public static bool IsAddedToStartup
    {
        get
        {
            using (var key = Registry.CurrentUser.OpenSubKey(AppConstants.RegistryPath, false))
            {
                return key?.GetValue(AppConstants.AppName)?.ToString() == GetStartupCommand();
            }
        }
    }

    /// <summary>
    /// Проверяет, был ли указан аргумент <see cref="AppConstants.SilentArgument"/> при запуске.
    /// </summary>
    public static bool IsSilentMode
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            return args.Contains(AppConstants.SilentArgument, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Добавляет приложение в автозагрузку.
    /// </summary>
    public static void Enable()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(AppConstants.RegistryPath, true))
        {
            key?.SetValue(AppConstants.AppName, GetStartupCommand());
        }
    }

    /// <summary>
    /// Удаляет приложение из автозагрузки.
    /// </summary>
    public static void Disable()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(AppConstants.RegistryPath, true))
        {
            key?.DeleteValue(AppConstants.AppName, false);
        }
    }

    private static string GetStartupCommand()
    {
        if (string.IsNullOrEmpty(ExecutablePath))
        {
            throw new InvalidOperationException("Путь исполняемого файла не определён.");
        }

        return $"\"{ExecutablePath}\" {AppConstants.SilentArgument}";
    }
}