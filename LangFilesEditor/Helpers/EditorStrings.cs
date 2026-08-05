namespace LangFilesEditor.Helpers;

/// <summary>
/// Тексты интерфейса редактора, собранные в одном месте: сообщения status bar, заголовки
/// операций и уведомления. Локализация самого редактора живёт здесь, а не в файлах
/// <c>LanguageFiles</c> — те принадлежат продукту, а не инструменту.
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
    /// Заголовок операции: сколько операций выполняется одновременно.
    /// </summary>
    /// <param name="operationsCount">Число активных операций.</param>
    public static string FormatOperationsCount(int operationsCount) => $"Операций: {operationsCount}";

    /// <summary>
    /// Сообщение об отсутствии корневого каталога решения.
    /// </summary>
    public const string SolutionDirectoryNotFound = "Не найден корневой каталог решения (LanguageFiles).";

    /// <summary>
    /// Сообщение об ошибке чтения локальной версии пакета локализации.
    /// </summary>
    public const string LocalVersionReadFailed = "Не удалось прочитать локальную версию локализации";

    /// <summary>
    /// Сообщение о некорректно введённой версии.
    /// </summary>
    public const string EnterValidVersion = "Введите корректную версию!";

    /// <summary>
    /// Заголовок окна с сообщением об ошибке.
    /// </summary>
    public const string ErrorCaption = "Ошибка";

    /// <summary>
    /// Значение, отображаемое вместо неизвестной версии локализации.
    /// </summary>
    public const string UnknownVersion = "—";

    /// <summary>
    /// Отображаемое имя светлой темы.
    /// </summary>
    public const string ThemeLight = "Светлая";

    /// <summary>
    /// Отображаемое имя тёмной темы.
    /// </summary>
    public const string ThemeDark = "Тёмная";

    /// <summary>
    /// Название категории диагностики «ошибки».
    /// </summary>
    public const string DiagnosticCategoryErrors = "Ошибки";

    /// <summary>
    /// Название категории диагностики «предупреждения».
    /// </summary>
    public const string DiagnosticCategoryWarnings = "Предупреждения";

    /// <summary>
    /// Название категории диагностики «обновления».
    /// </summary>
    public const string DiagnosticCategoryUpdates = "Обновления";

    /// <summary>
    /// Сообщение о том, что в буфере обмена нет скопированной строки перевода.
    /// </summary>
    public const string ClipboardHasNoEntry = "В буфере обмена нет скопированной строки перевода.";

    /// <summary>
    /// Сообщение о запрете удаления строки, отмеченной комментарием.
    /// </summary>
    public const string RemoveCommentedEntryForbidden = "Позиции, отмеченные комментарием, удалять нельзя!";

    /// <summary>
    /// Вопрос перед удалением строки, не помеченной к удалению в будущей версии.
    /// </summary>
    public const string RemoveEntryQuestion =
        "Нельзя удалять строки из локализации, если плагин уже в релизе!" +
        " Такие строки следует отмечать комментарием с пометкой об удалении." +
        "\nТочно удалить?";

    /// <summary>
    /// Сообщение о невозможности сохранить изменения из-за некорректных данных.
    /// </summary>
    public const string SaveBlockedByIncorrectData =
        "Есть некорректные данные — изменения не сохранены." +
        " Исправьте ошибки или закройте без сохранения.";

    /// <summary>
    /// Форматирует подсказку окна ручного импорта.
    /// </summary>
    /// <param name="languagesOrder">Перечисление языков в порядке вставки строк.</param>
    public static string FormatImportNote(string languagesOrder) =>
        $"Вставьте текст, содержащий перевод фраз в порядке: {languagesOrder}." +
        " Перевод для каждого языка должен быть на новой строке." +
        " Можно добавлять перевод сразу для нескольких новых строк." +
        " Пустые строки игнорируются";

    /// <summary>
    /// Форматирует подсказку окна автоматического импорта (текст вместе с тегами).
    /// </summary>
    /// <param name="languagesOrder">Перечисление языков в порядке вставки строк.</param>
    public static string FormatImportWithTagsNote(string languagesOrder) =>
        $"Вставьте текст (вместе с тегами), содержащий перевод фраз в порядке: {languagesOrder}." +
        " Перевод для каждого языка должен быть на новой строке." +
        " Можно добавлять перевод сразу для нескольких новых строк. Пустые строки игнорируются";

    /// <summary>
    /// Форматирует сообщение о добавлении ключа пользователем.
    /// </summary>
    /// <param name="entryName">Имя добавленного ключа.</param>
    /// <param name="moduleName">Имя модуля.</param>
    public static string FormatEntryAddedByUser(string entryName, string moduleName) =>
        $"Добавлен ключ «{entryName}» в «{moduleName}»";

    /// <summary>
    /// Форматирует сообщение об импорте ключа из кода.
    /// </summary>
    /// <param name="entryName">Имя ключа.</param>
    /// <param name="moduleName">Имя модуля.</param>
    public static string FormatEntryImportedFromCode(string entryName, string moduleName) =>
        $"Импорт из кода: «{entryName}» в «{moduleName}»";

    /// <summary>
    /// Форматирует сообщение об импорте строки перевода.
    /// </summary>
    /// <param name="entryName">Имя ключа.</param>
    /// <param name="moduleName">Имя модуля.</param>
    public static string FormatEntryImported(string entryName, string moduleName) =>
        $"Импорт строки «{entryName}» в «{moduleName}»";

    /// <summary>
    /// Форматирует подсказку о пометке ключа к удалению.
    /// </summary>
    /// <param name="version">Версия, после которой ключ удаляется.</param>
    public static string FormatEntryMarkedForDeletion(string version) =>
        string.IsNullOrWhiteSpace(version)
            ? "Помечен к удалению"
            : $"Помечен к удалению после версии {version.Trim()}";
}