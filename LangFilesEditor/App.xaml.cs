namespace LangFilesEditor;

using System.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        var mainContext = new MainContext(mainWindow);
        mainWindow.DataContext = mainContext;

        mainWindow.Loaded += (_, _) => mainContext.Load();
        mainWindow.Closing += (_, _) => mainContext.Save();

        mainWindow.Show();
    }
}