namespace LangFilesEditor.Core.Abstractions;

using System.Windows.Input;

/// <summary>
/// Команда панели кнопок инструментов.
/// </summary>
public sealed class LangFilesEditorToolbarCommand
{
    /// <summary>
    /// Название команды.
    /// </summary>
    public required string Label { get; init; }
    
    /// <summary>
    /// Команда.
    /// </summary>
    public required ICommand Command { get; init; }
}