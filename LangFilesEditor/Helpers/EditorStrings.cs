namespace LangFilesEditor.Helpers;

/// <summary>
/// todo: временно вынесенная локализация из точек раскиданных по приложению
/// </summary>
internal static class EditorStrings
{
    /// <summary>
    /// Текст status bar, когда не выбраны ни домен, ни модуль, ни запись, и не активен поиск.
    /// </summary>
    public const string StatusBarReady = "Готово";
    
    /// <summary>
    /// Метка режима поиска в status bar.
    /// </summary>
    public const string StatusBarSearchMode = "Режим поиска";
    
    /// <summary>
    /// Форматирует текст «Открыто модулей: N» для status bar.
    /// </summary>
    /// <param name="openModulesCount">Число открытых модулей.</param>
    /// <returns>Готовый текст сегмента/подсказки status bar.</returns>
    public static string FormatOpenModulesCount(int openModulesCount) => $"Открыто модулей: {openModulesCount}";
    
    /// <summary>
    /// Заголовок операции прогресса при загрузке данных для поиска (<see cref="Core.Application.EditorSearchCoordinator"/>).
    /// </summary>
    public const string LoadingSearchData = "Загрузка данных для поиска";
    
    /// <summary>
    /// Заголовок операции прогресса при сканировании диагностики (<see cref="Services.Diagnostics.LocalizationDiagnosticsScanner"/>).
    /// </summary>
    public const string ScanningDiagnostics = "Сканирование диагностики";
    
    /// <summary>
    /// Форматирует заголовок операции загрузки конкретного модуля для диагностики
    /// (<see cref="Core.Application.EditorDiagnosticLoadCoordinator"/>).
    /// </summary>
    /// <param name="moduleName">Имя загружаемого модуля.</param>
    /// <returns>Заголовок для <see cref="Services.EditorOperationTracker"/>.</returns>
    public static string FormatModuleLoadTitle(string moduleName) => $"Загрузка «{moduleName}»";
    
    /// <summary>
    /// Форматирует заголовок операции загрузки каталога модулей domain
    /// (<see cref="Core.Application.DomainModuleLoadCoordinator"/>).
    /// </summary>
    /// <param name="domainName">Имя domain, каталог модулей которого загружается.</param>
    /// <returns>Заголовок для <see cref="Services.EditorOperationTracker"/>.</returns>
    public static string FormatModuleCatalogLoadTitle(string domainName) => $"Загрузка модулей: {domainName}";
    
    /// <summary>
    /// Сообщение об отсутствии каталога языковых файлов
    /// (<see cref="Services.RepositoryServices.LanguageRepositoryService.GetLanguageDirectory"/>).
    /// </summary>
    public const string LanguageDirectoryNotFound = "Language files directory not found.";
    
    /// <summary>
    /// Сообщение о том, что установленный ModPlus не найден при слиянии языковых файлов.
    /// </summary>
    public const string InstalledModPlusNotFound = "Installed ModPlus not found!";
    
    /// <summary>
    /// Форматирует сообщение с версией целевого языка при слиянии.
    /// </summary>
    /// <param name="version">Версия локализации.</param>
    public static string FormatTargetLanguageVersion(object version) => $"Target language version: {version}";
    
    /// <summary>
    /// Форматирует сообщение о начале обработки языка при слиянии.
    /// </summary>
    /// <param name="languageName">Код обрабатываемого языка.</param>
    public static string FormatProcessLanguage(string languageName) => $"Process language: {languageName}";
    
    /// <summary>
    /// Форматирует путь к целевому каталогу языковых файлов при слиянии.
    /// </summary>
    /// <param name="targetLanguageDirectory">Путь к каталогу назначения.</param>
    public static string FormatTargetLanguagesDirectory(string targetLanguageDirectory) =>
        $"Target languages directory: {targetLanguageDirectory}";
    
    /// <summary>
    /// Форматирует сообщение о создании языкового файла при слиянии.
    /// </summary>
    /// <param name="languageName">Код языка, для которого создан файл.</param>
    public static string FormatLanguageFileCreated(string languageName) => $"Language file for {languageName} created";
    
    /// <summary>
    /// Сообщение о завершении длительной операции (слияние, сканирование и т.п.).
    /// </summary>
    public const string Done = "Done";
    
    /// <summary>
    /// Форматирует сообщение о начале обработки части (domain prefix) при построении объединённого документа слияния.
    /// </summary>
    /// <param name="domainPrefix">Префикс имён доменов этой части (например, «Common», «AutoCAD»).</param>
    public static string FormatProcessMergePart(string domainPrefix) => $"    Process part: {domainPrefix}";
    
    /// <summary>
    /// Форматирует сообщение об ошибке удаления файла при подготовке каталога слияния.
    /// </summary>
    /// <param name="filePath">Путь к файлу, который не удалось удалить.</param>
    /// <param name="exceptionMessage">Текст исключения.</param>
    public static string FormatDeleteFileFailed(string filePath, string exceptionMessage) =>
        $"Failed delete file {filePath}.\nException: {exceptionMessage}.\nDelete it manually and try again";
    
    // todo: локализация
    /// <summary>Подсказка: пустое имя ключа.</summary>
    public const string EntryTooltipEmptyName = "Пустое имя ключа";
    
    /// <summary>Подсказка: имя ключа начинается с цифры.</summary>
    public const string EntryTooltipNameStartsWithDigit = "Имя ключа не должно начинаться с цифры";
    
    /// <summary>Подсказка: есть пустые значения перевода.</summary>
    public const string EntryTooltipEmptyValues = "Есть пустые значения перевода";
    
    /// <summary>Подсказка: дубликат имени ключа.</summary>
    public const string EntryTooltipDuplicateName = "Дубликат имени ключа";
    
    /// <summary>Подсказка: дубликат набора значений.</summary>
    public const string EntryTooltipDuplicateValues = "Дубликат набора значений перевода";
    
    /// <summary>
    /// Форматирует подсказку о пометке ключа к удалению.
    /// </summary>
    /// <param name="version">Версия, после которой ключ удаляется.</param>
    public static string FormatEntryMarkedForDeletion(string version) =>
        string.IsNullOrWhiteSpace(version)
            ? "Помечен к удалению"
            : $"Помечен к удалению после версии {version.Trim()}";
}