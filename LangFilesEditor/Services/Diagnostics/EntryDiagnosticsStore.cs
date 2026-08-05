namespace LangFilesEditor.Services.Diagnostics;

using System.Runtime.CompilerServices;
using Models;

/// <summary>
/// Внешнее хранилище диагностики от расширений/скана: ключ — <see cref="TranslationEntry"/> по ссылке
/// (<see cref="ConditionalWeakTable{TKey,TValue}"/>, без утечек при пересоздании записи).
/// Это источник истины для диагностики, опубликованной извне (не встроенная валидация) — он не хранится
/// в самой записи перевода и не привязан к её жизненному циклу, поэтому слой диагностики может владеть
/// этими данными независимо от домена (см. <see cref="EditorDiagnosticsService"/>).
/// </summary>
public sealed class EntryDiagnosticsStore
{
    private readonly ConditionalWeakTable<TranslationEntry, EditorDiagnostic> _byEntry = new();

    /// <summary>
    /// Записывает (или заменяет) диагностику для указанной записи перевода.
    /// </summary>
    /// <param name="entry">Запись перевода, к которой относится диагностика.</param>
    /// <param name="diagnostic">Диагностика (наибольшего приоритета среди опубликованных для этой записи).</param>
    public void Set(TranslationEntry entry, EditorDiagnostic diagnostic)
    {
        _byEntry.Remove(entry);
        _byEntry.Add(entry, diagnostic);
    }

    /// <summary>
    /// Удаляет диагностику указанной записи, если она была установлена.
    /// </summary>
    /// <param name="entry">Запись перевода.</param>
    public void Clear(TranslationEntry entry) => _byEntry.Remove(entry);

    /// <summary>
    /// Пытается получить текущую диагностику записи.
    /// </summary>
    /// <param name="entry">Запись перевода.</param>
    /// <param name="diagnostic">Найденная диагностика или <see langword="null"/>.</param>
    /// <returns><see langword="true"/>, если для записи есть диагностика.</returns>
    public bool TryGet(TranslationEntry entry, out EditorDiagnostic diagnostic) =>
        _byEntry.TryGetValue(entry, out diagnostic);
}