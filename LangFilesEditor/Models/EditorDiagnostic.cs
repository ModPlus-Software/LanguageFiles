namespace LangFilesEditor.Models;

using System;

/// <summary>
/// Единица диагностики, опубликованная core-валидацией или расширением.
/// Привязана к модулю и (опционально) к конкретной строке перевода для подсветки.
/// </summary>
public sealed class EditorDiagnostic
{
    /// <summary>
    /// Создаёт диагностику для модуля и, при необходимости, конкретной строки.
    /// </summary>
    /// <param name="severity">Категория диагностики.</param>
    /// <param name="module">Модуль, к которому относится диагностика.</param>
    /// <param name="message">Текст для подсказки и подробного списка.</param>
    /// <param name="entry">Строка перевода для подсветки в рабочей области (необязательно).</param>
    public EditorDiagnostic(
        DiagnosticSeverity severity,
        Module module,
        string message = "",
        TranslationEntry entry = null)
    {
        Severity = severity;
        Module = module ?? throw new ArgumentNullException(nameof(module));
        Message = message;
        Entry = entry;
    }

    /// <summary>
    /// Категория диагностики.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// Модуль, к которому относится диагностика.
    /// </summary>
    public Module Module { get; }

    /// <summary>
    /// Строка перевода для подсветки, либо <c>null</c> для диагностики уровня модуля.
    /// </summary>
    public TranslationEntry Entry { get; }

    /// <summary>
    /// Человекочитаемое описание проблемы.
    /// </summary>
    public string Message { get; }
}