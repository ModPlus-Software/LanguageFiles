namespace ModPlus.LanguageTools.Publishing;

using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Renci.SshNet;

/// <summary>
/// Выгрузка собранных языковых файлов и <c>Langs.xml</c> на сервер сайта, сервер web-приложения
/// и в хранилище.
/// </summary>
/// <remarks>
/// Все проверки выполняются до первой выгрузки: <c>Langs.xml</c>, в котором указан язык
/// без файла или файл другой версии, сломал бы обновление переводов у пользователей.
/// <para>
/// <c>Langs.xml</c> в каждое место выгружается после языковых файлов: клиент не должен увидеть
/// новую версию раньше файлов.
/// </para>
/// <para>
/// Хранилище — не единственный источник файлов: при неудаче клиенты скачивают файлы с сервера
/// web-приложения. Поэтому сбой хранилища не прерывает публикацию: в лог пишется предупреждение,
/// а в результате <see cref="PublishResult.StorageUploaded"/> = <c>false</c>.
/// </para>
/// </remarks>
public static class LanguagePublisher
{
    /// <summary>
    /// Выгружает языковые файлы
    /// </summary>
    /// <param name="root">Папка с файлами локализации (источник <c>Langs.xml</c> и состава языков)</param>
    /// <param name="builtDirectory">Папка с собранными языковыми файлами</param>
    /// <param name="settings">Параметры выгрузки</param>
    /// <param name="log">Лог или <c>null</c></param>
    /// <exception cref="LanguageToolsException">Не собраны файлы, расходятся версии, не заданы параметры</exception>
    public static async Task<PublishResult> PublishAsync(
        string root,
        string builtDirectory,
        PublishSettings settings,
        ILanguageToolsLog log = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        log ??= NullLanguageToolsLog.Instance;

        settings.Validate();

        root = LanguageFolder.EnsureRoot(root);
        var languages = LanguageFolder.GetLanguageNames(root, log);
        log.Info($"Языки: {string.Join(", ", languages)}");

        var languageFiles = languages
            .Select(language => Path.Combine(builtDirectory, $"{language}.xml"))
            .ToList();

        var version = LanguageMerger.GetBuiltVersion(languageFiles);

        // Версия в Langs.xml меняется отдельно от сборки. Если после её смены файлы
        // не пересобрали, клиент получил бы старые переводы под видом новых
        var sourceVersion = LanguagesVersionFile.Read(root);
        if (version != sourceVersion)
        {
            throw new LanguageToolsException(
                $"Версия собранных языковых файлов ({version}) не совпадает с версией в " +
                $"{LanguageFolder.VersionFileName} ({sourceVersion}) — выгрузка отменена, выполните сборку заново");
        }

        // Состав языков в Langs.xml приводится к папкам языков
        LanguagesVersionFile.Write(root, languages, version, log);
        var versionFile = LanguageFolder.GetVersionFilePath(root);

        log.Info($"Выгрузка языковых файлов на сервер сайта: {settings.SiteLanguagesPath}");
        using (var client = CreateSiteClient(settings))
        {
            client.Connect();
            foreach (var file in languageFiles)
                Upload(client, file, settings.SiteLanguagesPath, log);

            Upload(client, versionFile, settings.SiteDownloadsPath, log);
        }

        var serviceDirectory = $"{settings.ServiceDownloadsPath}/Languages";
        log.Info($"Выгрузка языковых файлов на сервер web-приложения: {serviceDirectory}");
        using (var client = CreateServiceClient(settings, log))
        {
            client.Connect();
            EnsureDirectory(client, serviceDirectory, log);

            foreach (var file in languageFiles)
                Upload(client, file, serviceDirectory, log);

            Upload(client, versionFile, serviceDirectory, log);
        }

        var storageUploaded = await UploadToStorageAsync(settings, languageFiles, versionFile, log).ConfigureAwait(false);

        log.Info($"Языковые файлы версии {version} выгружены");

        return new PublishResult(version, storageUploaded);
    }

    private static async Task<bool> UploadToStorageAsync(
        PublishSettings settings,
        IReadOnlyList<string> languageFiles,
        string versionFile,
        ILanguageToolsLog log)
    {
        log.Info("Выгрузка языковых файлов в хранилище");
        try
        {
            var config = new AmazonS3Config { ServiceURL = settings.StorageServiceUrl };
            AWSCredentials credentials = new BasicAWSCredentials(settings.StorageAccessKey, settings.StorageSecretKey);
            using var client = new AmazonS3Client(credentials, config);

            foreach (var file in languageFiles)
                await UploadToStorageAsync(client, settings.StorageBucketName, file, log).ConfigureAwait(false);

            await UploadToStorageAsync(client, settings.StorageBucketName, versionFile, log).ConfigureAwait(false);

            return true;
        }
        catch (Exception exception)
        {
            log.Warning(
                $"!!! Не удалось выгрузить в хранилище языковые файлы. {exception.GetType().Name}: {exception.Message}. " +
                "Файлы есть только на серверах — после восстановления хранилища выгрузите их повторно");
            return false;
        }
    }

    private static async Task UploadToStorageAsync(AmazonS3Client client, string bucketName, string file, ILanguageToolsLog log)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            StorageClass = S3StorageClass.Standard,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.None,
            FilePath = file,
            Key = $"Languages/{Path.GetFileName(file)}",
        };

        var response = await client.PutObjectAsync(request).ConfigureAwait(false);

        if (response.HttpStatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"Не удалось выгрузить в хранилище файл {file}: {response.HttpStatusCode}");

        log.Info($"File sent to storage: {file}");
    }

    /// <summary>
    /// Клиент SFTP сервера сайта (авторизация по приватному ключу)
    /// </summary>
    private static SftpClient CreateSiteClient(PublishSettings settings)
    {
        var keyFile = new PrivateKeyFile(settings.SiteSftpPrivateKeyFile);
        var connectionInfo = new ConnectionInfo(
            settings.SiteSftpHost,
            settings.SiteSftpPort,
            settings.SiteSftpUserName,
            new PrivateKeyAuthenticationMethod(settings.SiteSftpUserName, keyFile));

        return new SftpClient(connectionInfo);
    }

    /// <summary>
    /// Клиент SFTP сервера web-приложения (авторизация по паролю и, если задан файл ключа, по ключу)
    /// </summary>
    /// <param name="settings">Параметры выгрузки</param>
    /// <param name="log">Лог</param>
    private static SftpClient CreateServiceClient(PublishSettings settings, ILanguageToolsLog log)
    {
        var methods = new List<AuthenticationMethod>
        {
            new PasswordAuthenticationMethod(settings.ServiceSftpUserName, settings.ServiceSftpPassword),
        };

        // Ключ — дополнительный способ авторизации: если файла нет или его не удалось прочитать,
        // подключение выполняется по паролю, как и раньше
        if (!string.IsNullOrWhiteSpace(settings.ServiceSftpPrivateKeyFile) && File.Exists(settings.ServiceSftpPrivateKeyFile))
        {
            try
            {
                methods.Add(new PrivateKeyAuthenticationMethod(
                    settings.ServiceSftpUserName, new PrivateKeyFile(settings.ServiceSftpPrivateKeyFile)));
            }
            catch (Exception exception)
            {
                log.Warning(
                    $"Не удалось прочитать ключ сервера web-приложения {settings.ServiceSftpPrivateKeyFile}: " +
                    $"{exception.Message}. Подключение по паролю");
            }
        }

        var connectionInfo = new ConnectionInfo(
            settings.ServiceSftpHost,
            settings.ServiceSftpPort,
            settings.ServiceSftpUserName,
            methods.ToArray());

        return new SftpClient(connectionInfo);
    }

    /// <summary>
    /// Создаёт папку на сервере вместе с недостающими родительскими папками
    /// </summary>
    private static void EnsureDirectory(SftpClient client, string remoteDirectory, ILanguageToolsLog log)
    {
        if (string.IsNullOrWhiteSpace(remoteDirectory) || client.Exists(remoteDirectory))
            return;

        var isRooted = remoteDirectory[0] == '/';
        var current = string.Empty;

        foreach (var segment in remoteDirectory.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current.Length == 0
                ? isRooted ? $"/{segment}" : segment
                : $"{current}/{segment}";

            if (client.Exists(current))
                continue;

            client.CreateDirectory(current);
            log.Info($"Directory created: {current}");
        }
    }

    /// <summary>
    /// Выгружает один файл через открытое подключение
    /// </summary>
    private static void Upload(SftpClient client, string localFile, string remoteDirectory, ILanguageToolsLog log)
    {
        using var stream = new FileStream(localFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        client.UploadFile(stream, $"{remoteDirectory}/{Path.GetFileName(localFile)}");

        log.Info($"File sent: {localFile}");
    }
}

/// <summary>
/// Результат выгрузки языковых файлов
/// </summary>
/// <param name="Version">Выгруженная версия</param>
/// <param name="StorageUploaded">Удалось ли выгрузить в хранилище</param>
public sealed record PublishResult(Version Version, bool StorageUploaded);
