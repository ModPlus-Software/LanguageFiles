namespace ModPlus.LanguageTools.Checks;

/// <summary>
/// Важность проблемы, найденной проверкой
/// </summary>
public enum CheckSeverity
{
    /// <summary>
    /// Ошибка: сборка и публикация невозможны
    /// </summary>
    Error,

    /// <summary>
    /// Предупреждение: только выводится, сборку и публикацию не блокирует
    /// </summary>
    Warning,
}

/// <summary>
/// Проблема, найденная проверкой файлов локализации
/// </summary>
/// <param name="Severity">Важность</param>
/// <param name="Category">Раздел проверки</param>
/// <param name="Message">Описание проблемы</param>
/// <param name="File">Полный путь к файлу, к которому относится проблема, или <c>null</c></param>
/// <param name="Line">Номер строки в файле или <c>null</c></param>
public sealed record CheckIssue(
    CheckSeverity Severity, string Category, string Message, string File = null, int? Line = null);

/// <summary>
/// Результат проверки файлов локализации
/// </summary>
/// <param name="Languages">Проверенные языки</param>
/// <param name="Issues">Найденные проблемы</param>
public sealed record CheckResult(IReadOnlyList<string> Languages, IReadOnlyList<CheckIssue> Issues)
{
    /// <summary>
    /// Ошибки
    /// </summary>
    public IEnumerable<CheckIssue> Errors => Issues.Where(issue => issue.Severity == CheckSeverity.Error);

    /// <summary>
    /// Предупреждения
    /// </summary>
    public IEnumerable<CheckIssue> Warnings => Issues.Where(issue => issue.Severity == CheckSeverity.Warning);

    /// <summary>
    /// Есть ли ошибки
    /// </summary>
    public bool HasErrors => Errors.Any();
}
