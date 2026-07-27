namespace LangFilesEditor.Models;

// todo: <see cref="TranslationEntryEventArgs"/>
/// <summary>
/// Контекст мутации коллекции записей модуля.
/// </summary>
/// <param name="Source">Источник добавления записи.</param>
/// <param name="BulkProgress">Прогресс пакетной загрузки; указывается при чтении с диска.</param>
public readonly record struct TranslationEntryAddContext(
    TranslationEntryAddSource Source,
    BulkLoadProgress? BulkProgress = null);