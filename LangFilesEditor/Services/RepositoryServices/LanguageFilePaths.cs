namespace LangFilesEditor.Services.RepositoryServices;

using System.IO;

// todo: мб и есть нужна конечно... Но это яно не Services, а Utils. Т. е. я бы как минимум это переместил в другую папку, а как максимум посмотрил использования и возможно избавился бы от этого класса.
/// <summary>
/// Построение путей к XML-файлам локализации в каталоге LanguageFiles.
/// </summary>
internal static class LanguageFilePaths
{
    /// <summary>
    /// Возвращает каталог одного языка внутри корня LanguageFiles.
    /// </summary>
    /// <param name="languageFilesRoot">Корневой каталог LanguageFiles.</param>
    /// <param name="languageName">Код языка (имя подпапки).</param>
    public static string GetLanguageDirectory(string languageFilesRoot, string languageName) =>
        Path.Combine(languageFilesRoot, languageName);
    
    /// <summary>
    /// Возвращает полный путь к XML-файлу домена/источника для языка.
    /// </summary>
    /// <param name="languageFilesRoot">Корневой каталог LanguageFiles.</param>
    /// <param name="languageName">Код языка.</param>
    /// <param name="sourceFileName">Имя XML-файла без расширения.</param>
    public static string GetSourceFilePath(string languageFilesRoot, string languageName, string sourceFileName) =>
        Path.Combine(GetLanguageDirectory(languageFilesRoot, languageName), $"{sourceFileName}.xml");
    
    /// <summary>
    /// Перечисляет существующие пути к файлу источника во всех языковых подпапках.
    /// </summary>
    /// <param name="languageFilesRoot">Корневой каталог LanguageFiles.</param>
    /// <param name="sourceFileName">Имя XML-файла без расширения.</param>
    public static IEnumerable<string> EnumerateExistingSourceFilePaths(string languageFilesRoot, string sourceFileName)
    {
        if (!Directory.Exists(languageFilesRoot))
        {
            yield break;
        }
        
        foreach (var languageDirectory in Directory.GetDirectories(languageFilesRoot))
        {
            var filePath = Path.Combine(languageDirectory, $"{sourceFileName}.xml");
            if (File.Exists(filePath))
            {
                yield return filePath;
            }
        }
    }
    
    // todo: если не привязано к ui (а для методов там очень специфические требования) - то бесполезно
    /// <summary>
    /// Проверяет, существует ли модуль хотя бы в одном языковом файле источника.
    /// </summary>
    /// <param name="languageFilesRoot">Корневой каталог LanguageFiles.</param>
    /// <param name="languages">Список кодов языков для проверки.</param>
    /// <param name="sourceFileName">Имя XML-файла без расширения.</param>
    /// <param name="moduleName">Имя модуля (локальное имя XML-узла).</param>
    /// <returns><c>true</c>, если узел модуля найден в одном из файлов.</returns>
    public static bool ModuleExistsInLanguages(
        string languageFilesRoot,
        IReadOnlyList<string> languages,
        string sourceFileName,
        string moduleName)
    {
        if (string.IsNullOrEmpty(sourceFileName))
        {
            return false;
        }
        
        foreach (var languageName in languages)
        {
            var filePath = GetSourceFilePath(languageFilesRoot, languageName, sourceFileName);
            if (ModuleXmlSerializer.FileContainsModule(filePath, moduleName))
            {
                return true;
            }
        }
        
        return false;
    }
}