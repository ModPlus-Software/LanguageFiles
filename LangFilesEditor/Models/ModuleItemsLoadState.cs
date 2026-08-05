namespace LangFilesEditor.Models;

/// <summary>
/// Состояние загрузки строк перевода модуля в память редактора.
/// </summary>
public enum ModuleItemsLoadState
{
    /// <summary>
    /// Строки не загружены (только каталог с диска).
    /// </summary>
    None,

    /// <summary>
    /// Загружены только строки, отображаемые в режиме фильтра диагностики.
    /// </summary>
    PartialDiagnostic,

    /// <summary>
    /// Модуль полностью загружен с диска.
    /// </summary>
    Full,
}