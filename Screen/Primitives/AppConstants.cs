namespace Screen.Primitives;

public static class AppConstants
{
    /// <summary>
    /// Имя приложения.
    /// </summary>
    public const string AppName = "Screen";

    /// <summary>
    /// Аргумент для запуска в фоновом режиме.
    /// </summary>
    public const string SilentArgument = "--silent";

    /// <summary>
    /// Путь к ключу реестра для автозагрузки.
    /// </summary>
    public const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
}