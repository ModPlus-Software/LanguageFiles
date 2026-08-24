using LangFilesEditor.Services.RepositoryServices;

namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Core.Abstractions;
using Helpers;
using Models;
using Services;
using Services.Diagnostics;
using ModPlusAPI;
using ModPlusAPI.Mvvm;
using Constants = LangFilesEditor.Constants;

/// <summary>
/// ViewModel окна настроек редактора.
/// </summary>
public sealed class SettingsWindowVM : ObservableObject
{
    private readonly IEditorSession _session;
    private readonly EditorDiagnosticsService _diagnostics;
    private readonly ILanguageRepository _repository;
    private readonly EditorSettingsStore _settings;
    private bool _runStartupDiagnosticsScan;
    private int _initialRowsPerFrame;
    private int _maxRowsPerFrame;
    private string _selectedThemeName;
    private ICommand _openLanguageFilesFolderCommand;
    private ICommand _rescanDiagnosticsCommand;

    /// <summary>
    /// Создаёт ViewModel настроек на основе host API и сервисов редактора.
    /// </summary>
    public SettingsWindowVM(
        IEditorSession session,
        IExtensionHost extensionHost,
        EditorDiagnosticsService diagnostics,
        ILanguageRepository repository,
        EditorSettingsStore settings)
    {
        _session = session;
        _diagnostics = diagnostics;
        _repository = repository;
        _settings = settings;
        _runStartupDiagnosticsScan = settings.Current.RunStartupDiagnosticsScan;
        _initialRowsPerFrame = settings.Current.InitialRowsPerFrame;
        _maxRowsPerFrame = settings.Current.MaxRowsPerFrame;
        _selectedThemeName = ToThemeName(settings.Current.Theme);

        LocalVersion = new LocalizationVersionService().GetLocalVersion()?.ToString() ?? EditorStrings.UnknownVersion;
        LanguageFilesPath = Constants.LanguageFilesDirectory;
        Languages = new ObservableCollection<LanguageDisplayInfo>(
            LanguageDisplayHelper.BuildDisplayList(session.Languages));
        Extensions = extensionHost.ToolbarCommands
            .Select(command => command.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct()
            .ToList();
        HasExtensions = Extensions.Count > 0;
        ThemeOptions = [EditorStrings.ThemeLight, EditorStrings.ThemeDark];

        _diagnostics.PropertyChanged += OnDiagnosticsPropertyChanged;
    }

    /// <summary>
    /// Локальная версия пакета локализации.
    /// </summary>
    public string LocalVersion { get; }

    /// <summary>
    /// Абсолютный путь к каталогу LanguageFiles (read-only).
    /// </summary>
    public string LanguageFilesPath { get; }

    /// <summary>
    /// Языки проекта, как их видит редактор (read-only).
    /// </summary>
    public ObservableCollection<LanguageDisplayInfo> Languages { get; }

    /// <summary>
    /// Подписи зарегистрированных расширениями команд.
    /// </summary>
    public IReadOnlyList<string> Extensions { get; }

    /// <summary>
    /// Есть ли подключённые расширения.
    /// </summary>
    public bool HasExtensions { get; }

    /// <summary>
    /// Нет ни одного подключённого расширения (для заглушки в окне настроек).
    /// </summary>
    public bool NoExtensions => !HasExtensions;

    /// <summary>
    /// Есть ли загруженные языки.
    /// </summary>
    public bool HasLanguages => Languages.Count > 0;

    /// <summary>
    /// Не найдено ни одного языка (для заглушки в окне настроек).
    /// </summary>
    public bool NoLanguages => !HasLanguages;

    /// <summary>
    /// Варианты темы для ComboBox.
    /// </summary>
    public IReadOnlyList<string> ThemeOptions { get; }

    /// <summary>
    /// Выбранная тема (отображаемое имя).
    /// </summary>
    public string SelectedThemeName
    {
        get => _selectedThemeName;
        set
        {
            if (_selectedThemeName == value)
            {
                return;
            }

            _selectedThemeName = value;
            OnPropertyChanged();

            var theme = FromThemeName(value);
            if (_settings.Current.Theme == theme)
            {
                return;
            }

            _settings.Current.Theme = theme;
            _settings.Save();
            EditorThemeManager.Apply(theme);
        }
    }

    /// <summary>
    /// Сканировать диагностику при запуске редактора.
    /// </summary>
    public bool RunStartupDiagnosticsScan
    {
        get => _runStartupDiagnosticsScan;
        set
        {
            if (_runStartupDiagnosticsScan == value)
            {
                return;
            }

            _runStartupDiagnosticsScan = value;
            _settings.Current.RunStartupDiagnosticsScan = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Минимальный размер пачки единиц перевода при загрузке модуля.
    /// </summary>
    public int InitialRowsPerFrame
    {
        get => _initialRowsPerFrame;
        set
        {
            if (_initialRowsPerFrame == value)
            {
                return;
            }

            _initialRowsPerFrame = value;
            _settings.Current.InitialRowsPerFrame = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Максимальный размер пачки единиц перевода при загрузке модуля.
    /// </summary>
    public int MaxRowsPerFrame
    {
        get => _maxRowsPerFrame;
        set
        {
            if (_maxRowsPerFrame == value)
            {
                return;
            }

            _maxRowsPerFrame = value;
            _settings.Current.MaxRowsPerFrame = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Выполняется ли сейчас ручное сканирование диагностики.
    /// </summary>
    public bool IsDiagnosticsScanning => _diagnostics.IsScanning;

    /// <summary>
    /// Открыть каталог LanguageFiles в проводнике.
    /// </summary>
    public ICommand OpenLanguageFilesFolderCommand =>
        _openLanguageFilesFolderCommand ??= new RelayCommand(OpenLanguageFilesFolder);

    /// <summary>
    /// Запустить сканирование диагностики сейчас.
    /// </summary>
    public ICommand RescanDiagnosticsCommand => _rescanDiagnosticsCommand ??= new RelayCommand(
        () => SafeExecute.ExecuteAsync(RescanDiagnosticsAsync),
        _ => !IsDiagnosticsScanning);

    private async Task RescanDiagnosticsAsync()
    {
        await _diagnostics.RunStartupScanAsync(_repository, _session.Languages);
    }

    private void OpenLanguageFilesFolder()
    {
        if (!Directory.Exists(LanguageFilesPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = LanguageFilesPath,
            UseShellExecute = true,
        });
    }

    private void OnDiagnosticsPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorDiagnosticsService.IsScanning))
        {
            OnPropertyChanged(nameof(IsDiagnosticsScanning));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private static string ToThemeName(EditorAppTheme theme) =>
        theme == EditorAppTheme.Dark ? EditorStrings.ThemeDark : EditorStrings.ThemeLight;

    private static EditorAppTheme FromThemeName(string name) =>
        string.Equals(name, EditorStrings.ThemeDark, StringComparison.Ordinal)
            ? EditorAppTheme.Dark
            : EditorAppTheme.Light;
}