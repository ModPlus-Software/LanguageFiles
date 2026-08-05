namespace LangFilesEditor.Extensions;

using Core.Abstractions;

/// <summary>
/// Точка входа: все встроенные расширения из папки Extensions.
/// </summary>
public static class ExtensionCatalog
{
    /// <summary>
    /// Создаёт список расширений, подключаемых при старте.
    /// </summary>
    /// <returns>Встроенные реализации <see cref="ILangFilesEditorExtension"/>.</returns>
    public static IReadOnlyList<ILangFilesEditorExtension> CreateDefaultExtensions() => [];
}