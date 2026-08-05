namespace LangFilesEditor;

using Utils;

/// <summary>
/// Глобальные константы путей к файлам локализации и имён доменов.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Абсолютный путь к каталогу <c>LanguageFiles</c>. Каталог ищется относительно
    /// запущенного приложения — см. <see cref="LanguageFilesLocator"/>.
    /// </summary>
    /// <remarks>
    /// Свойство, а не поле: инициализатор статического поля выполняется в инициализаторе типа,
    /// и исключение из него CLR оборачивает в <see cref="System.TypeInitializationException"/>.
    /// Обработчик <c>CriticalException</c> в <c>App.OnStartup</c> такое исключение не ловил,
    /// и вместо понятного сообщения редактор падал с системным окном ошибки. Заодно порча
    /// инициализатора типа делала недоступными и остальные члены класса.
    /// </remarks>
    public static string LanguageFilesDirectory => LanguageFilesLocator.Resolve();

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
