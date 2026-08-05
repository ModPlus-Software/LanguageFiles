namespace LangFilesEditor.Services.Loggers;

using System.Collections.Concurrent;
using System.IO;
using Utils;

/// <summary>
/// Файловый логгер: пишет строки с отметкой времени в лог-файл в отдельном фоновом потоке,
/// чтобы не блокировать основной поток. Каталог логов находится под .gitignore и создаётся при отсутствии.
/// </summary>
public class Logger
{
    private static readonly BlockingCollection<string> Queue = new();
    private static readonly Lazy<string> LogFilePath = new(CreateLogFilePath);

    static Logger()
    {
        var writer = new Thread(WriteQueuedLines)
        {
            IsBackground = true,
            Name = "LangFilesEditor.Logger",
        };

        writer.Start();
    }

    /// <summary>
    /// Записывает одну строку в лог-файл.
    /// </summary>
    /// <param name="message">Сообщение для записи.</param>
    public void Log(string message) => Log([message]);

    /// <summary>
    /// Записывает несколько строк в лог-файл.
    /// </summary>
    /// <param name="messages">Сообщения для записи.</param>
    public void Log(params string[] messages) => Log((IEnumerable<string>)messages);

    /// <summary>
    /// Записывает перечисление строк в лог-файл (каждая со своей отметкой времени).
    /// </summary>
    /// <param name="messages">Коллекция сообщений для записи.</param>
    public void Log(IEnumerable<string> messages)
    {
        if (messages == null)
        {
            return;
        }

        foreach (var message in messages)
        {
            Queue.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{message}");
        }
    }

    private static void WriteQueuedLines()
    {
        var batch = new List<string>();
        foreach (var line in Queue.GetConsumingEnumerable())
        {
            // Накопленные строки пишутся одним открытием файла: при пакетных уведомлениях
            // (слияние, сканирование) построчная запись открывала бы файл сотни раз подряд.
            batch.Add(line);
            while (Queue.TryTake(out var queued))
            {
                batch.Add(queued);
            }

            var path = LogFilePath.Value;
            if (path != null)
            {
                try
                {
                    File.AppendAllLines(path, batch);
                }
                catch
                {
                    // Логирование не должно ронять приложение при недоступности файла.
                }
            }

            batch.Clear();
        }
    }

    private static string CreateLogFilePath()
    {
        try
        {
            var logDirectory = Path.Combine(DirectoryUtils.GetSolutionDirectory(), "logs");
            Directory.CreateDirectory(logDirectory);
            return Path.Combine(logDirectory, $"editor_{DateTime.Now:yyyyMMdd}.log");
        }
        catch
        {
            return null;
        }
    }
}