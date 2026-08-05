namespace LangFilesEditor.UI.Models;

/// <summary>
/// Строка read-only списка языков в настройках.
/// </summary>
public sealed class LanguageDisplayInfo
{
    /// <summary>
    /// Код языка (например, <c>ru-RU</c>).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Человекочитаемое название языка.
    /// </summary>
    public required string Title { get; init; }
}