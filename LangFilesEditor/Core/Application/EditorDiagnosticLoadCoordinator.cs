namespace LangFilesEditor.Core.Application;

using System.Collections.Concurrent;
using Abstractions;
using Models;
using Services;

// todo: в SearchEngine перенести?
/// <summary>
/// Загружает в модули только строки, соответствующие фильтру диагностики, без полного наполнения UI.
/// Общая часть (гейт «один загрузчик на модуль») вынесена в <see cref="ModuleLoadGate"/>
/// и переиспользуется в <see cref="EditorSearchCoordinator"/>.
/// </summary>
internal sealed class EditorDiagnosticLoadCoordinator
{
    private readonly ILanguageRepository _repository;
    private readonly EditorOperationTracker _operations;
    private readonly IReadOnlyList<string> _languages;
    private readonly ModuleLoadGate _loadGate = new();
    
    /// <summary>
    /// Создаёт координатор частичной загрузки диагностики.
    /// </summary>
    public EditorDiagnosticLoadCoordinator(
        ILanguageRepository repository,
        EditorOperationTracker operations,
        IReadOnlyList<string> languages)
    {
        _repository = repository;
        _operations = operations;
        _languages = languages;
    }
    
    /// <summary>
    /// Загружает диагностические строки для нескольких модулей параллельно. «Scope» здесь — уже
    /// разрешённый список модулей, для которых нужна диагностическая загрузка (тот же смысл термина,
    /// что и в <see cref="EditorSearchCoordinator.LoadScopeEntriesAsync"/>), а не строка/область поиска.
    /// </summary>
    /// <param name="openModules">Модули, уже открытые во вкладках — для них частичная загрузка не выполняется.</param>
    /// <returns>Модули, в которых после загрузки есть хотя бы одна подходящая строка.</returns>
    public async Task<IReadOnlyList<Module>> LoadScopeAsync(
        IReadOnlyList<Module> modules,
        DiagnosticSeverity severity,
        IReadOnlyCollection<Module> openModules,
        CancellationToken cancellationToken = default)
    {
        if (modules == null || modules.Count == 0)
        {
            return [];
        }
        
        // todo: Возможно лишние здесь преобразования.
        openModules ??= [];
        var openSet = openModules as HashSet<Module> ?? openModules.ToHashSet();
        
        var toLoad = modules
            .Where(module => module.ItemsLoadState != ModuleItemsLoadState.Full && !openSet.Contains(module))
            .ToList();
        
        if (toLoad.Count == 0)
        {
            return modules.Where(module => ModuleHasDiagnosticMatches(module, severity, openSet)).ToList();
        }
        
        var results = new ConcurrentBag<Module>();
        
        await Parallel.ForEachAsync(
            toLoad,
            new ParallelOptions { CancellationToken = cancellationToken },
            async (module, ct) =>
            {
                if (await LoadModuleAsync(module, severity, openSet, ct))
                {
                    results.Add(module);
                }
            });
        
        // todo: нарушение принципа KISS
        foreach (var module in modules.Where(module => !toLoad.Contains(module)))
        {
            if (ModuleHasDiagnosticMatches(module, severity, openSet))
            {
                results.Add(module);
            }
        }
        
        return results.Distinct().ToList();
    }
    
    /// <summary>
    /// Загружает диагностические строки одного модуля.
    /// </summary>
    /// <param name="openModules">Модули, уже открытые во вкладках.</param>
    /// <returns><see langword="true"/>, если найдена хотя бы одна подходящая строка.</returns>
    // todo: Это группирование не обязательно только про диагностика
    public async Task<bool> LoadModuleAsync(
        Module module,
        DiagnosticSeverity severity,
        IReadOnlyCollection<Module> openModules,
        CancellationToken cancellationToken = default)
    {
        // todo: Что меня в этих двух строках смущает - Store по каким-то причинам сюда обращается для загрузки модулей, и чисто диагностика. Такое ощущение, словно этот метод должен лежать в самом store. Мб я не прав, конечно. Нужно посмотреть сверху как выполняется работа (в классе Store в основном)
        openModules ??= [];
        var openSet = openModules as HashSet<Module> ?? openModules.ToHashSet();
        
        // todo: Возможно при качественной переработке выполняемой задачи этим классом, мб переносе куда-то многое из необходимого здесь на данный момент отпадёт.
        if (openSet.Contains(module))
        {
            return ModuleHasDiagnosticMatches(module, severity, openSet);
        }
        
        if (module.ItemsLoadState == ModuleItemsLoadState.Full)
        {
            return module.Items.Any(entry => entry.DiagnosticState.MatchesDiagnosticFilter(severity));
        }
        
        if (module.ItemsLoadState == ModuleItemsLoadState.PartialDiagnostic && module.Items.Count > 0)
        {
            return module.Items.Any(entry => entry.DiagnosticState.MatchesDiagnosticFilter(severity));
        }
        
        // todo: дублирование всех if'ов после семафора можно убрать
        return await _loadGate.RunAsync(
            module,
            async () =>
            {
                if (openSet.Contains(module))
                {
                    return ModuleHasDiagnosticMatches(module, severity, openSet);
                }
                
                if (module.ItemsLoadState == ModuleItemsLoadState.Full)
                {
                    return module.Items.Any(entry => entry.DiagnosticState.MatchesDiagnosticFilter(severity));
                }
                
                if (module.ItemsLoadState == ModuleItemsLoadState.PartialDiagnostic && module.Items.Count > 0)
                {
                    return module.Items.Any(entry => entry.DiagnosticState.MatchesDiagnosticFilter(severity));
                }
                
                if (module.IsBulkItemsLoading)
                {
                    return module.Items.Any(entry => entry.DiagnosticState.MatchesDiagnosticFilter(severity));
                }
                
                EditorOperation operation = null;
                try
                {
                    operation = _operations.Begin(
                        FormatModuleLoadTitle(module),
                        key: module.Name,
                        total: Math.Max(module.EntryCount, 1));
                    
                    var data = await _repository.ReadTranslationEntriesAsync(module, _languages, cancellationToken);
                    await module.PopulateDiagnosticEntriesAsync(data.Items, severity, cancellationToken);
                    return module.Items.Count > 0;
                }
                finally
                {
                    if (operation != null)
                    {
                        _operations.End(operation);
                    }
                }
            },
            cancellationToken);
    }
    
    // todo: Вопрос необходимости этого.
    private static string FormatModuleLoadTitle(Module module) => Helpers.EditorStrings.FormatModuleLoadTitle(module.Name);
    
    // todo: можно было бы здесь метод Modules с перегрузкой и передачей одного модуля. Таким гобразом можно было бы убрать один цикл и добавить функциональности при необходимости.
    // todo: Это группирование не обязательно только про диагностика
    private static bool ModuleHasDiagnosticMatches(
        Module module,
        DiagnosticSeverity severity,
        HashSet<Module> openModules)
    {
        if (module.ItemsLoadState == ModuleItemsLoadState.Full || openModules.Contains(module))
        {
            return module.Items.Any(entry => entry.DiagnosticState.MatchesDiagnosticFilter(severity));
        }
        
        return module.ItemsLoadState == ModuleItemsLoadState.PartialDiagnostic
               && module.Items.Any(entry => entry.DiagnosticState.MatchesDiagnosticFilter(severity));
    }
}