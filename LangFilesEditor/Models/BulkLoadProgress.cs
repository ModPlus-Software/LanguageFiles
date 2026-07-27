namespace LangFilesEditor.Models;

/// <summary>
/// Прогресс пакетной подгрузки записей в модуль.
/// </summary>
/// <param name="Current">Число уже загруженных записей.</param>
/// <param name="Total">Ожидаемое общее число записей.</param>
public readonly record struct BulkLoadProgress(int Current, int Total);