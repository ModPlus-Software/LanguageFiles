namespace LangFilesEditor.Services;

using System.Collections.ObjectModel;
using Models;
using Utils;

/// <summary>
/// Параметры области поиска по модулям локализации.
/// </summary>
public sealed class SearchScopeOptions
{
    /// <summary>
    /// Искать в модуле Common вместе с текущим выбором.
    /// </summary>
    public bool SearchInCommon { get; init; }

    /// <summary>
    /// Искать во всех открытых вкладках модулей.
    /// </summary>
    public bool SearchInOpenModules { get; init; }

    /// <summary>
    /// Искать во всех модулях всех доменов.
    /// </summary>
    public bool SearchInAllModules { get; init; }
}

/// <summary>
/// Движок поиска: применение фильтра к модулям, определение области и поиск по тегам.
/// </summary>
public class SearchEngine
{
    /// <summary>
    /// Применяет строку поиска к целевым модулям; остальным сбрасывает фильтр.
    /// </summary>
    /// <param name="selectedModule">Текущий выбранный модуль.</param>
    /// <param name="selectedDomain">Текущий выбранный домен.</param>
    /// <param name="domains">Все домены редактора.</param>
    /// <param name="openModules">Открытые вкладки модулей.</param>
    /// <param name="query">Строка поиска.</param>
    /// <param name="scope">Параметры области поиска.</param>
    public void ApplySearch(
        Module selectedModule,
        Domain selectedDomain,
        ObservableCollection<Domain> domains,
        IReadOnlyCollection<Module> openModules,
        string query,
        SearchScopeOptions scope)
    {
        var targets = ResolveTargetModules(selectedModule, selectedDomain, domains, openModules, scope);
        var targetSet = targets.ToHashSet();
        foreach (var module in CollectModules(domains))
        {
            var searchText = targetSet.Contains(module) ? query : string.Empty;
            if (module.SearchString != searchText)
            {
                module.SearchString = searchText;
            }

            // Текстовый поиск и фильтр диагностики — независимые режимы; включение одного выключает другой.
            module.DiagnosticFilter = null;
        }
    }

    /// <summary>
    /// Определяет список модулей, участвующих в поиске, по выбранной области.
    /// </summary>
    /// <param name="selectedModule">Текущий выбранный модуль.</param>
    /// <param name="selectedDomain">Текущий выбранный домен.</param>
    /// <param name="domains">Все домены редактора.</param>
    /// <param name="openModules">Открытые вкладки модулей.</param>
    /// <param name="scope">Параметры области поиска.</param>
    /// <returns>Список модулей для фильтрации.</returns>
    public IReadOnlyList<Module> ResolveTargetModules(
        Module selectedModule,
        Domain selectedDomain,
        ObservableCollection<Domain> domains,
        IReadOnlyCollection<Module> openModules,
        SearchScopeOptions scope)
    {
        if (scope.SearchInAllModules && domains != null)
        {
            return CollectModules(domains).ToList();
        }

        if (scope.SearchInOpenModules)
        {
            var modules = openModules?.ToList() ?? [];
            EnsureCommonModuleIncluded(domains, modules);
            return modules;
        }

        if (scope.SearchInCommon)
        {
            var modules = new List<Module>();
            if (selectedModule != null)
            {
                modules.Add(selectedModule);
            }

            EnsureCommonModuleIncluded(domains, modules);
            return modules;
        }

        return selectedModule != null ? [selectedModule] : [];
    }

    /// <summary>
    /// Собирает все модули из коллекции доменов без дубликатов.
    /// </summary>
    /// <param name="domains">Домены редактора.</param>
    /// <returns>Плоский список модулей.</returns>
    public static IReadOnlyList<Module> CollectAllModules(ObservableCollection<Domain> domains) =>
        CollectModules(domains).ToList();

    /// <summary>
    /// Возвращает модули из целевого списка, в которых есть совпадения по текущему запросу.
    /// </summary>
    /// <param name="targets">Модули-кандидаты.</param>
    /// <param name="query">Строка поиска.</param>
    /// <returns>Модули с видимыми результатами поиска.</returns>
    public IReadOnlyList<Module> GetModulesWithResults(IReadOnlyList<Module> targets, string query)
    {
        if (string.IsNullOrWhiteSpace(query) || targets == null)
        {
            return [];
        }

        var result = new List<Module>();
        foreach (var module in targets)
        {
            if (module.HasVisibleSearchResults)
            {
                result.Add(module);
            }
        }

        return result;
    }

    /// <summary>
    /// Применяет фильтр диагностики к целевым модулям; остальным сбрасывает фильтр.
    /// Фильтр диагностики и текстовый поиск — независимые режимы, поэтому здесь же сбрасывается
    /// текстовый поиск у всех модулей (включение одного режима выключает другой).
    /// </summary>
    /// <param name="domains">Все домены редактора.</param>
    /// <param name="targetModules">Модули, в которых нужно показать только строки категории.</param>
    /// <param name="severity">Категория диагностики.</param>
    public void ApplyDiagnosticFilter(
        ObservableCollection<Domain> domains,
        IReadOnlyList<Module> targetModules,
        DiagnosticSeverity severity)
    {
        var targetSet = targetModules.ToHashSet();
        foreach (var module in CollectModules(domains))
        {
            module.DiagnosticFilter = targetSet.Contains(module) ? severity : null;

            if (module.SearchString != string.Empty)
            {
                module.SearchString = string.Empty;
            }
        }
    }

    /// <summary>
    /// Проверяет, проходит ли запись текущий фильтр модуля
    /// (используется для <see cref="Module.ItemsView"/> и <see cref="Module.HasVisibleSearchResults"/>).
    /// Текстовый поиск и фильтр диагностики — независимые режимы: если задан <paramref name="diagnosticFilter"/>,
    /// он и определяет видимость записи, иначе используется текстовый поиск по <paramref name="searchText"/>.
    /// </summary>
    /// <param name="item">Проверяемая запись перевода.</param>
    /// <param name="searchText">Текущая строка текстового поиска модуля (см. <see cref="Module.SearchString"/>).</param>
    /// <param name="diagnosticFilter">Текущий фильтр диагностики модуля (см. <see cref="Module.DiagnosticFilter"/>).</param>
    /// <returns><see langword="true"/>, если запись должна быть видна при этом фильтре.</returns>
    public static bool PassesFilter(TranslationEntry item, string searchText, DiagnosticSeverity? diagnosticFilter)
    {
        if (item == null)
        {
            return false;
        }

        if (diagnosticFilter.HasValue)
        {
            return item.DiagnosticState.MatchesDiagnosticFilter(diagnosticFilter.Value);
        }

        if (string.IsNullOrEmpty(searchText))
        {
            return true;
        }

        if (item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        foreach (var pair in item.Values)
        {
            if (pair.Value.Value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Проверяет наличие записи с указанным именем (порядковое сравнение без учёта регистра не выполняется).
    /// </summary>
    /// <param name="items">Строки модуля для поиска.</param>
    /// <param name="name">Искомое имя ключа.</param>
    /// <returns><see langword="true"/>, если запись с таким именем уже есть в коллекции.</returns>
    public static bool ContainsItemByName(IEnumerable<TranslationEntry> items, string name) =>
        items.Any(item => string.Equals(item.Name, name, StringComparison.Ordinal));

    /// <summary>
    /// Находит последнюю строку модуля с указанным базовым именем тега (без числового суффикса).
    /// </summary>
    /// <param name="module">Модуль для поиска.</param>
    /// <param name="tagValue">Базовое имя тега без суффикса.</param>
    /// <param name="index">Индекс найденной строки или -1.</param>
    /// <returns>Наибольший числовой суффикс среди совпадений или -1.</returns>
    public static int SearchLastRowWithTagValue(Module module, string tagValue, out int index)
    {
        index = -1;
        if (module == null || string.IsNullOrWhiteSpace(tagValue))
        {
            return -1;
        }

        var biggestNumber = -1;
        for (var i = 0; i < module.Items.Count; i++)
        {
            var item = module.Items[i];
            TagTextUtils.GetTagValueAndNumber(item.Name, out var value, out var number);
            if (!value.Equals(tagValue, StringComparison.Ordinal) || i <= index)
            {
                continue;
            }

            biggestNumber = number;
            index = i;
        }

        return biggestNumber;
    }

    private static IEnumerable<Module> CollectModules(ObservableCollection<Domain> domains)
    {
        if (domains == null)
        {
            yield break;
        }

        var seen = new HashSet<Module>();
        foreach (var domain in domains)
        {
            if (domain?.Modules == null)
            {
                continue;
            }

            foreach (var module in domain.Modules)
            {
                if (seen.Add(module))
                {
                    yield return module;
                }
            }
        }
    }

    /// <summary>
    /// Возвращает общий (Common) домен, если он присутствует в коллекции.
    /// </summary>
    /// <param name="domains">Домены редактора.</param>
    /// <returns>Общий домен или <c>null</c>.</returns>
    public static Domain TryGetCommonDomain(ObservableCollection<Domain> domains) =>
        domains?.FirstOrDefault(d => d.IsCommon);

    /// <summary>
    /// Общий модуль — модуль общего домена с именем <see cref="Constants.CommonDomainName"/>
    /// (единственный источник общих строк). Модули домена отсортированы по алфавиту,
    /// поэтому определять общий модуль по позиции в списке нельзя — только по имени.
    /// </summary>
    /// <param name="domains">Домены редактора.</param>
    /// <returns>Общий модуль или <c>null</c>.</returns>
    public static Module TryGetCommonModule(ObservableCollection<Domain> domains)
    {
        var commonDomain = TryGetCommonDomain(domains);
        if (commonDomain == null)
        {
            return null;
        }

        return commonDomain.Modules.FirstOrDefault(
                   m => string.Equals(m.Name, Constants.CommonDomainName, StringComparison.OrdinalIgnoreCase))
               ?? commonDomain.Modules.FirstOrDefault();
    }

    private static void EnsureCommonModuleIncluded(
        ObservableCollection<Domain> domains,
        List<Module> modules)
    {
        var common = TryGetCommonModule(domains);
        if (common != null && !modules.Contains(common))
        {
            modules.Add(common);
        }
    }
}