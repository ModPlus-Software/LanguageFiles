namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Windows;

/// <summary>
/// Окно настроек редактора локализации.
/// </summary>
public partial class SettingsWindow
{
    /// <summary>
    /// Инициализирует разметку окна настроек.
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
        WindowStyle = WindowStyle.ToolWindow;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    private void Close_OnClick(object sender, RoutedEventArgs e) => Close();
}