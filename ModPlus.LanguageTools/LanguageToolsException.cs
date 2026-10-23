namespace ModPlus.LanguageTools;

using Checks;

/// <summary>
/// Ошибка операции с файлами локализации, которую нужно показать пользователю как есть
/// (без стека вызовов): не найдена папка, не пройдена проверка, расходятся версии и т.п.
/// </summary>
public sealed class LanguageToolsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageToolsException"/> class.
    /// </summary>
    /// <param name="message">Текст ошибки</param>
    public LanguageToolsException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageToolsException"/> class.
    /// </summary>
    /// <param name="message">Текст ошибки</param>
    /// <param name="innerException">Исходное исключение</param>
    public LanguageToolsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Результат проверки, если ошибка вызвана непройденной проверкой файлов локализации
    /// </summary>
    public CheckResult CheckResult { get; init; }
}
