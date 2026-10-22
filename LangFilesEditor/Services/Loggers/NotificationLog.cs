namespace LangFilesEditor.Services.Loggers;

using ModPlus.LanguageTools;

/// <summary>
/// Вывод лога операций библиотеки ModPlus.LanguageTools (сборка, проверка) через <see cref="NotificationService"/>.
/// </summary>
/// <remarks>
/// В режиме «только ошибки» информационные сообщения и предупреждения не выводятся.
/// </remarks>
public sealed class NotificationLog : ILanguageToolsLog
{
    private readonly NotificationService _notifications;
    private readonly bool _errorsOnly;

    /// <summary>
    /// Создаёт адаптер лога.
    /// </summary>
    /// <param name="notifications">Сервис уведомлений.</param>
    /// <param name="errorsOnly">Выводить только ошибки.</param>
    public NotificationLog(NotificationService notifications, bool errorsOnly = false)
    {
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _errorsOnly = errorsOnly;
    }

    /// <inheritdoc />
    public void Info(string message)
    {
        if (!_errorsOnly)
        {
            _notifications.Notify(message);
        }
    }

    /// <inheritdoc />
    public void Warning(string message)
    {
        if (!_errorsOnly)
        {
            _notifications.Notify(message);
        }
    }

    /// <inheritdoc />
    public void Error(string message) => _notifications.NotifyError(message);
}
