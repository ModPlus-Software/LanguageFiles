namespace LangFilesEditor;

using System.IO;
using Utils;

/// <summary>
/// Глобальные константы путей к файлам локализации и имён доменов.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Абсолютный путь к каталогу <c>LanguageFiles</c> в корне solution.
    /// </summary>
    public static readonly string LanguageFilesDirectory =
        Path.Combine(DirectoryUtils.GetSolutionDirectory(), "LanguageFiles");
    
    /// <summary>
    /// Разделитель частей имени XML-файла домена, например <c>Revit_Architecture</c>.
    /// </summary>
    public const string DomainNamesSeparator = "_";
    
    /// <summary>
    /// Имя общего домена по соглашению об именах файлов локализации (файл <c>Common.xml</c>).
    /// Единственное место, где зашито это имя; остальной код использует <see cref="Models.Domain.IsCommon"/>.
    /// </summary>
    public const string CommonDomainName = "Common";
    
    /// <summary>
    /// Префикс комментария, которым помечается строка перевода к удалению в будущей версии.
    /// После префикса следует номер версии, например <c>todo remove after 1.2.3</c>.
    /// </summary>
    public const string RemoveAfterCommentPrefix = "todo remove after ";
}