namespace LangFilesEditor.Core.Abstractions;

using System.Collections.ObjectModel;
using Models;
using Services.Loggers;
using Services.RepositoryServices;

/// <summary>
/// Доступ к XML-файлам локализации на диске.
/// </summary>
public interface ILanguageRepository
{
    /// <summary>
    /// Список кодов языков (подпапок LanguageFiles).
    /// </summary>
    /// <param name="languageDirectory">Корневой каталог языковых файлов на диске.</param>
    /// <returns>Упорядоченный список языковых кодов, найденных в каталоге.</returns>
    IReadOnlyList<string> LoadLanguages(string languageDirectory);
    
    // todo: вот этот метод, и метод следующий за ним - под вопросом. ИМХО. Мб нужен какой-то один из них только? Раз есть необходимость подгружать конкретные данные в какой-то момент.
    /// <summary>
    /// Получение доменов по структуре каталога.
    /// </summary>
    /// <param name="languageDirectory">Корневой каталог языковых файлов.</param>
    /// <param name="languages">Список кодов языков проекта.</param>
    /// <param name="isFullLoad">Загружать ли сразу единицы переводов(entries) модулей.</param>
    /// <returns>Коллекция доменов локализации.</returns>
    ObservableCollection<Domain> LoadDomains(
        string languageDirectory,
        IReadOnlyList<string> languages,
        bool isFullLoad = false);
    
    /// <summary>
    /// Получение каталога модулей передаваемого domain без загрузки их entries.
    /// </summary>
    /// <param name="domain">Domain, для которого нужно получить список модулей.</param>
    /// <returns>Коллекция модулей domain с метаданными без строк перевода.</returns>
    Task<ObservableCollection<Module>> LoadModulesAsync(Domain domain);
    
    /// <summary>
    /// Чтение metadata и единиц переводов(entries) модуля с диска.
    /// </summary>
    /// <param name="module">Модуль, данные которого читаются.</param>
    /// <param name="languages">Список кодов языков для загрузки значений перевода.</param>
    /// <param name="cancellationToken">Токен отмены операции чтения.</param>
    /// <returns>Метаданные модуля и список строк перевода.</returns>
    Task<ModuleTranslationData> ReadTranslationEntriesAsync(
        Module module,
        IReadOnlyList<string> languages,
        CancellationToken cancellationToken = default);
    
    // todo: itemsToRemove выглядит странно. Мы же всё-таки сохраняем здесь...
    /// <summary>
    /// Запись изменённых данных доменов на диск.
    /// </summary>
    /// <param name="domains">Домены с изменёнными модулями.</param>
    /// <param name="languages">Список кодов языков для записи XML-файлов.</param>
    /// <param name="itemsToRemove">Словарь «имя модуля → имена удаляемых entries» или <see langword="null"/>.</param>
    void Save(
        ICollection<Domain> domains,
        IReadOnlyList<string> languages,
        IReadOnlyDictionary<string, List<string>> itemsToRemove);
    
    /// <summary>
    /// Слияние LanguageFiles в каталог установленного ModPlus.
    /// </summary>
    /// <param name="notifications">Сервис уведомлений о ходе и ошибках слияния.</param>
    void MergeWithWorkingDirectory(NotificationService notifications);
}