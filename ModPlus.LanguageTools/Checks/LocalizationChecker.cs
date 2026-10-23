namespace ModPlus.LanguageTools.Checks;

/// <summary>
/// Все проверки файлов локализации.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>формат xml — каждый файл должен читаться;</item>
/// <item>состав — одинаковые файлы, узлы, под-узлы и атрибуты во всех языках (<see cref="StructureCheck"/>);</item>
/// <item>записи — правила редактора локализации (<see cref="EntryRules"/>, <see cref="EntriesCheck"/>);</item>
/// <item>кавычки — типографские кавычки отмечаются предупреждением (исправляются при сборке).</item>
/// </list>
/// Ошибки блокируют сборку и публикацию, предупреждения только выводятся.
/// </remarks>
public static class LocalizationChecker
{
    /// <summary>
    /// Раздел проверки кавычек
    /// </summary>
    public const string QuotesCategory = "Кавычки";

    /// <summary>
    /// Проверяет файлы локализации
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="log">Лог или <c>null</c></param>
    /// <exception cref="LanguageToolsException">Папки нет или в ней нет подпапок языков</exception>
    public static CheckResult Check(string root, ILanguageToolsLog log = null)
    {
        root = LanguageFolder.EnsureRoot(root);
        log?.Info($"Проверка файлов локализации: {root}");

        var languages = LanguageFolder.GetLanguageNames(root, log);
        log?.Info($"Языки: {string.Join(", ", languages)}");

        var sources = SourceSet.Load(root, languages);
        var issues = new List<CheckIssue>(sources.LoadIssues);

        StructureCheck.Run(sources, issues);
        EntriesCheck.Run(sources, issues);

        foreach (var file in QuoteNormalizer.FindFiles(root, languages))
        {
            issues.Add(new CheckIssue(
                CheckSeverity.Warning,
                QuotesCategory,
                $"{Path.GetRelativePath(root, file)}: типографские кавычки — будут заменены на \" при сборке",
                file));
        }

        var result = new CheckResult(languages, issues);
        if (log != null)
            Report(result, log);

        return result;
    }

    /// <summary>
    /// Выводит в лог итог проверки и список проблем по разделам: сначала ошибки, затем предупреждения
    /// </summary>
    /// <param name="result">Результат проверки</param>
    /// <param name="log">Лог</param>
    public static void Report(CheckResult result, ILanguageToolsLog log)
    {
        foreach (var group in result.Errors.GroupBy(issue => issue.Category))
        {
            log.Error($"{group.Key}:");
            foreach (var issue in group)
                log.Error($"    {issue.Message}");
        }

        foreach (var group in result.Warnings.GroupBy(issue => issue.Category))
        {
            log.Warning($"{group.Key}:");
            foreach (var issue in group)
                log.Warning($"    {issue.Message}");
        }

        var errors = result.Errors.Count();
        var warnings = result.Warnings.Count();
        var summary = $"ошибок — {errors}, предупреждений — {warnings}";

        if (errors > 0)
            log.Error($"Проверка не пройдена: {summary}");
        else
            log.Info($"Проверка пройдена: {summary}");
    }
}
