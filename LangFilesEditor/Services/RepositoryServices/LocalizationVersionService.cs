namespace LangFilesEditor.Services.RepositoryServices;

using System.IO;
using System.Net.Http;
using System.Windows;
using System.Xml.Linq;
using Helpers;

/// <summary>
/// Чтение и запись версии пакета локализации (локально и с удалённого хранилища).
/// </summary>
public class LocalizationVersionService
{
    // Каталог версий языковых пакетов ModPlus: источник актуальной (удалённой) версии локализации.
    private const string RemoteVersionCatalogUrl = "https://storage.modplus.org/Languages/Langs.xml";

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
            MessageBox.Show(
                EditorStrings.LocalVersionReadFailed,
                EditorStrings.ErrorCaption,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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

    /// <summary>
    /// Загружает версию локализации с удалённого XML-каталога ModPlus.
    /// </summary>
    /// <returns>Удалённая версия или <c>null</c>, если запрос не удался.</returns>
    public async Task<Version> GetRemoteVersion()
    {
        try
        {
            var str = await HttpClient.GetStringAsync(RemoteVersionCatalogUrl);
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