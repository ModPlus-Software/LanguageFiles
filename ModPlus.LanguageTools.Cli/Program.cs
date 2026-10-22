namespace ModPlus.LanguageTools.Cli;

using System.Text;
using Checks;
using Publishing;

/// <summary>
/// Консольная утилита файлов локализации ModPlus: проверка, сборка и публикация.
/// Вся логика — в библиотеке ModPlus.LanguageTools, общей с редактором локализации
/// и программой сборки ModPlusBuilder.
/// </summary>
internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;
    private const int ExitUsage = 2;

    private static async Task<int> Main(string[] args)
    {
        TryUseUtf8Console();
        var log = new ConsoleLog();

        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return args.Length == 0 ? ExitUsage : ExitSuccess;
        }

        if (!TryParseOptions(args.Skip(1).ToArray(), out var options, out var error))
        {
            log.Fail(error);
            PrintUsage();
            return ExitUsage;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "check" => Check(options, log),
                "build" => Build(options, log),
                "publish" => await PublishAsync(options, log).ConfigureAwait(false),
                _ => UnknownCommand(args[0], log),
            };
        }
        catch (LanguageToolsException exception)
        {
            if (exception.CheckResult != null)
                log.Annotate(exception.CheckResult);

            log.Fail(exception.Message);
            return ExitFailure;
        }
        catch (Exception exception)
        {
            log.Fail($"Ошибка: {exception}");
            return ExitFailure;
        }
    }

    /// <summary>
    /// Проверка файлов локализации. Файлы не изменяются
    /// </summary>
    private static int Check(Options options, ConsoleLog log)
    {
        var result = LocalizationChecker.Check(ResolveRoot(options), log);
        log.Annotate(result);

        return result.HasErrors ? ExitFailure : ExitSuccess;
    }

    /// <summary>
    /// Сборка с версией из Langs.xml: исправление кавычек в исходных файлах, проверка, слияние
    /// </summary>
    private static int Build(Options options, ConsoleLog log)
    {
        var root = ResolveRoot(options);
        var output = RequireOutput(options);
        var version = LanguagesVersionFile.Read(root);

        var result = LanguageBuilder.Build(root, output, version, log);
        log.Annotate(result.Check);

        return ExitSuccess;
    }

    /// <summary>
    /// Полный цикл публикации: исправление кавычек, проверка, поднятие версии, слияние, выгрузка
    /// </summary>
    private static async Task<int> PublishAsync(Options options, ConsoleLog log)
    {
        var root = ResolveRoot(options);
        var output = RequireOutput(options);

        using var settings = EnvironmentSettings.Read();
        settings.Value.Validate();

        var localVersion = LanguagesVersionFile.Read(root);
        var remoteVersion = await LanguagesVersionFile.ReadRemoteAsync(settings.Value.StorageApiUrl).ConfigureAwait(false);
        var version = LanguagesVersionFile.GetNextVersion(localVersion, remoteVersion);

        log.Info($"Версия в {LanguageFolder.VersionFileName}: {localVersion}, в хранилище: {remoteVersion}, новая версия: {version}");

        var build = LanguageBuilder.Build(root, output, version, log);
        log.Annotate(build.Check);

        // Версия записывается только после успешной сборки: при непройденной проверке
        // Langs.xml остаётся как был
        LanguagesVersionFile.Write(root, build.Languages, version, log);

        var result = await LanguagePublisher.PublishAsync(root, output, settings.Value, log).ConfigureAwait(false);

        if (!result.StorageUploaded)
            log.Notice("Языковые файлы не выгружены в хранилище — они есть только на серверах, выгрузите их повторно");

        WriteGitHubOutput("version", result.Version.ToString());
        WriteGitHubOutput("storage_uploaded", result.StorageUploaded ? "true" : "false");

        return ExitSuccess;
    }

    /// <summary>
    /// Папка с файлами локализации: <c>--root</c>, иначе <c>LanguageFiles</c> в текущей папке,
    /// иначе текущая папка
    /// </summary>
    private static string ResolveRoot(Options options)
    {
        if (!string.IsNullOrWhiteSpace(options.Root))
            return LanguageFolder.EnsureRoot(options.Root);

        var current = Directory.GetCurrentDirectory();
        var nested = Path.Combine(current, "LanguageFiles");

        return File.Exists(LanguageFolder.GetVersionFilePath(nested)) ? nested : current;
    }

    private static string RequireOutput(Options options) =>
        string.IsNullOrWhiteSpace(options.Output)
            ? throw new LanguageToolsException("Не задана папка для собранных языковых файлов (--output)")
            : Path.GetFullPath(options.Output);

    /// <summary>
    /// Передаёт значение следующим шагам workflow GitHub Actions
    /// </summary>
    private static void WriteGitHubOutput(string name, string value)
    {
        var file = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        if (!string.IsNullOrEmpty(file))
            File.AppendAllText(file, $"{name}={value}{Environment.NewLine}");
    }

    private static bool TryParseOptions(string[] args, out Options options, out string error)
    {
        options = new Options();
        error = null;

        for (var i = 0; i < args.Length; i++)
        {
            var name = args[i].ToLowerInvariant();
            if (name is not ("--root" or "--output"))
            {
                error = $"Неизвестный параметр: {args[i]}";
                return false;
            }

            if (i + 1 >= args.Length)
            {
                error = $"Не задано значение параметра {args[i]}";
                return false;
            }

            var value = args[++i];
            if (name == "--root")
                options.Root = value;
            else
                options.Output = value;
        }

        return true;
    }

    private static int UnknownCommand(string command, ConsoleLog log)
    {
        log.Fail($"Неизвестная команда: {command}");
        PrintUsage();
        return ExitUsage;
    }

    private static bool IsHelp(string argument) => argument is "--help" or "-h" or "/?" or "help";

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            ModPlus.LanguageTools.Cli — файлы локализации ModPlus.

            Использование:
              check   [--root <папка>]
                  проверить файлы локализации (файлы не изменяются);
                  код возврата 1 при ошибках, предупреждения публикацию не блокируют
              build   --output <папка> [--root <папка>]
                  исправить кавычки в исходных файлах, проверить и собрать языковые файлы
                  с версией из Langs.xml (версия не меняется, ничего не выгружается)
              publish --output <папка> [--root <папка>]
                  исправить кавычки, проверить, поднять версию (вторая часть большей из версий
                  в Langs.xml и в хранилище +1), собрать, записать версию в Langs.xml и выгрузить
                  языковые файлы и Langs.xml на серверы и в хранилище

            --root    папка с файлами локализации (подпапки языков и Langs.xml);
                      по умолчанию LanguageFiles в текущей папке, иначе текущая папка
            --output  папка для собранных языковых файлов

            Параметры выгрузки (переменные окружения):
              MODPLUS_SITE_SFTP_HOST, MODPLUS_SITE_SFTP_PORT (22), MODPLUS_SITE_SFTP_USER (root),
              MODPLUS_SITE_SFTP_PRIVATE_KEY (содержимое ключа) или MODPLUS_SITE_SFTP_PRIVATE_KEY_FILE,
              MODPLUS_SITE_ROOT_PATH,
              MODPLUS_SERVICE_SFTP_HOST, MODPLUS_SERVICE_SFTP_PORT (22), MODPLUS_SERVICE_SFTP_USER (root),
              MODPLUS_SERVICE_SFTP_PASSWORD,
              MODPLUS_SERVICE_SFTP_PRIVATE_KEY или MODPLUS_SERVICE_SFTP_PRIVATE_KEY_FILE (необязательно),
              MODPLUS_SERVICE_DOWNLOADS_PATH,
              MODPLUS_STORAGE_SERVICE_URL (https://s3.yandexcloud.net), MODPLUS_STORAGE_BUCKET (storage.modplus.org),
              MODPLUS_STORAGE_ACCESS_KEY, MODPLUS_STORAGE_SECRET_KEY,
              MODPLUS_STORAGE_API_URL (https://storage.modplus.org/)

            Коды возврата: 0 — успех, 1 — ошибка, 2 — неверные аргументы.
            """);
    }

    private static void TryUseUtf8Console()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Перенаправленный вывод может не поддерживать смену кодировки — это не ошибка
        }
    }

    /// <summary>
    /// Параметры командной строки
    /// </summary>
    private sealed class Options
    {
        public string Root { get; set; }

        public string Output { get; set; }
    }
}
