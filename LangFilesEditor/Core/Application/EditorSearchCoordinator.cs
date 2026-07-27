namespace LangFilesEditor.Core.Application;

using System.Collections.ObjectModel;
using Abstractions;
using Models;
using Services;

// todo: опять же, мне странно, что есть три разных координатора. Либо их добавление нужно сделать унифицированным и наследуемым от каокго-то интерфейса, что добавило бы структуру, логичность и понимание. А то они как-то... слишком уж отдельные.
// todo: не увидел как здесь отдаётся кадр UI, чтобы он мог спокойно функционировать. Кстати этому вопрос следует уделить внимание и у других координаторов
// todo: мб вообще лишний класс
/// <summary>
/// Параллельная загрузка entries для модулей области поиска.
/// Общая часть (гейт «один загрузчик на модуль») вынесена в <see cref="ModuleLoadGate"/>
/// и переиспользуется в <see cref="EditorDiagnosticLoadCoordinator"/>.
/// </summary>
internal sealed class EditorSearchCoordinator
{
    private readonly ILanguageRepository _repository;
    private readonly EditorOperationTracker _operations;
    private readonly IReadOnlyList<string> _languages;
    private readonly ModuleLoadGate _loadGate = new();
    
    // todo: это можно перевести в primary конструктор.
    /// <summary>
    /// Создаёт координатор загрузки entries для области поиска.
    /// </summary>
    /// <param name="repository">Репозиторий языковых файлов.</param>
    /// <param name="operations">Трекер прогресса длительных операций.</param>
    /// <param name="languages">Коды языков проекта для чтения переводов.</param>
    public EditorSearchCoordinator(
        ILanguageRepository repository,
        EditorOperationTracker operations,
        IReadOnlyList<string> languages)
    {
        _repository = repository;
        _operations = operations;
        _languages = languages;
    }
    
    /// <summary>
    /// Догружает entries для модулей без items. «Scope» здесь — уже разрешённый список модулей
    /// области поиска (см. <see cref="SearchEngine.ResolveTargetModules"/>), а не строка поиска;
    /// тот же смысл термина используется в <see cref="EditorDiagnosticLoadCoordinator.LoadScopeAsync"/>.
    /// </summary>
    /// <param name="domains">Все domain редактора; используются для разрешения общего domain поиска.</param>
    /// <param name="modules">Модули области поиска, для которых требуется загрузка строк.</param>
    /// <returns>Исходный список модулей после завершения загрузки entries.</returns>
    public async Task<IReadOnlyList<Module>> LoadScopeEntriesAsync(
        ObservableCollection<Domain> domains,
        IReadOnlyList<Module> modules)
    {
        if (modules == null || modules.Count == 0)
        {
            return modules ?? [];
        }
        
        var commonDomain = SearchEngine.TryGetCommonDomain(domains);
        
        if (commonDomain is { Modules.Count: 0 })
        {
            commonDomain.Modules = await _repository.LoadModulesAsync(commonDomain);
        }
        
        var toLoad = modules.Where(m => m.Items.Count == 0).ToList();
        
        if (toLoad.Count == 0)
        {
            return modules;
        }
        
        var operation = _operations.Begin(Helpers.EditorStrings.LoadingSearchData, total: toLoad.Count);
        var loadedModules = 0;
        
        try
        {
            await Parallel.ForEachAsync(
                toLoad,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                async (module, cancellationToken) =>
                {
                    // todo: вот этот await не ломает всю параллельность?
                    await LoadModuleEntriesAsync(module, cancellationToken);
                    var done = Interlocked.Increment(ref loadedModules);
                    _operations.Report(operation, done, toLoad.Count);
                });
        }
        finally
        {
            _operations.End(operation);
        }
        
        return modules;
    }
    
    private Task LoadModuleEntriesAsync(Module module, CancellationToken cancellationToken) =>
        _loadGate.RunAsync(
            module,
            async () =>
            {
                if (module.Items.Count > 0)
                {
                    return true;
                }
                
                var data = await _repository.ReadTranslationEntriesAsync(module, _languages, cancellationToken);
                // todo: Populat.... наименование неправильное.
                await module.PopulateFromRepositoryAsync(data.Metadata, data.Items, cancellationToken);
                return true;
            },
            cancellationToken);
}