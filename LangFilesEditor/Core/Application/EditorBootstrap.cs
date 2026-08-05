namespace LangFilesEditor.Core.Application;

using Abstractions;
using Services;
using Services.Diagnostics;
using Services.RepositoryServices;

/// <summary>
/// Загрузчик приложения?
/// </summary>
public sealed class EditorBootstrap
{
    /// <summary>
    /// Создаёт и связывает основные сервисы редактора и регистрирует переданные расширения.
    /// </summary>
    /// <param name="extensions">Набор расширений, подключаемых к <see cref="ExtensionHost"/>.</param>
    public EditorBootstrap(IEnumerable<ILangFilesEditorExtension> extensions)
    {
        Settings = EditorSettingsStore.Load();
        Repository = new LanguageRepositoryService();
        SearchEngine = new SearchEngine();
        OperationTracker = new EditorOperationTracker();
        Store = new Store(Repository, OperationTracker);
        Workspace = new EditorWorkspace(Store, SearchEngine);
        ModuleEditor = new ModuleEditorService();
        Operations = new BackgroundOperationService(OperationTracker);
        Diagnostics = new EditorDiagnosticsService(OperationTracker);
        Diagnostics.AttachToSession(Store, Workspace);
        var extensionSession = new ExtensionSessionAdapter(Store, Workspace);
        ExtensionHost = new ExtensionHost(extensionSession, Store, ModuleEditor, Operations, Diagnostics, extensions);
    }

    /// <summary>
    /// Пользовательские настройки редактора.
    /// </summary>
    public EditorSettingsStore Settings { get; }

    /// <summary>
    /// Доступ к XML-файлам локализации.
    /// </summary>
    public ILanguageRepository Repository { get; }

    /// <summary>
    /// Единый движок поиска/фильтрации на всё приложение (одна инстанция вместо отдельных на каждый ViewModel).
    /// </summary>
    public SearchEngine SearchEngine { get; }

    /// <summary>
    /// Трекер длительных операций — владелец состояния прогресса на всё приложение.
    /// UI (status bar) биндится к нему напрямую, минуя сессию.
    /// </summary>
    public EditorOperationTracker OperationTracker { get; }

    /// <summary>
    /// Сессия данных редактора (языки, домены, загрузка, сохранение).
    /// </summary>
    public Store Store { get; }

    /// <summary>
    /// Состояние рабочей области (выбор, вкладки, результатные режимы) — view-слой поверх сессии.
    /// </summary>
    public EditorWorkspace Workspace { get; }

    /// <summary>
    /// Мутация modules.
    /// </summary>
    public IModuleEditor ModuleEditor { get; }

    /// <summary>
    /// Фоновые операции.
    /// </summary>
    public BackgroundOperationService Operations { get; }

    /// <summary>
    /// Сводка диагностики и публикация для расширений.
    /// </summary>
    public EditorDiagnosticsService Diagnostics { get; }

    /// <summary>
    /// Host для расширений.
    /// </summary>
    public ExtensionHost ExtensionHost { get; }
}