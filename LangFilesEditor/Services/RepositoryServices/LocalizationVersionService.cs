namespace LangFilesEditor.Services.RepositoryServices;

using System.IO;
using System.Net.Http;
using System.Windows;
using System.Xml.Linq;

/// <summary>
/// Чтение и запись версии пакета локализации (локально и с удалённого хранилища).
/// </summary>
public class LocalizationVersionService
{
    // Один общий HttpClient на процесс: создание нового на каждый запрос приводит к исчерпанию сокетов.
    private static readonly HttpClient HttpClient = new();
    
    /// <summary>
    /// Читает локальную версию из файла Version.txt в каталоге LanguageFiles решения.
    /// </summary>
    /// <returns>Версия локализации или <c>null</c> при ошибке чтения.</returns>
    public Version GetLocalVersion()
    {
        try
        {
            return Version.Parse(File.ReadAllText(Path.Combine(Constants.LanguageFilesDirectory, "Version.txt")));
        }
        catch
        {
            // todo: вот для таких штук должно передаваться какое-то уведомление во view где было бы что-то вроде показать пользователю сообщение, а там уже оно само решало каким образом ему это показывать.
            MessageBox.Show("Failed get local version!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }
    
    /// <summary>
    /// Возвращает актуальную версию как максимум из локальной и удалённой.
    /// </summary>
    /// <param name="sourceLanguagesDirectory">Каталог LanguageFiles для чтения Version.txt.</param>
    /// <returns>Более новая из локальной и удалённой версий.</returns>
    public async Task<Version> GetVersion(string sourceLanguagesDirectory)
    {
        var localVersion = Version.Parse(await File.ReadAllTextAsync(Path.Combine(sourceLanguagesDirectory, "Version.txt")));
        var remoteVersion = await GetRemoteVersion();
        return localVersion > remoteVersion ? localVersion : remoteVersion;
    }
    
    /// <summary>
    /// Записывает указанную версию в локальный файл Version.txt.
    /// </summary>
    /// <param name="version">Версия для сохранения.</param>
    public void SetLocalVersion(Version version)
    {
        File.WriteAllText(Path.Combine(Constants.LanguageFilesDirectory, "Version.txt"), version.ToString());
    }
    
    // todo: это не окей, потому что, вероятно, она должна подтягиваться с git
    /// <summary>
    /// Загружает версию локализации с удалённого XML-каталога ModPlus.
    /// </summary>
    /// <returns>Удалённая версия или <c>null</c>, если запрос не удался.</returns>
    public async Task<Version> GetRemoteVersion()
    {
        try
        {
            const string url = "https://storage.modplus.org/Languages/Langs.xml"; // todo: см. todo в этом методе ниже. Или на что стоит обращать внимания если здесь указание на локализацию на сайте?
            var str = await HttpClient.GetStringAsync(url);
            // todo: мб добавить какую-то подкачку прямо с git? мне кажется, что это было бы удобнее, чем постоянно пулить. Т. е. если изменения только в рамках файлов локализации, то можно было бы добавить более простые способы коммитов, и подгрузки при отличии версий. А если отличается версия и самого редактора, то стоило бы писать напоминание о новой версии, и что было бы здорово с гита самому запуллить новую версию
            return !string.IsNullOrEmpty(str)
                ? Version.Parse(XElement.Parse(str).Elements("lang").First().Attribute("Version")!.Value)
                : null;
        }
        catch
        {
            return null;
        }
    }
}