namespace ModPlus.LanguageTools.Cli;

using Publishing;

/// <summary>
/// Параметры выгрузки из переменных окружения (в GitHub Actions — из Secrets).
/// </summary>
/// <remarks>
/// Приватные ключи можно передать содержимым (<c>..._PRIVATE_KEY</c>) — тогда они записываются
/// во временные файлы, которые удаляются при освобождении объекта, — или путём к файлу
/// (<c>..._PRIVATE_KEY_FILE</c>). Пустая переменная равносильна отсутствующей: GitHub
/// подставляет пустую строку вместо незаданного секрета.
/// </remarks>
internal sealed class EnvironmentSettings : IDisposable
{
    private readonly List<string> _temporaryFiles = [];

    private EnvironmentSettings()
    {
    }

    /// <summary>
    /// Параметры выгрузки
    /// </summary>
    public PublishSettings Value { get; } = new();

    /// <summary>
    /// Читает параметры выгрузки из переменных окружения
    /// </summary>
    public static EnvironmentSettings Read()
    {
        var result = new EnvironmentSettings();
        var settings = result.Value;

        settings.SiteSftpHost = Get("MODPLUS_SITE_SFTP_HOST");
        settings.SiteSftpPort = GetPort("MODPLUS_SITE_SFTP_PORT", settings.SiteSftpPort);
        settings.SiteSftpUserName = Get("MODPLUS_SITE_SFTP_USER") ?? settings.SiteSftpUserName;
        settings.SiteSftpPrivateKeyFile = result.GetKeyFile("MODPLUS_SITE_SFTP_PRIVATE_KEY");
        settings.SiteRootPath = Get("MODPLUS_SITE_ROOT_PATH");

        settings.ServiceSftpHost = Get("MODPLUS_SERVICE_SFTP_HOST");
        settings.ServiceSftpPort = GetPort("MODPLUS_SERVICE_SFTP_PORT", settings.ServiceSftpPort);
        settings.ServiceSftpUserName = Get("MODPLUS_SERVICE_SFTP_USER") ?? settings.ServiceSftpUserName;
        settings.ServiceSftpPassword = Get("MODPLUS_SERVICE_SFTP_PASSWORD");
        settings.ServiceSftpPrivateKeyFile = result.GetKeyFile("MODPLUS_SERVICE_SFTP_PRIVATE_KEY");
        settings.ServiceDownloadsPath = Get("MODPLUS_SERVICE_DOWNLOADS_PATH");

        settings.StorageServiceUrl = Get("MODPLUS_STORAGE_SERVICE_URL") ?? settings.StorageServiceUrl;
        settings.StorageBucketName = Get("MODPLUS_STORAGE_BUCKET") ?? settings.StorageBucketName;
        settings.StorageAccessKey = Get("MODPLUS_STORAGE_ACCESS_KEY");
        settings.StorageSecretKey = Get("MODPLUS_STORAGE_SECRET_KEY");
        settings.StorageApiUrl = Get("MODPLUS_STORAGE_API_URL") ?? settings.StorageApiUrl;

        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var file in _temporaryFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Временный файл в папке runner'а удалится вместе с ним
            }
        }

        _temporaryFiles.Clear();
    }

    private static string Get(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int GetPort(string name, int defaultValue)
    {
        var value = Get(name);
        if (value == null)
            return defaultValue;

        return int.TryParse(value, out var port) && port is > 0 and < 65536
            ? port
            : throw new LanguageToolsException($"{name}: «{value}» не является номером порта");
    }

    /// <summary>
    /// Возвращает путь к файлу ключа: из <c>{name}_FILE</c> или временный файл с содержимым <c>{name}</c>
    /// </summary>
    private string GetKeyFile(string name)
    {
        var file = Get($"{name}_FILE");
        if (file != null)
            return file;

        // Ключ берётся без Trim: Get обрезает пробелы, а завершающий перевод строки
        // в OpenSSH-ключе обязателен
        var content = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(content))
            return null;

        content = content.Replace("\r\n", "\n");
        if (!content.EndsWith('\n'))
            content += "\n";

        var path = Path.Combine(Path.GetTempPath(), $"modplus-{Guid.NewGuid():N}.key");
        File.WriteAllText(path, content);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        _temporaryFiles.Add(path);
        return path;
    }
}
