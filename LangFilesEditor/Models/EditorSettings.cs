using LangFilesEditor.UI.Models;

namespace LangFilesEditor.Models;

/// <summary>
/// Пользовательские настройки редактора, сохраняемые между сеансами.
/// </summary>
public sealed class EditorSettings
{
    /// <summary>
    /// Запускать ли фоновое сканирование диагностики после загрузки каталога модулей.
    /// </summary>
    public bool RunStartupDiagnosticsScan { get; set; } = true;

    /// <summary>
    /// Выбранная тема интерфейса.
    /// </summary>
    public EditorAppTheme Theme { get; set; } = EditorAppTheme.Light;
}