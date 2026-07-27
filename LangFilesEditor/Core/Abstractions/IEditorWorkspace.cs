namespace LangFilesEditor.Core.Abstractions;

using System.Collections.ObjectModel;
using System.ComponentModel;
using Models;

// todo: во view перенести
/// <summary>
/// Состояние рабочей области редактора.
/// </summary>
public interface IEditorWorkspace : INotifyPropertyChanged
{
    /// <summary>
    /// Выбранный domain.
    /// </summary>
    Domain SelectedDomain { get; set; }
    
    /// <summary>
    /// Выбранный module.
    /// </summary>
    Module SelectedModule { get; set; }
    
    /// <summary>
    /// Выбранная строка перевода.
    /// </summary>
    TranslationEntry SelectedTranslationEntry { get; set; }
    
    /// <summary>
    /// Открытые вкладки модулей.
    /// </summary>
    ObservableCollection<Module> OpenModules { get; }
    
    /// <summary>
    /// Модули для отображения в workspace.
    /// </summary>
    IReadOnlyList<Module> DisplayModules { get; }
    
    /// <summary>
    /// Режим отображения результатов поиска.
    /// </summary>
    bool IsSearchResultsView { get; }
    
    /// <summary>
    /// Режим просмотра диагностики (фильтр по ошибкам/предупреждениям/обновлениям).
    /// </summary>
    bool IsDiagnosticResultsView { get; }
    
    /// <summary>
    /// Активный фильтр диагностики в рабочей области (<see langword="null"/>, если фильтр выключен).
    /// </summary>
    DiagnosticSeverity? ActiveDiagnosticFilter { get; }
    
    /// <summary>
    /// Устанавливает или сбрасывает фильтр диагностики в рабочей области.
    /// </summary>
    /// <param name="severity">Категория фильтра или <see langword="null"/> для сброса.</param>
    void SetActiveDiagnosticFilter(DiagnosticSeverity? severity);
    
    /// <summary>
    /// Переключает режим результатов поиска.
    /// </summary>
    /// <param name="active"><see langword="true"/> для включения режима поиска; <see langword="false"/> для возврата к обычным вкладкам.</param>
    /// <param name="modules">Модули, отображаемые в режиме поиска; игнорируются при <paramref name="active"/> = <see langword="false"/>.</param>
    void SetSearchResultsView(bool active, IReadOnlyList<Module> modules);
    
    /// <summary>
    /// Включает или выключает режим просмотра диагностики в workspace.
    /// </summary>
    /// <param name="active">Включён ли режим.</param>
    /// <param name="modules">Модули для отображения.</param>
    void SetDiagnosticResultsView(bool active, IReadOnlyList<Module> modules);
    
    /// <summary>
    /// Выбор модуля из таблицы поиска без выхода из режима поиска.
    /// </summary>
    /// <param name="module">Модуль, который нужно выделить в результатах поиска.</param>
    void SelectModuleDuringSearch(Module module);
    
    /// <summary>
    /// Выбор модуля в режиме диагностики без добавления вкладки.
    /// </summary>
    /// <param name="module">Выбранный модуль.</param>
    void SelectModuleDuringDiagnostic(Module module);
    
    /// <summary>
    /// Показывает диагностику одного модуля в workspace.
    /// </summary>
    /// <param name="module">Модуль.</param>
    /// <param name="severity">Категория диагностики.</param>
    Task ShowModuleDiagnosticAsync(Module module, DiagnosticSeverity severity);
    
    /// <summary>
    /// Показывает диагностику по нескольким модулям.
    /// </summary>
    /// <param name="severity">Категория диагностики.</param>
    /// <param name="modules">Модули-кандидаты.</param>
    Task ShowDiagnosticFilterAsync(DiagnosticSeverity severity, IReadOnlyList<Module> modules);
    
    /// <summary>
    /// Закрывает вкладку модуля.
    /// </summary>
    /// <param name="module">Модуль, вкладку которого нужно закрыть.</param>
    void CloseModule(Module module);
    
    /// <summary>
    /// Запускает ленивую загрузку entries открытого модуля.
    /// </summary>
    /// <param name="module">Модуль, строки перевода которого нужно загрузить с диска.</param>
    void BeginLoadModuleEntries(Module module);
    
    /// <summary>
    /// Идёт ли загрузка entries указанного модуля.
    /// </summary>
    /// <param name="module">Проверяемый модуль.</param>
    /// <returns><see langword="true"/>, если загрузка entries для модуля выполняется в данный момент.</returns>
    bool IsModuleEntriesLoading(Module module);
}