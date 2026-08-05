namespace LangFilesEditor.Core.Application;

using Abstractions;
using Models;
using Services;

/// <summary>
/// Асинхронная загрузка списков modules domain.
/// </summary>
internal sealed class DomainModuleLoadCoordinator
{
    private readonly ILanguageRepository _repository;
    private readonly ModuleCatalogAttacher _catalogAttacher;
    private readonly EditorOperationTracker _operations;
    private readonly Dictionary<Domain, LoadOperation> _inFlight = new();

    /// <summary>
    /// Создаёт координатор загрузки каталогов модулей.
    /// </summary>
    /// <param name="repository">Репозиторий языковых файлов на диске.</param>
    /// <param name="catalogAttacher">Компонент подписки на события добавления entries в модули.</param>
    /// <param name="operations">Трекер длительных операций для отображения прогресса в status bar.</param>
    public DomainModuleLoadCoordinator(
        ILanguageRepository repository,
        ModuleCatalogAttacher catalogAttacher,
        EditorOperationTracker operations)
    {
        _repository = repository;
        _catalogAttacher = catalogAttacher;
        _operations = operations;
    }

    /// <summary>
    /// Загружает modules для domain, если ни один модуль незагружен.
    /// </summary>
    /// <param name="domain">Domain, модули которого должны быть загружены</param>
    /// <returns>Задача, завершающаяся после загрузки или присоединения к уже выполняющейся загрузке.</returns>
    public async Task EnsureLoadedAsync(Domain domain)
    {
        if (domain.Modules is { Count: > 0 })
        {
            return;
        }

        var operation = _operations.Begin(Helpers.EditorStrings.FormatModuleCatalogLoadTitle(domain.Name), key: $"catalog:{domain.Name}");
        try
        {
            if (_inFlight.TryGetValue(domain, out var existing))
            {
                try
                {
                    await existing.Task;
                }
                catch (OperationCanceledException)
                {
                    // ожидаемо при отмене загрузки
                }

                return;
            }

            var cts = new CancellationTokenSource();
            var loadTask = LoadCoreAsync(domain, cts.Token);
            _inFlight[domain] = new LoadOperation(loadTask, cts);

            try
            {
                await loadTask;
            }
            catch (OperationCanceledException)
            {
                // ожидаемо при отмене загрузки
            }
            finally
            {
                _inFlight.Remove(domain);
                cts.Dispose();
            }
        }
        finally
        {
            _operations.End(operation);
        }
    }

    /// <summary>
    /// Загружает modules для всех domain с пустым списком.
    /// </summary>
    /// <param name="domains">Коллекция domain, для которых требуется каталог модулей.</param>
    /// <returns>Задача, завершающаяся после параллельной загрузки всех недостающих каталогов.</returns>
    public async Task EnsureAllLoadedAsync(IEnumerable<Domain> domains)
    {
        var tasks = domains
            .Where(d => d.Modules is not { Count: > 0 })
            .Select(EnsureLoadedAsync)
            .ToList();

        if (tasks.Count <= 0)
        {
            return;
        }

        await Task.WhenAll(tasks);
    }

    private async Task LoadCoreAsync(Domain domain, CancellationToken cancellationToken)
    {
        var modules = await _repository.LoadModulesAsync(domain);
        cancellationToken.ThrowIfCancellationRequested();

        if (domain.Modules is { Count: > 0 })
        {
            return;
        }

        domain.Modules = modules;
        _catalogAttacher.AttachDomain(domain);
    }

    private sealed record LoadOperation(Task Task, CancellationTokenSource Cts);
}