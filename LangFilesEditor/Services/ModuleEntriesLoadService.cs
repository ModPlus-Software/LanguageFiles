namespace LangFilesEditor.Services;

using System.Windows;
using Helpers;
using Models;
using Core.Abstractions;
using Utils;

/// <summary>
/// Загрузка TranslationEntry в модуль с дедупликацией параллельных запросов
/// и возможностью отмены через <see cref="Cancel"/>.
/// </summary>
public sealed class ModuleEntriesLoadService
{
    private readonly ILanguageRepository _repository;
    private readonly EditorOperationTracker _operations;
    private readonly IReadOnlyList<string> _languages;
    private readonly Dictionary<Module, LoadOperation> _inFlight = new();

    /// <summary>
    /// Создаёт сервис загрузки записей модулей.
    /// </summary>
    /// <param name="repository">Репозиторий XML-файлов локализации.</param>
    /// <param name="operations">Трекер операций для status bar.</param>
    /// <param name="languages">Список языков для чтения значений.</param>
    public ModuleEntriesLoadService(
        ILanguageRepository repository,
        EditorOperationTracker operations,
        IReadOnlyList<string> languages)
    {
        _repository = repository;
        _operations = operations;
        _languages = languages;
    }

    /// <summary>
    /// Загружает записи модуля с диска, если коллекция <see cref="Module.Items"/> ещё пуста.
    /// </summary>
    /// <param name="module">Модуль для загрузки.</param>
    /// <param name="domain">Домен модуля (не используется напрямую, сохранён для совместимости вызовов).</param>
    /// <param name="reportToStatusBar">Показывать ли прогресс в status bar.</param>
    /// <param name="isStillOpen">Проверка, что модуль всё ещё открыт в workspace (иначе загрузка не нужна).</param>
    public async Task LoadIfEmptyAsync(
        Module module,
        Domain domain,
        bool reportToStatusBar,
        Func<bool> isStillOpen)
    {
        if (module == null || module.ItemsLoadState == ModuleItemsLoadState.Full || isStillOpen is not { } stillOpen || !stillOpen())
        {
            return;
        }

        if (_inFlight.TryGetValue(module, out var existing))
        {
            await AwaitExistingLoadAsync(module, existing, reportToStatusBar);
            return;
        }

        await StartNewLoadAsync(module, reportToStatusBar, stillOpen);
    }

    /// <summary>
    /// Отменяет активную загрузку модуля, если она есть.
    /// </summary>
    public void Cancel(Module module)
    {
        if (module == null || !_inFlight.TryGetValue(module, out var op))
        {
            return;
        }

        op.Cts.Cancel();
    }

    /// <summary>
    /// Проверяет, выполняется ли сейчас загрузка указанного модуля.
    /// </summary>
    /// <param name="module">Проверяемый модуль.</param>
    /// <returns><c>true</c>, если загрузка активна.</returns>
    public bool IsLoading(Module module) => module != null && _inFlight.ContainsKey(module);

    private async Task AwaitExistingLoadAsync(Module module, LoadOperation existing, bool reportToStatusBar)
    {
        var operation = reportToStatusBar
            ? _operations.Begin(
                EditorStrings.FormatModuleLoadTitle(module.Name),
                key: module.Name,
                total: Math.Max(module.EntryCount, 1))
            : null;

        try
        {
            await existing.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // загрузка была отменена другим запросом
        }
        finally
        {
            if (operation != null)
            {
                _operations.End(operation);
            }
        }
    }

    private async Task StartNewLoadAsync(Module module, bool reportToStatusBar, Func<bool> isStillOpen)
    {
        if (!isStillOpen())
        {
            return;
        }

        var expectedTotal = Math.Max(module.EntryCount, 1);
        EditorOperation? operation = null;
        if (reportToStatusBar)
        {
            operation = _operations.Begin(EditorStrings.FormatModuleLoadTitle(module.Name), key: module.Name, total: expectedTotal);
            await Application.Current.Dispatcher.YieldAsync();
        }

        var cts = new CancellationTokenSource();
        var loadTask = LoadCoreAsync(module, cts.Token, isStillOpen);
        _inFlight[module] = new LoadOperation(loadTask, cts);

        try
        {
            await loadTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // ожидаемо при отмене через Cancel(module)
        }
        finally
        {
            _inFlight.Remove(module);
            cts.Dispose();
            if (operation != null)
            {
                _operations.End(operation);
            }
        }
    }

    private async Task LoadCoreAsync(Module module, CancellationToken cancellationToken, Func<bool> isStillOpen)
    {
        if (!isStillOpen())
        {
            throw new OperationCanceledException();
        }

        var data = await _repository.ReadTranslationEntriesAsync(module, _languages, cancellationToken)
            .ConfigureAwait(false);

        if (!isStillOpen())
        {
            throw new OperationCanceledException();
        }

        await module.PopulateFromRepositoryAsync(data.Metadata, data.Items, cancellationToken);
    }

    private sealed record LoadOperation(Task Task, CancellationTokenSource Cts);
}