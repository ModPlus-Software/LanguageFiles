namespace LangFilesEditor.Models;

using System;

// todo: переименовать на DiagnosticIssue, сделать так чтобы нигде в ui (view) это не использовалось, так как это model всё-таки, и проверить наименования остальных классов из этого разряда. Вроде DiagnosticModuleEntry(который вроде бы тоже чисто model[соответственно область применения]) и DiagnosticCategory
// todo: мб диагностику вообще в отдельные файлы или как-то так повыносить? Наверное не стоит, но явно бы сделать схожешсть их названий, чтобы в какие-то логические группы группировались.

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
        // todo: мне не нравится здесь проброс ошибки.
        Module = module ?? throw new ArgumentNullException(nameof(module));
        Message = message;
        Entry = entry;
    }
    
    /// <summary>
    /// Категория диагностики.
    /// </summary>
    public DiagnosticSeverity Severity { get; }
    
    // todo: вопрос нужно ли так здесь знать о модуле... пу-пу-пу. Скорее всего нет. По крайней мере на мой взгляд это может быть лишним. Но может быть и окей..
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