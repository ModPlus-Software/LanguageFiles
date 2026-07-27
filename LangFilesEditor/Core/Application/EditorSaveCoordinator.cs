namespace LangFilesEditor.Core.Application;

using Models;
using Abstractions;

// todo: а зачем вообще для сохранения на диск нужен координатор? мб он лишний...
/// <summary>
/// Сохранение на диск и учёт строк, помеченных на удаление.
/// </summary>
internal sealed class EditorSaveCoordinator
{
    private readonly ILanguageRepository _repository;
    private readonly Dictionary<Module, List<TranslationEntry>> _entriesToRemove = new();
    
    /// <summary>
    /// Создаёт координатор сохранения с указанным репозиторием.
    /// </summary>
    /// <param name="repository">Репозиторий для записи изменений на диск.</param>
    public EditorSaveCoordinator(ILanguageRepository repository)
    {
        _repository = repository;
    }
    
    /// <summary>
    /// Сохраняет domains.
    /// </summary>
    /// <param name="domains">Домены с изменёнными модулями.</param>
    /// <param name="languages">Список кодов языков для записи XML.</param>
    /// <returns><see langword="false"/>, если есть некорректные данные или <paramref name="domains"/> равен <see langword="null"/>.</returns>
    public bool Save(ICollection<Domain> domains, IReadOnlyList<string> languages)
    {
        // todo: оно не просто должно возвращать false, как я понимаю, а какое-то уведомлени вверх пользователю отправлять что-ли.
        if (domains == null || HasAnyModuleWithIncorrectData(domains))
        {
            return false;
        }
        
        _repository.Save(domains, languages, ProjectRemovalsToNames());
        _entriesToRemove.Clear();
        return true;
    }
    
    /// <summary>
    /// Запоминает entry для удаления из XML при сохранении.
    /// </summary>
    /// <param name="module">Модуль, содержащий удаляемую строку.</param>
    /// <param name="entry">Строка перевода для удаления.</param>
    public void TrackItemForRemoval(Module module, TranslationEntry entry)
    {
        if (module == null || entry == null)
        {
            return;
        }
        
        if (!_entriesToRemove.TryGetValue(module, out var entries))
        {
            entries = [];
            _entriesToRemove[module] = entries;
        }
        
        if (!entries.Contains(entry))
        {
            entries.Add(entry);
        }
    }
    
    /// <summary>
    /// Проецирует помеченные по ссылкам строки в словарь «имя модуля → имена entries» для репозитория.
    /// Имена берутся актуальные на момент сохранения.
    /// </summary>
    private Dictionary<string, List<string>> ProjectRemovalsToNames()
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var pair in _entriesToRemove)
        {
            var names = pair.Value
                .Select(e => e.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            
            if (string.IsNullOrEmpty(pair.Key.Name) || names.Count == 0)
            {
                continue;
            }
            
            result[pair.Key.Name] = names;
        }
        
        return result;
    }
    
    // todo: думаю, что такой параметр можно было бы настроить в самом domain. Чтобы не нужно было проходится по всем модулям так потом. И domain из-за этого если что можно будет подсвечивать как-то. При необходимости.
    private static bool HasAnyModuleWithIncorrectData(IEnumerable<Domain> domains)
    {
        foreach (var domain in domains)
        {
            if (domain.Modules?.Any(m => m.HasIncorrectData) == true)
            {
                return true;
            }
        }
        
        return false;
    }
}