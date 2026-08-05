namespace LangFilesEditor.Services;

using System.Windows;
using Core.Abstractions;
using Loggers;
using Diagnostics;
using UI.Windows.DialogWindows;

/// <summary>
/// Реализация диалогов редактора.
/// </summary>
public class DialogService : IDialogService
{
    private readonly Store _host;
    private readonly IEditorWorkspace _workspace;
    private readonly IExtensionHost _extensionHost;
    private readonly EditorDiagnosticsService _diagnostics;
    private readonly ILanguageRepository _repository;
    private readonly EditorSettingsStore _settings;

    /// <summary>
    /// Создаёт сервис диалогов с host API.
    /// </summary>
    public DialogService(
        Store host,
        IEditorWorkspace workspace,
        IExtensionHost extensionHost,
        EditorDiagnosticsService diagnostics,
        ILanguageRepository repository,
        EditorSettingsStore settings)
    {
        _host = host;
        _workspace = workspace;
        _extensionHost = extensionHost;
        _diagnostics = diagnostics;
        _repository = repository;
        _settings = settings;
    }

    /// <inheritdoc />
    public bool? ShowImportWindowWithCheckbox()
    {
        var win = new ImportWindowWithCheckbox(_host.Languages)
        {
            DataContext = new ImportVM(_workspace, _host),
        };

        return win.ShowDialog();
    }

    /// <inheritdoc />
    public bool? ShowImportWindow()
    {
        var win = new ImportWindow(_host.Languages)
        {
            DataContext = new ImportVM(_workspace, _host),
        };

        return win.ShowDialog();
    }

    /// <inheritdoc />
    public bool? ShowMergerWindow()
    {
        var win = new Merger
        {
            Owner = Application.Current.MainWindow,
            DataContext = new MergerVM(_host, _repository, this, new NotificationService()),
        };

        return win.ShowDialog();
    }

    /// <inheritdoc />
    public bool? ShowMarkForDeletionWindow()
    {
        var win = new MarkForDeletionWindow(_workspace)
        {
            // Без владельца WindowStartupLocation="CenterOwner" откатывается
            // к позиции по умолчанию и окно появляется в углу экрана.
            Owner = Application.Current.MainWindow,
        };

        return win.ShowDialog();
    }

    /// <inheritdoc />
    public void ShowSettingsWindow()
    {
        var win = new SettingsWindow
        {
            Owner = Application.Current.MainWindow,
            DataContext = new SettingsWindowVM(_host, _extensionHost, _diagnostics, _repository, _settings),
        };

        win.ShowDialog();
    }

    /// <inheritdoc />
    public void ShowMessageWindow(string message) =>
        MessageBox.Show(message, "LangFilesEditor", MessageBoxButton.OK, MessageBoxImage.Information);

    /// <inheritdoc />
    public bool ShowQuestionWindow(string message) =>
        MessageBox.Show(message, "LangFilesEditor", MessageBoxButton.YesNo, MessageBoxImage.Question)
        == MessageBoxResult.Yes;
}