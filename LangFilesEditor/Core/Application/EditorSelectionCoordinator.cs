namespace LangFilesEditor.Core.Application;

using Models;

/// <summary>
/// todo:
/// </summary>
internal sealed class EditorSelectionCoordinator
{
    private readonly DomainModuleLoadCoordinator _domainLoads;
    
    /// <summary>
    /// Создаёт координатор выбора.
    /// </summary>
    /// <param name="domainLoads">Координатор загрузки каталогов модулей domain'ов.</param>
    public EditorSelectionCoordinator(DomainModuleLoadCoordinator domainLoads)
    {
        _domainLoads = domainLoads;
    }
    
    /// <summary>
    /// Выбранный domain.
    /// </summary>
    public Domain SelectedDomain { get; private set; }
    
    /// <summary>
    /// Выбранный module.
    /// </summary>
    public Module SelectedModule { get; private set; }
    
    /// <summary>
    /// Выбранная строка перевода.
    /// </summary>
    public TranslationEntry SelectedTranslationEntry { get; private set; }
    
    /// <summary>
    /// Выбирает domain. Если выбранный module принадлежит другому domain — сбрасывает его вместе со строкой.
    /// Для domain с незагруженным каталогом запускает фоновую загрузку списка модулей.
    /// </summary>
    /// <param name="domain">Новый выбранный domain или <see langword="null"/>.</param>
    public void SelectDomain(Domain domain)
    {
        if (SelectedDomain == domain)
        {
            return;
        }
        
        SelectedDomain = domain;
        
        if (SelectedModule != null && domain != null && SelectedModule.Group != domain)
        {
            ClearModule();
        }
        
        if (domain is { Modules.Count: 0 })
        {
            _ = _domainLoads.EnsureLoadedAsync(domain);
        }
    }
    
    /// <summary>
    /// Общее ядро выбора модуля: синхронизирует domain (с догрузкой его каталога) и сбрасывает выбранную строку.
    /// <see langword="null"/> сбрасывает module и строку, не трогая domain.
    /// </summary>
    /// <param name="module">Новый выбранный module или <see langword="null"/>.</param>
    /// <returns><see langword="true"/>, если выбранный module изменился; <see langword="false"/> при повторном выборе того же.</returns>
    public bool SelectModule(Module module)
    {
        if (module == null)
        {
            ClearModule();
            return false;
        }
        
        if (ReferenceEquals(SelectedModule, module))
        {
            return false;
        }
        
        SelectedModule = module;
        
        if (SelectedDomain != module.Group)
        {
            SelectedDomain = module.Group;
            if (module.Group.Modules.Count == 0)
            {
                _ = _domainLoads.EnsureLoadedAsync(module.Group);
            }
        }
        
        SelectedTranslationEntry = null;
        return true;
    }
    
    /// <summary>
    /// Сбрасывает выбранный module вместе со строкой, не трогая domain.
    /// </summary>
    public void ClearModule()
    {
        SelectedModule = null;
        SelectedTranslationEntry = null;
    }
    
    /// <summary>
    /// Выбирает строку перевода.
    /// </summary>
    /// <param name="entry">Новая выбранная строка или <see langword="null"/>.</param>
    public void SelectTranslationEntry(TranslationEntry entry) => SelectedTranslationEntry = entry;
}