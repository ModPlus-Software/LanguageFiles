namespace LangFilesEditor.Core.Abstractions;

using System.Collections.Generic;
using Models;

// todo: пу-пу-пу. В странном формате оно всё-таки
/// <summary>
/// Публикация статусов (для расширений).
/// </summary>
public interface IDiagnosticsPublisher
{
    /// <summary>
    /// Заменяет весь набор диагностик указанного источника на переданный.
    /// </summary>
    /// <param name="source">Уникальный ключ источника (например, id расширения).</param>
    /// <param name="diagnostics">Новый полный набор диагностик источника.</param>
    void Publish(string source, IEnumerable<EditorDiagnostic> diagnostics);
    
    /// <summary>
    /// Удаляет все диагностики указанного источника.
    /// </summary>
    /// <param name="source">Ключ источника, опубликованного ранее.</param>
    void Clear(string source);
}