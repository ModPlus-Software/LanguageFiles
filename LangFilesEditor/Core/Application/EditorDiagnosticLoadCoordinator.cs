namespace LangFilesEditor.Core.Application;

using System.Collections.Concurrent;
using Abstractions;
using Helpers;
using Models;
using Services;

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

        openModules ??= [];
        var openSet = openModules as HashSet<Module> ?? openModules.ToHashSet();

        var toLoad = modules
            .Where(module => module.ItemsLoadState != ModuleItemsLoadState.Full && !openSet.Contains(module))
            .ToList();

        if (toLoad.Count == 0)
        {
            return modules.Where(module => ModuleHasDiagnosticMatches(module, severity, openSet)).ToList();
        }

        var toLoadSet = toLoad.ToHashSet();
        var matched = new ConcurrentDictionary<Module, byte>();

        await Parallel.ForEachAsync(
            toLoad,
            new ParallelOptions { CancellationToken = cancellationToken },
            async (module, ct) =>
            {
                if (await LoadModuleAsync(module, severity, openSet, ct))
                {
                    matched.TryAdd(module, 0);
                }
            });

        foreach (var module in modules)
        {
            if (!toLoadSet.Contains(module) && ModuleHasDiagnosticMatches(module, severity, openSet))
            {
                matched.TryAdd(module, 0);
            }
        }

        // Порядок отображения берётся из исходного списка, а не из порядка завершения параллельных задач.
        return modules.Where(matched.ContainsKey).ToList();
    }

    /// <summary>
    /// Загружает диагностические строки одного модуля.
    /// </summary>
    /// <param name="openModules">Модули, уже открытые во вкладках.</param>
    /// <returns><see langword="true"/>, если найдена хотя бы одна подходящая строка.</returns>
    public async Task<bool> LoadModuleAsync(
        Module module,
        DiagnosticSeverity severity,
        IReadOnlyCollection<Module> openModules,
        CancellationToken cancellationToken = default)
    {
        openModules ??= [];
        var openSet = openModules as HashSet<Module> ?? openModules.ToHashSet();

        if (TryResolveWithoutLoad(module, severity, openSet, out var resolved))
        {
            return resolved;
        }

        return await _loadGate.RunAsync(
            module,
            async () =>
            {
                // Пока ждали гейт, модуль мог быть загружен другим запросом — проверки повторяются.
                if (TryResolveWithoutLoad(module, severity, openSet, out var resolvedAfterGate))
                {
                    return resolvedAfterGate;
                }

                if (module.IsBulkItemsLoading)
                {
                    return HasDiagnosticMatches(module, severity);
                }

                EditorOperation operation = null;
                try
                {
                    operation = _operations.Begin(
                        EditorStrings.FormatModuleLoadTitle(module.Name),
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

    /// <summary>
    /// Пытается ответить на вопрос «есть ли подходящие строки» без чтения файлов — по уже
    /// загруженному состоянию модуля.
    /// </summary>
    /// <returns><see langword="true"/>, если ответ получен и загрузка не требуется.</returns>
    private static bool TryResolveWithoutLoad(
        Module module,
        DiagnosticSeverity severity,
        HashSet<Module> openModules,
        out bool hasMatches)
    {
        if (openModules.Contains(module))
        {
            hasMatches = ModuleHasDiagnosticMatches(module, severity, openModules);
            return true;
        }

        if (module.ItemsLoadState == ModuleItemsLoadState.Full
            || (module.ItemsLoadState == ModuleItemsLoadState.PartialDiagnostic && module.Items.Count > 0))
        {
            hasMatches = HasDiagnosticMatches(module, severity);
            return true;
        }

        hasMatches = false;
        return false;
    }

    private static bool HasDiagnosticMatches(Module module, DiagnosticSeverity severity) =>
        module.Items.Any(entry => entry.DiagnosticState.MatchesDiagnosticFilter(severity));

    private static bool ModuleHasDiagnosticMatches(
        Module module,
        DiagnosticSeverity severity,
        HashSet<Module> openModules)
    {
        if (module.ItemsLoadState == ModuleItemsLoadState.Full || openModules.Contains(module))
        {
            return HasDiagnosticMatches(module, severity);
        }

        return module.ItemsLoadState == ModuleItemsLoadState.PartialDiagnostic
               && HasDiagnosticMatches(module, severity);
    }
}