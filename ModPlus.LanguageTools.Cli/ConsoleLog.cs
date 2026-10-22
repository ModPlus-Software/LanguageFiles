namespace ModPlus.LanguageTools.Cli;

using Checks;

/// <summary>
/// Вывод лога в консоль. При запуске в GitHub Actions ошибки и предупреждения дополнительно
/// оформляются командами <c>::error::</c> / <c>::warning::</c>: они попадают в сводку запуска,
/// а проблемы проверки с указанием файла и строки — прямо в файлы pull request.
/// </summary>
internal sealed class ConsoleLog : ILanguageToolsLog
{
    private readonly bool _gitHubActions =
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

    private readonly string _workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");

    /// <inheritdoc />
    public void Info(string message) => Console.WriteLine(message);

    /// <inheritdoc />
    public void Warning(string message) => Console.WriteLine(message);

    /// <inheritdoc />
    public void Error(string message) => Console.Error.WriteLine(message);

    /// <summary>
    /// Сообщение об ошибке выполнения команды
    /// </summary>
    /// <param name="message">Текст</param>
    public void Fail(string message)
    {
        Console.Error.WriteLine(message);
        if (_gitHubActions)
            Console.WriteLine($"::error::{Escape(message)}");
    }

    /// <summary>
    /// Предупреждение, которое нужно показать в сводке запуска GitHub
    /// </summary>
    /// <param name="message">Текст</param>
    public void Notice(string message)
    {
        Console.WriteLine(message);
        if (_gitHubActions)
            Console.WriteLine($"::warning::{Escape(message)}");
    }

    /// <summary>
    /// В GitHub Actions выводит проблемы проверки командами с указанием файла и строки
    /// (в обычном выводе они уже есть — их печатает сама проверка)
    /// </summary>
    /// <param name="result">Результат проверки</param>
    public void Annotate(CheckResult result)
    {
        if (!_gitHubActions)
            return;

        foreach (var issue in result.Issues)
        {
            var command = issue.Severity == CheckSeverity.Error ? "error" : "warning";
            var properties = new List<string>();

            var file = ToWorkspacePath(issue.File);
            if (file != null)
                properties.Add($"file={EscapeProperty(file)}");
            if (issue.Line != null)
                properties.Add($"line={issue.Line}");
            properties.Add($"title={EscapeProperty(issue.Category)}");

            Console.WriteLine($"::{command} {string.Join(",", properties)}::{Escape(issue.Message)}");
        }
    }

    private string ToWorkspacePath(string file)
    {
        if (string.IsNullOrEmpty(file))
            return null;

        var basePath = string.IsNullOrEmpty(_workspace) ? Directory.GetCurrentDirectory() : _workspace;
        return Path.GetRelativePath(basePath, file).Replace('\\', '/');
    }

    private static string Escape(string value) =>
        value.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A");

    private static string EscapeProperty(string value) =>
        Escape(value).Replace(":", "%3A").Replace(",", "%2C");
}
