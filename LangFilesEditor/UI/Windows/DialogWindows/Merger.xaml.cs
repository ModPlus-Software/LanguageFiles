namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Windows;

/// <summary>
/// Окно слияния локализации с рабочим каталогом ModPlus.
/// </summary>
public partial class Merger
{
    /// <summary>
    /// Создаёт окно merger с разметкой из XAML.
    /// </summary>
    public Merger()
    {
        InitializeComponent();
        WindowStyle = WindowStyle.ToolWindow;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}