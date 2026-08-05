namespace LangFilesEditor.Services;

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Core.Abstractions;

/// <summary>
/// Трекер длительных операций редактора: хранит список активных операций, их суммарный прогресс
/// и последнее краткое сообщение. Владелец состояния прогресса на всё приложение — status bar
/// биндится к нему напрямую.
/// </summary>
public sealed class EditorOperationTracker
{
    private readonly ObservableCollection<IEditorOperation> _operations = [];
    private readonly Dictionary<string, EditorOperation> _byKey = new(StringComparer.Ordinal);
    private string _transientMessage = string.Empty;
    private int _autoKey;

    /// <summary>
    /// Создаёт трекер с пустым набором операций.
    /// </summary>
    public EditorOperationTracker()
    {
        Operations = new ReadOnlyObservableCollection<IEditorOperation>(_operations);
    }

    /// <summary>
    /// Активные операции в порядке их начала.
    /// </summary>
    public ReadOnlyObservableCollection<IEditorOperation> Operations { get; }

    /// <summary>
    /// Выполняется ли хотя бы одна операция.
    /// </summary>
    public bool IsActive => _operations.Count > 0;

    /// <summary>
    /// Число активных операций.
    /// </summary>
    public int ActiveCount => _operations.Count;

    /// <summary>
    /// Неопределён ли общий прогресс (ни у одной активной операции нет известного объёма).
    /// </summary>
    public bool IsOverallIndeterminate => _operations.Count > 0 && _operations.All(o => o.Total <= 0);

    /// <summary>
    /// Общая доля выполнения всех операций с известным объёмом — от 0 до 1.
    /// </summary>
    public double OverallProgress
    {
        get
        {
            long current = 0;
            long total = 0;
            foreach (var operation in _operations)
            {
                if (operation.Total <= 0)
                {
                    continue;
                }

                current += operation.Current;
                total += operation.Total;
            }

            return total > 0 ? Math.Clamp((double)current / total, 0, 1) : 0;
        }
    }

    /// <summary>
    /// Последнее краткое сообщение для status bar; сбрасывается при начале новой операции.
    /// </summary>
    public string TransientMessage => _transientMessage;

    /// <summary>
    /// Вызывается при изменении состава операций, прогресса или сообщений.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Начинает операцию или присоединяется к существующей с тем же ключом.
    /// </summary>
    /// <param name="title">Заголовок операции для status bar.</param>
    /// <param name="key">Ключ операции; <see langword="null"/> — уникальная операция без переиспользования.</param>
    /// <param name="total">Ожидаемое общее число единиц работы; 0 — неизвестно.</param>
    /// <returns>Описатель операции, который нужно передать в <see cref="End"/>.</returns>
    public EditorOperation Begin(string title, string? key = null, int total = 0)
    {
        EditorOperation? result = null;
        RunOnUi(() =>
        {
            if (key != null && _byKey.TryGetValue(key, out var existing))
            {
                existing.RefCount++;
                existing.Retitle(title);
                if (total > 0)
                {
                    existing.Report(existing.Current, total);
                }

                result = existing;
                Changed?.Invoke();
                return;
            }

            var operationKey = key ?? $"__auto_{++_autoKey}";
            var operation = new EditorOperation(operationKey, title, total);
            _byKey[operationKey] = operation;
            _operations.Add(operation);
            _transientMessage = string.Empty;
            result = operation;
            Changed?.Invoke();
        });

        return result!;
    }

    /// <summary>
    /// Обновляет прогресс операции.
    /// </summary>
    /// <param name="operation">Операция, полученная из <see cref="Begin"/>.</param>
    /// <param name="current">Число выполненных единиц работы.</param>
    /// <param name="total">Общее число единиц работы; неположительное значение оставляет прежнее.</param>
    public void Report(EditorOperation operation, int current, int total = 0)
    {
        if (operation == null)
        {
            return;
        }

        PostToUi(() =>
        {
            operation.Report(current, total);
            Changed?.Invoke();
        });
    }

    /// <summary>
    /// Обновляет прогресс операции по её ключу, если такая активна.
    /// </summary>
    /// <param name="key">Ключ операции.</param>
    /// <param name="current">Число выполненных единиц работы.</param>
    /// <param name="total">Общее число единиц работы; неположительное значение оставляет прежнее.</param>
    public void ReportByKey(string key, int current, int total = 0)
    {
        if (key == null)
        {
            return;
        }

        PostToUi(() =>
        {
            if (!_byKey.TryGetValue(key, out var operation))
            {
                return;
            }

            operation.Report(current, total);
            Changed?.Invoke();
        });
    }

    /// <summary>
    /// Меняет заголовок активной операции.
    /// </summary>
    /// <param name="operation">Операция, полученная из <see cref="Begin"/>.</param>
    /// <param name="title">Новый заголовок.</param>
    public void SetTitle(EditorOperation operation, string title)
    {
        if (operation == null)
        {
            return;
        }

        RunOnUi(() =>
        {
            operation.Retitle(title);
            Changed?.Invoke();
        });
    }

    /// <summary>
    /// Публикует краткое сообщение.
    /// </summary>
    /// <param name="message">Текст транзиентного сообщения.</param>
    public void PublishTransient(string message)
    {
        PostToUi(() =>
        {
            _transientMessage = message ?? string.Empty;
            Changed?.Invoke();
        });
    }

    /// <summary>
    /// Завершает одно удержание операции; удаляет её из списка при достижении нуля.
    /// </summary>
    /// <param name="operation">Операция, полученная из <see cref="Begin"/>.</param>
    public void End(EditorOperation operation)
    {
        if (operation == null)
        {
            return;
        }

        RunOnUi(() =>
        {
            if (--operation.RefCount > 0)
            {
                return;
            }

            _byKey.Remove(operation.Key);
            _operations.Remove(operation);
            Changed?.Invoke();
        });
    }

    private static void RunOnUi(Action action) =>
        Utils.DispatcherExtensions.RunOnUiThread(Application.Current?.Dispatcher, action, DispatcherPriority.Background);

    /// <summary>
    /// Ставит обновление в очередь UI-потока, не дожидаясь его выполнения. Прогресс приходит из
    /// параллельных фоновых задач; синхронный <see cref="Dispatcher.Invoke(Action, DispatcherPriority)"/>
    /// на каждое сообщение выстраивал бы рабочие потоки в очередь к UI-потоку.
    /// </summary>
    private static void PostToUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.InvokeAsync(action, DispatcherPriority.Background);
    }
}