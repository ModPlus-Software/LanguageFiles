namespace ModPlus.LanguageTools;

/// <summary>
/// Приёмник лога операций с файлами локализации.
/// </summary>
/// <remarks>
/// Библиотека ничего не знает о том, где выводится лог: в консоль утилиты,
/// в окно слияния редактора или в лог программы сборки.
/// </remarks>
public interface ILanguageToolsLog
{
    /// <summary>
    /// Информационное сообщение
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    void Info(string message);

    /// <summary>
    /// Предупреждение: на результат не влияет, но требует внимания
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    void Warning(string message);

    /// <summary>
    /// Ошибка
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    void Error(string message);
}

/// <summary>
/// Лог, который ничего не выводит
/// </summary>
public sealed class NullLanguageToolsLog : ILanguageToolsLog
{
    /// <summary>
    /// Единственный экземпляр
    /// </summary>
    public static readonly NullLanguageToolsLog Instance = new();

    private NullLanguageToolsLog()
    {
    }

    /// <inheritdoc />
    public void Info(string message)
    {
    }

    /// <inheritdoc />
    public void Warning(string message)
    {
    }

    /// <inheritdoc />
    public void Error(string message)
    {
    }
}
