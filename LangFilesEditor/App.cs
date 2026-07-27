using LangFilesEditor.Core.Application;
using LangFilesEditor.Extensions;

namespace LangFilesEditor;

using System.Windows;
using Exceptions;
using UI.Windows.MainWindow;

// todo: локализация для этого самого приложения должна быть, вероятно, отдельным файлом, а не в репозитории, которое отправляется в облако.
// todo: в модуле проверку по полной загруженности можно было бы делать по метадате (кол-ву загруженных entries и кол-ву xml items которые мы получали в документе, и которые мы отображаем как количество entries). Там и другие механизму на основании этого можно дорабатывать. Простая, но дико полезная вещь.

/// <summary>
/// Точка входа WPF-приложения редактора языковых файлов.
/// </summary>
public partial class App
{
    // Ненулевой код завершения — соглашение ОС о том, что процесс упал с ошибкой
    // (0 = успех). Используется системами запуска/скриптами для отличения сбоя от нормального выхода.
    private const int CriticalErrorExitCode = 1;
    
    /// <summary>
    /// Запускает приложение, создаёт и отображает главное окно.
    /// При критической ошибке показывает сообщение и завершает процесс с кодом ошибки.
    /// </summary>
    /// <param name="e">Аргументы запуска приложения.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            // todo: вот понятия ExtensionCatalog не должно быть в том формате, что он передаёт почему-то основу существования главного VM.
            var dataContext = new MainWindowVM(ToolBar.DockPanelWidth, new EditorBootstrap(ExtensionCatalog.CreateDefaultExtensions()));
            var mainWindow = new MainWindow(dataContext);
            mainWindow.Show();
        }
        catch (CriticalException ex)
        {
            MessageBox.Show(ex.Message);
            Environment.Exit(CriticalErrorExitCode);
        }
    }
}