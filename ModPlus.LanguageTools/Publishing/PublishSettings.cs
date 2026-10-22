namespace ModPlus.LanguageTools.Publishing;

/// <summary>
/// Параметры выгрузки языковых файлов.
/// </summary>
/// <remarks>
/// Используются два сервера с разной авторизацией и S3-совместимое хранилище:
/// <list type="bullet">
/// <item>сервер сайта — авторизация по приватному ключу;</item>
/// <item>сервер web-приложения — авторизация по паролю (и по ключу, если он задан);
/// резервный источник файлов для клиентов;</item>
/// <item>хранилище — основной источник файлов для клиентов.</item>
/// </list>
/// </remarks>
public sealed class PublishSettings
{
    /// <summary>
    /// Хост сервера сайта
    /// </summary>
    public string SiteSftpHost { get; set; }

    /// <summary>
    /// Порт SFTP сервера сайта
    /// </summary>
    public int SiteSftpPort { get; set; } = 22;

    /// <summary>
    /// Пользователь сервера сайта
    /// </summary>
    public string SiteSftpUserName { get; set; } = "root";

    /// <summary>
    /// Файл приватного ключа сервера сайта
    /// </summary>
    public string SiteSftpPrivateKeyFile { get; set; }

    /// <summary>
    /// Корневая папка сайта на сервере. Языковые файлы выгружаются в <c>{корень}/Downloads/Languages</c>,
    /// <c>Langs.xml</c> — в <c>{корень}/Downloads</c>
    /// </summary>
    public string SiteRootPath { get; set; }

    /// <summary>
    /// Хост сервера web-приложения
    /// </summary>
    public string ServiceSftpHost { get; set; }

    /// <summary>
    /// Порт SFTP сервера web-приложения
    /// </summary>
    public int ServiceSftpPort { get; set; } = 22;

    /// <summary>
    /// Пользователь сервера web-приложения
    /// </summary>
    public string ServiceSftpUserName { get; set; } = "root";

    /// <summary>
    /// Пароль сервера web-приложения
    /// </summary>
    public string ServiceSftpPassword { get; set; }

    /// <summary>
    /// Файл приватного ключа сервера web-приложения (необязательно). Используется, если файл существует
    /// </summary>
    public string ServiceSftpPrivateKeyFile { get; set; }

    /// <summary>
    /// Папка загрузок web-приложения на сервере. Языковые файлы и <c>Langs.xml</c> выгружаются
    /// в <c>{папка}/Languages</c>
    /// </summary>
    public string ServiceDownloadsPath { get; set; }

    /// <summary>
    /// Адрес S3-сервиса хранилища
    /// </summary>
    public string StorageServiceUrl { get; set; } = "https://s3.yandexcloud.net";

    /// <summary>
    /// Имя бакета хранилища
    /// </summary>
    public string StorageBucketName { get; set; } = "storage.modplus.org";

    /// <summary>
    /// Ключ доступа к хранилищу
    /// </summary>
    public string StorageAccessKey { get; set; }

    /// <summary>
    /// Секретный ключ хранилища
    /// </summary>
    public string StorageSecretKey { get; set; }

    /// <summary>
    /// Публичный адрес хранилища: откуда читается опубликованный <c>Langs.xml</c>
    /// </summary>
    public string StorageApiUrl { get; set; } = LanguagesVersionFile.DefaultStorageUrl;

    /// <summary>
    /// Папка языковых файлов на сервере сайта
    /// </summary>
    public string SiteLanguagesPath => $"{SiteDownloadsPath}/Languages";

    /// <summary>
    /// Папка загрузок на сервере сайта
    /// </summary>
    public string SiteDownloadsPath => $"{SiteRootPath}/Downloads";

    /// <summary>
    /// Проверяет, что заданы все обязательные параметры
    /// </summary>
    /// <exception cref="LanguageToolsException">Не задан обязательный параметр</exception>
    public void Validate()
    {
        var missing = new List<string>();

        void Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                missing.Add(name);
        }

        Require(SiteSftpHost, nameof(SiteSftpHost));
        Require(SiteSftpUserName, nameof(SiteSftpUserName));
        Require(SiteSftpPrivateKeyFile, nameof(SiteSftpPrivateKeyFile));
        Require(SiteRootPath, nameof(SiteRootPath));
        Require(ServiceSftpHost, nameof(ServiceSftpHost));
        Require(ServiceSftpUserName, nameof(ServiceSftpUserName));
        Require(ServiceSftpPassword, nameof(ServiceSftpPassword));
        Require(ServiceDownloadsPath, nameof(ServiceDownloadsPath));
        Require(StorageServiceUrl, nameof(StorageServiceUrl));
        Require(StorageBucketName, nameof(StorageBucketName));
        Require(StorageAccessKey, nameof(StorageAccessKey));
        Require(StorageSecretKey, nameof(StorageSecretKey));

        if (missing.Count > 0)
            throw new LanguageToolsException($"Не заданы параметры выгрузки: {string.Join(", ", missing)}");

        if (!File.Exists(SiteSftpPrivateKeyFile))
            throw new LanguageToolsException($"Не найден файл приватного ключа сервера сайта: {SiteSftpPrivateKeyFile}");
    }
}
