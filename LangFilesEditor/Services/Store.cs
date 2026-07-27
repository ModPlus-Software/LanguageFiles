namespace LangFilesEditor.Services;

using System.Collections.ObjectModel;
using Core.Abstractions;
using Core.Application;
using Models;
using ModPlusAPI.Mvvm;

/// <summary>
/// Сессия данных редактора: языки, домены, загрузка каталогов/entries и сохранение.
/// Презентационное состояние (выбор, вкладки, результатные режимы) живёт в <see cref="EditorWorkspace"/>.
/// Изменяется только через стабильные контракты в <see cref="Core.Abstractions"/>.
/// </summary>
public class Store : ObservableObject, IEditorSession, IEditorCommands
{
    private readonly EditorSaveCoordinator _save;
    private readonly DomainModuleLoadCoordinator _domainLoads;
    private readonly EditorSearchCoordinator _search;
    private readonly EditorDiagnosticLoadCoordinator _diagnostic;
    private readonly ModuleEntriesLoadService _entryLoads;
    private readonly EditorOperationTracker _operations;
    private ObservableCollection<Domain> _domains;
    private bool _wasOperationInProgress;
    
    /// <summary>
    /// Создаёт сессию с указанным репозиторием локализации.
    /// </summary>
    /// <param name="repository">Репозиторий XML-файлов локализации.</param>
    /// <param name="operations">Трекер длительных операций (владелец — <see cref="Core.Application.EditorBootstrap"/>).</param>
    public Store(ILanguageRepository repository, EditorOperationTracker operations)
    {
        Repository = repository;
        _operations = operations;
        Languages = repository.LoadLanguages(Constants.LanguageFilesDirectory);
        var notifier = new ModuleEntryStatusNotifier(operations);
        var catalogAttacher = new ModuleCatalogAttacher(notifier);
        _save = new EditorSaveCoordinator(repository);
        _domainLoads = new DomainModuleLoadCoordinator(repository, catalogAttacher, operations);
        _search = new EditorSearchCoordinator(repository, operations, Languages);
        _diagnostic = new EditorDiagnosticLoadCoordinator(repository, operations, Languages);
        _entryLoads = new ModuleEntriesLoadService(repository, operations, Languages);
        operations.Changed += OnOperationChanged;
        Domains = repository.LoadDomains(Constants.LanguageFilesDirectory, Languages);
        catalogAttacher.AttachAll(Domains);
    }
    
    /// <summary>
    /// todo: нужна ли здесь вообще эта штука?
    /// </summary>
    internal ILanguageRepository Repository { get; }
    
    /// <summary>
    /// Координатор загрузки каталогов модулей domain'ов (для <see cref="EditorWorkspace"/>).
    /// </summary>
    internal DomainModuleLoadCoordinator DomainLoads => _domainLoads;
    
    /// <summary>
    /// Сервис ленивой загрузки entries модулей (для <see cref="EditorWorkspace"/>).
    /// </summary>
    internal ModuleEntriesLoadService EntryLoads => _entryLoads;
    
    /// <summary>
    /// Координатор загрузки entries для диагностических отображений (для <see cref="EditorWorkspace"/>).
    /// </summary>
    internal EditorDiagnosticLoadCoordinator DiagnosticLoads => _diagnostic;
    
    /// <summary>
    /// Занят ли редактор длительной операцией. Детали прогресса живут в
    /// <see cref="EditorOperationTracker"/> — UI биндится к нему напрямую.
    /// </summary>
    /// <inheritdoc />
    public bool IsOperationInProgress => _operations.IsActive;
    
    // todo: вроде важная штука. Так её оставить? Но то, что readonly только - это круто.
    /// <inheritdoc />
    public IReadOnlyList<string> Languages { get; }
    
    /// <inheritdoc />
    public ObservableCollection<Domain> Domains
    {
        get => _domains;
        set
        {
            if (_domains == value)
            {
                return;
            }
            
            _domains = value;
            OnPropertyChanged();
        }
    }
    
    // todo: как я говорил, можно сделать так, чтобы в этом словно не было необходимости. Но тут нужно подумать. Это важное архитектурное решение.
    /// <inheritdoc />
    public async Task EnsureDomainModuleListsLoadedAsync() => await _domainLoads.EnsureAllLoadedAsync(_domains ?? []);
    
    // todo: Словно можно просто Load сделать, а не вот этот SearchScope. Наименование не верное. Лишние методы
    /// <inheritdoc />
    public async Task<IReadOnlyList<Module>> LoadSearchScopeEntriesAsync(IReadOnlyList<Module> modules) =>
        await _search.LoadScopeEntriesAsync(_domains, modules);
    
    // todo: Это должно быть. Но нужно посмотреть функционал.
    /// <inheritdoc />
    public bool Save() => _domains != null && _save.Save(_domains, Languages);
    
    /// <inheritdoc />
    public void TrackItemForRemoval(Module module, TranslationEntry entry) =>
        _save.TrackItemForRemoval(module, entry);
    
    // Трекер дёргает Changed на каждый Report; наружу транслируется только реальная смена
    // «занят/свободен» — остальные детали прогресса UI берёт из трекера напрямую.
    private void OnOperationChanged()
    {
        if (_wasOperationInProgress == _operations.IsActive)
        {
            return;
        }
        
        _wasOperationInProgress = _operations.IsActive;
        OnPropertyChanged(nameof(IsOperationInProgress));
    }
}