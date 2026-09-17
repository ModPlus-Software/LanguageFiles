namespace LangFilesEditor.Cli;

using System.IO;
using System.Text;
using LangFilesEditor.Core.Abstractions;
using LangFilesEditor.Exceptions;
using LangFilesEditor.Models;
using LangFilesEditor.Services;
using LangFilesEditor.Services.Loggers;
using LangFilesEditor.Services.RepositoryServices;
using Microsoft.Win32;

/// <summary>
/// Консольная утилита для слияния и проверки файлов локализации. Использует те же сервисы,
/// что и графический редактор, и запускается из каталога своей сборки: каталог LanguageFiles
/// находится автоматически, целевой каталог слияния берётся из реестра ModPlus.
/// </summary>
internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;
    private const int ExitUsage = 2;

    private static int Main(string[] args)
    {
        TryUseUtf8Console();

        if (args.Length == 0)
        {
            PrintUsage();
            return ExitUsage;
        }

        if (IsHelp(args[0]))
        {
            PrintUsage();
            return ExitSuccess;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "--check" => RunCheckCommand(args),
                "--merge" => RunMerge(),
                _ => PrintUnknownCommand(args[0]),
            };
        }
        catch (CriticalException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return ExitFailure;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Ошибка: {exception.Message}");
            return ExitFailure;
        }
    }

    /// <summary>
    /// Разбирает аргументы команды <c>--check</c>: <c>[модуль] [all|errors]</c>.
    /// </summary>
    private static int RunCheckCommand(string[] args)
    {
        var options = args.Skip(1).ToArray();
        var showAll = false;
        if (options.Length > 0 && TryParseOutputMode(options[^1], out var mode))
        {
            showAll = mode;
            options = options[..^1];
        }

        if (options.Length > 1)
        {
            Console.Error.WriteLine("Для --check допускается только один модуль.");
            PrintUsage();
            return ExitUsage;
        }

        return RunCheck(options.Length == 1 ? options[0] : null, showAll);
    }

    /// <summary>
    /// Проверяет локализации по тем же правилам, что и редактор: пустое имя, имя с цифры,
    /// пустые значения, дубликаты имён (ошибки) и дубликаты наборов значений (предупреждения).
    /// </summary>
    /// <param name="moduleFilter">Имя модуля для проверки или <c>null</c> — проверить все модули.</param>
    /// <param name="showAll"><c>true</c> — выводить и ошибки, и предупреждения; <c>false</c> — только ошибки.</param>
    private static int RunCheck(string moduleFilter, bool showAll)
    {
        ILanguageRepository repository = new LanguageRepositoryService();
        var root = Constants.LanguageFilesDirectory;
        var languages = repository.LoadLanguages(root);

        Console.WriteLine($"Каталог локализации: {root}");
        if (languages.Count == 0)
        {
            Console.WriteLine("В каталоге LanguageFiles не найдено ни одного языкового каталога.");
            return ExitFailure;
        }

        Console.WriteLine($"Языки: {string.Join(", ", languages)}");
        Console.WriteLine($"Модуль: {moduleFilter ?? "все"}");
        Console.WriteLine();

        var domains = repository.LoadDomains(root, languages);
        var validator = new Validator();
        var totalErrors = 0;
        var totalWarnings = 0;
        var modulesWithIssues = 0;
        var matchedModules = 0;

        foreach (var domain in domains)
        {
            var modules = repository.LoadModulesAsync(domain).GetAwaiter().GetResult();
            foreach (var module in modules)
            {
                if (moduleFilter != null &&
                    !string.Equals(module.Name, moduleFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matchedModules++;
                var data = repository.ReadTranslationEntriesAsync(module, languages).GetAwaiter().GetResult();
                var metadata = data.Metadata.ToList();
                var items = data.Items.ToList();
                validator.ValidateAttributes(metadata);
                validator.ValidateItems(items);

                var errorLines = new List<string>();
                var warningLines = new List<string>();
                var errors = CollectIssues("атрибут", metadata, errorLines, warningLines);
                errors += CollectIssues("строка", items, errorLines, warningLines);
                var warnings = CountWarnings(metadata) + CountWarnings(items);

                // В режиме только ошибок модули без ошибок не показываются, даже если у них есть предупреждения.
                if (errors == 0 && (!showAll || warnings == 0))
                {
                    continue;
                }

                modulesWithIssues++;
                totalErrors += errors;
                totalWarnings += warnings;
                Console.WriteLine($"Домен: {domain.Name}  Модуль: {module.Name}");
                foreach (var line in errorLines)
                {
                    Console.WriteLine(line);
                }

                if (showAll)
                {
                    foreach (var line in warningLines)
                    {
                        Console.WriteLine(line);
                    }
                }

                Console.WriteLine();
            }
        }

        if (moduleFilter != null && matchedModules == 0)
        {
            Console.WriteLine($"Модуль «{moduleFilter}» не найден.");
            return ExitFailure;
        }

        var warningsSummary = showAll ? $", предупреждений — {totalWarnings}" : string.Empty;
        Console.WriteLine($"Итог: модулей с проблемами — {modulesWithIssues}, ошибок — {totalErrors}{warningsSummary}.");
        return totalErrors > 0 ? ExitFailure : ExitSuccess;
    }

    /// <summary>
    /// Выполняет слияние LanguageFiles в каталог установленного ModPlus. При успехе ничего не выводит:
    /// наружу отдаётся только код возврата. В случае сбоя печатает ошибки в stderr и возвращает 1.
    /// </summary>
    private static int RunMerge()
    {
        if (TryGetMergeTarget(out _, out var error) is false)
        {
            Console.Error.WriteLine(error);
            return ExitFailure;
        }

        var failures = new List<string>();
        var notifications = new NotificationService();
        notifications.OnError += messages => failures.AddRange(
            messages.Where(message => !string.IsNullOrEmpty(message)));

        var repository = new LanguageRepositoryService();
        repository.MergeWithWorkingDirectory(notifications);

        if (failures.Count == 0)
        {
            return ExitSuccess;
        }

        foreach (var failure in failures)
        {
            Console.Error.WriteLine(failure);
        }

        return ExitFailure;
    }

    /// <summary>
    /// Проверяет, что локальная версия читается, а установленный ModPlus доступен, и возвращает
    /// каталог Languages для слияния (та же логика, что у графического редактора).
    /// </summary>
    private static bool TryGetMergeTarget(out string target, out string error)
    {
        target = null;
        error = null;

        var versionFile = Path.Combine(Constants.LanguageFilesDirectory, "Version.txt");
        if (!File.Exists(versionFile) ||
            !Version.TryParse(File.ReadAllText(versionFile).Trim(), out _))
        {
            error = "Не удалось прочитать локальную версию локализации (Version.txt).";
            return false;
        }

        var topDir = Registry.CurrentUser.OpenSubKey("Software\\ModPlus")?.GetValue("TopDir")?.ToString();
        if (string.IsNullOrEmpty(topDir) || !Directory.Exists(topDir))
        {
            error = "Installed ModPlus not found!";
            return false;
        }

        target = Path.Combine(topDir, "Languages");
        return true;
    }

    /// <summary>
    /// Собирает строки отчёта по записям: ошибки — в <paramref name="errorLines"/>,
    /// предупреждения — в <paramref name="warningLines"/>. Возвращает число ошибок.
    /// </summary>
    private static int CollectIssues(
        string kind,
        IEnumerable<TranslationEntry> entries,
        List<string> errorLines,
        List<string> warningLines)
    {
        var errors = 0;
        foreach (var entry in entries)
        {
            var reasons = new List<string>();

            if (string.IsNullOrEmpty(entry.Name))
            {
                reasons.Add("пустое имя ключа");
            }
            else if (char.IsDigit(entry.Name[0]))
            {
                reasons.Add("имя ключа не должно начинаться с цифры");
            }

            var emptyLanguages = entry.Values
                .Where(pair => pair.Value == null || string.IsNullOrEmpty(pair.Value.Value))
                .Select(pair => pair.Key)
                .ToList();
            if (emptyLanguages.Count > 0)
            {
                reasons.Add($"пустое значение перевода ({string.Join(", ", emptyLanguages)})");
            }

            if (entry.DiagnosticState.HasDuplicateName)
            {
                reasons.Add("дубликат имени ключа");
            }

            var hasError = reasons.Count > 0;
            var hasWarning = entry.DiagnosticState.HasDuplicateValue;
            if (!hasError && !hasWarning)
            {
                continue;
            }

            var name = string.IsNullOrEmpty(entry.Name) ? "<без имени>" : entry.Name;
            if (hasError)
            {
                // Запись с ошибкой показывается один раз, даже если у неё есть и предупреждение:
                // дубликат набора значений в этом случае только добавляется в общий счётчик.
                errors++;
                errorLines.Add($"  [ошибка] ({kind}) {name}: {string.Join("; ", reasons)}");
            }
            else
            {
                warningLines.Add($"  [предупреждение] ({kind}) {name}: дубликат набора значений перевода");
            }
        }

        return errors;
    }

    // Правила совпадают с редактором: ошибка и предупреждение считаются независимо,
    // поэтому запись с обоими флагами попадает в оба счётчика.
    private static int CountWarnings(IEnumerable<TranslationEntry> entries) =>
        entries.Count(entry => entry.DiagnosticState.HasDuplicateValue);

    /// <summary>
    /// Разбирает режим вывода: <c>all</c> — ошибки и предупреждения, <c>errors</c> — только ошибки.
    /// </summary>
    private static bool TryParseOutputMode(string argument, out bool showAll)
    {
        if (string.Equals(argument, "all", StringComparison.OrdinalIgnoreCase))
        {
            showAll = true;
            return true;
        }

        if (string.Equals(argument, "errors", StringComparison.OrdinalIgnoreCase))
        {
            showAll = false;
            return true;
        }

        showAll = false;
        return false;
    }

    private static bool IsHelp(string argument) =>
        argument is "--help" or "-h" or "/?";

    private static int PrintUnknownCommand(string argument)
    {
        Console.Error.WriteLine($"Неизвестный аргумент: {argument}");
        PrintUsage();
        return ExitUsage;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("LangFilesEditor.Cli — консольная утилита локализации ModPlus.");
        Console.WriteLine();
        Console.WriteLine("Использование:");
        Console.WriteLine("  LangFilesEditor.Cli --check [модуль] [all|errors]");
        Console.WriteLine("      проверить локализации (код возврата 1 при ошибках);");
        Console.WriteLine("      без модуля — все модули; all — ошибки и предупреждения, errors — только ошибки (по умолчанию)");
        Console.WriteLine("  LangFilesEditor.Cli --merge   выполнить слияние в каталог установленного ModPlus");
        Console.WriteLine("  LangFilesEditor.Cli --help    показать эту справку");
    }

    private static void TryUseUtf8Console()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Перенаправленный вывод может не поддерживать смену кодировки — это не ошибка.
        }
    }
}
