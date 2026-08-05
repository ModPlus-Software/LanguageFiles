namespace LangFilesEditor.Core.Application;

using Abstractions;
using Services;

/// <summary>
/// Сервис фоновых операций.
/// </summary>
public sealed class BackgroundOperationService : IBackgroundOperationService
{
    private readonly EditorOperationTracker _operations;

    /// <summary>
    /// Создаёт сервис фоновых операций с указанным трекером прогресса.
    /// </summary>
    /// <param name="operations">Трекер, которому передаётся статус операции</param>
    public BackgroundOperationService(EditorOperationTracker operations)
    {
        _operations = operations;
    }

    /// <inheritdoc />
    public Task RunAsync(
        string progressMessage,
        Func<CancellationToken, Task> backgroundWork,
        Func<Task> applyOnUiThread,
        CancellationToken cancellationToken = default) =>
        RunAsync(progressMessage, (_, token) => backgroundWork(token), applyOnUiThread, cancellationToken);

    /// <inheritdoc />
    public async Task RunAsync(
        string progressTitle,
        Func<IEditorOperationProgress, CancellationToken, Task> backgroundWork,
        Func<Task> applyOnUiThread,
        CancellationToken cancellationToken = default)
    {
        var operation = _operations.Begin(progressTitle);
        var progress = new OperationProgress(_operations, operation);
        try
        {
            await backgroundWork(progress, cancellationToken).ConfigureAwait(false);
            await applyOnUiThread().ConfigureAwait(false);
        }
        finally
        {
            _operations.End(operation);
        }
    }

    private sealed class OperationProgress(EditorOperationTracker operations, EditorOperation operation)
        : IEditorOperationProgress
    {
        public void Report(int current, int total) => operations.Report(operation, current, total);

        public void SetTitle(string title) => operations.SetTitle(operation, title);
    }
}