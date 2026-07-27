namespace LangFilesEditor.Core.Abstractions;

/// <summary>
/// Фоновые операции с отображением прогресса в status bar и безопасным возвратом на UI-поток.
/// </summary>
public interface IBackgroundOperationService
{
    // todo:  Вообще странным выглядит максимально. Почему мутации связаны с UI...
    /// <summary>
    /// Выполняет работу в фоне; мутации UI и моделей — только в <paramref name="applyOnUiThread"/>.
    /// </summary>
    /// <param name="progressMessage">Сообщение прогресса для status bar на время выполнения операции.</param>
    /// <param name="backgroundWork">Асинхронная работа, выполняемая вне UI-потока.</param>
    /// <param name="applyOnUiThread">Действие применения результата на UI-потоке после завершения фоновой работы.</param>
    /// <param name="cancellationToken">Токен отмены фоновой операции.</param>
    /// <returns>Задача, завершающаяся после выполнения фоновой работы и применения результата.</returns>
    Task RunAsync(
        string progressMessage,
        Func<CancellationToken, Task> backgroundWork,
        Func<Task> applyOnUiThread,
        CancellationToken cancellationToken = default);

    // todo: не понимаю, раз такое отличие, то зачем в обоих applyOnUiThread.
    /// <summary>
    /// То же, что и базовый <see cref="RunAsync(string, Func{CancellationToken, Task}, Func{Task}, CancellationToken)"/>,
    /// но передаёт в фоновую работу <see cref="IEditorOperationProgress"/> для отправки прогресса и смены заголовка.
    /// </summary>
    /// <param name="progressTitle">Начальный заголовок операции для status bar.</param>
    /// <param name="backgroundWork">Асинхронная работа в фоне; получает репортер прогресса и токен отмены.</param>
    /// <param name="applyOnUiThread">Применение результата на UI-потоке после фоновой работы.</param>
    /// <param name="cancellationToken">Токен отмены фоновой операции.</param>
    /// <returns>Задача, завершающаяся после выполнения фоновой работы и применения результата.</returns>
    Task RunAsync(
        string progressTitle,
        Func<IEditorOperationProgress, CancellationToken, Task> backgroundWork,
        Func<Task> applyOnUiThread,
        CancellationToken cancellationToken = default);
}