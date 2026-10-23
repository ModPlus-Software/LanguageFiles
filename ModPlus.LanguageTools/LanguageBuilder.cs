namespace ModPlus.LanguageTools;

using Checks;

/// <summary>
/// Полная сборка языковых файлов: исправление кавычек в исходных файлах, проверка, слияние.
/// </summary>
public static class LanguageBuilder
{
    /// <summary>
    /// Собирает языковые файлы
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="outputDirectory">Папка назначения собранных файлов</param>
    /// <param name="version">Версия, записываемая в собранные файлы</param>
    /// <param name="log">Лог или <c>null</c></param>
    /// <exception cref="LanguageToolsException">Проверка не пройдена или сборка не удалась</exception>
    public static BuildResult Build(string root, string outputDirectory, Version version, ILanguageToolsLog log = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new LanguageToolsException("Не задана папка для собранных языковых файлов");

        root = LanguageFolder.EnsureRoot(root);
        var languages = LanguageFolder.GetLanguageNames(root, log);

        var fixedFiles = QuoteNormalizer.FixFiles(root, languages, log);

        var check = LocalizationChecker.Check(root, log);
        var errors = check.Errors.Count();
        if (errors > 0)
        {
            throw new LanguageToolsException($"Проверка файлов локализации не пройдена: ошибок — {errors}. Сборка отменена")
            {
                CheckResult = check,
            };
        }

        var files = LanguageMerger.Merge(root, check.Languages, outputDirectory, version, log);

        return new BuildResult(check.Languages, files, fixedFiles, check);
    }
}

/// <summary>
/// Результат сборки языковых файлов
/// </summary>
/// <param name="Languages">Языки</param>
/// <param name="Files">Собранные языковые файлы</param>
/// <param name="QuoteFixedFiles">Исходные файлы, в которых исправлены кавычки</param>
/// <param name="Check">Результат проверки (ошибок нет, могут быть предупреждения)</param>
public sealed record BuildResult(
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> QuoteFixedFiles,
    CheckResult Check);
