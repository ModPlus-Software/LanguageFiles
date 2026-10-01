namespace LangFilesEditor.Services.Loggers;

/// <summary>
/// Публикация сообщений о ходе длительных операций: одновременно в UI (через <see cref="OnNotify"/>)
/// и в файловый лог.
/// </summary>
public class NotificationService
{
    private readonly Logger _logger = new();

    /// <summary>
    /// Событие с новыми сообщениями для отображения в UI.
    /// </summary>
    public event Action<IEnumerable<string>> OnNotify;

    /// <summary>
    /// Событие с сообщениями об ошибках. В отличие от <see cref="OnNotify"/> не содержит
    /// прогресс-сообщений — используется headless-потребителями, которым нужен только сбой.
    /// </summary>
    public event Action<IEnumerable<string>> OnError;

    /// <summary>
    /// Публикует одно сообщение.
    /// </summary>
    /// <param name="message">Сообщение для показа и записи в лог.</param>
    public void Notify(string message) => Notify([message]);

    /// <summary>
    /// Публикует одно сообщение об ошибке: попадает и в <see cref="OnNotify"/>, и в <see cref="OnError"/>.
    /// </summary>
    /// <param name="message">Текст ошибки.</param>
    public void NotifyError(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Notify(message);
        OnError?.Invoke([message]);
    }

    /// <summary>
    /// Публикует несколько сообщений.
    /// </summary>
    /// <param name="messages">Сообщения для показа и записи в лог.</param>
    public void Notify(params string[] messages) => Notify((IEnumerable<string>)messages);

    /// <summary>
    /// Публикует перечисление сообщений: и в UI, и в файловый лог (по одной строке).
    /// </summary>
    /// <param name="messages">Коллекция сообщений.</param>
    public void Notify(IEnumerable<string> messages)
    {
        if (messages == null)
        {
            return;
        }

        foreach (var message in messages)
        {
            if (string.IsNullOrEmpty(message))
            {
                continue;
            }

            _logger.Log(message);
            OnNotify?.Invoke([message]);
        }
    }
}