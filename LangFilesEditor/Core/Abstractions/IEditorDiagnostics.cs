namespace LangFilesEditor.Core.Abstractions;

using System.Collections.Generic;
using System.ComponentModel;
using Models;

/// <summary>
/// Сводка диагностики для UI: фиксированные категории (ошибки/предупреждения/обновления),
/// каждая с суммарным счётчиком и разбивкой по модулям.
/// </summary>
public interface IEditorDiagnostics : INotifyPropertyChanged
{
    /// <summary>
    /// Категория ошибок (красный).
    /// </summary>
    DiagnosticCategory Errors { get; }

    /// <summary>
    /// Категория предупреждений (оранжевый).
    /// </summary>
    DiagnosticCategory Warnings { get; }

    /// <summary>
    /// Категория обновлений (зелёный).
    /// </summary>
    DiagnosticCategory Updates { get; }

    /// <summary>
    /// Все категории в порядке отображения.
    /// </summary>
    IReadOnlyList<DiagnosticCategory> Categories { get; }

    /// <summary>
    /// Есть ли хотя бы одна проблема любой категории.
    /// </summary>
    bool HasAny { get; }

    /// <summary>
    /// Пытается получить диагностику расширения/скана для конкретной записи перевода — независимо
    /// от того, что запись сама по себе о диагностике не знает (диагностика хранится во внешнем слое).
    /// </summary>
    /// <param name="entry">Запись перевода.</param>
    /// <param name="diagnostic">Найденная диагностика или <see langword="null"/>.</param>
    /// <returns><see langword="true"/>, если для записи есть диагностика.</returns>
    bool TryGetEntryDiagnostic(TranslationEntry entry, out EditorDiagnostic diagnostic);
}