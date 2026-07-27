namespace LangFilesEditor.Core.Application;

using System.Collections.Specialized;
using Models;
using Services;

// todo: а точно ли это должно быть отдельным классом? Но и это какой-то сервис словно. ПОдписки или ещё чего-то. Техническая штука очень.
/// <summary>
/// Подписка на события добавления entries и уведомление status bar.
/// </summary>
internal sealed class ModuleCatalogAttacher
{
    private readonly ModuleEntryStatusNotifier _notifier;
    private readonly HashSet<Domain> _attachedDomains = [];
    
    /// <summary>
    /// Создаёт компонент подписки на события модулей.
    /// </summary>
    /// <param name="notifier">Компонент уведомления status bar о добавлении entries.</param>
    public ModuleCatalogAttacher(ModuleEntryStatusNotifier notifier)
    {
        _notifier = notifier;
    }
    
    /// <summary>
    /// Подписывает все domain и их modules.
    /// </summary>
    /// <param name="domains">Коллекция domain для подключения обработчиков.</param>
    public void AttachAll(IEnumerable<Domain> domains)
    {
        foreach (var domain in domains)
        {
            AttachDomain(domain);
        }
    }
    
    /// <summary>
    /// Подписывает domain и следит за новыми modules.
    /// </summary>
    /// <param name="domain">Domain, модули которого нужно отслеживать.</param>
    public void AttachDomain(Domain domain)
    {
        _notifier.AttachAll(domain.Modules);
        
        if (!_attachedDomains.Add(domain))
        {
            return;
        }
        
        domain.Modules.CollectionChanged += OnDomainModulesChanged;
    }
    
    private void OnDomainModulesChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null)
        {
            return;
        }
        
        foreach (Module module in e.NewItems)
        {
            _notifier.Attach(module);
        }
    }
}