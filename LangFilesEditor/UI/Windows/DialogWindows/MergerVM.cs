namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Core.Abstractions;
using Services;
using Services.Loggers;
using Services.RepositoryServices;
using Utils;
using ModPlusAPI;
using ModPlusAPI.Mvvm;

/// <summary>
/// ViewModel окна слияния локализации с установленным ModPlus.
/// </summary>
public class MergerVM : ObservableObject
{
    private readonly IEditorCommands _host;
    private readonly IDialogService _dialogService;
    private readonly NotificationService _notifications;
    private string _localVersion;
    private string _mergeLog = string.Empty;
    private bool _isMerging;
    
    /// <summary>
    /// Создаёт VM merger.
    /// </summary>
    /// <param name="host">Команды редактора с сохранением изменений.</param>
    /// <param name="dialogService">Сервис диалогов для сообщений об ошибках.</param>
    /// <param name="notifications">Сервис уведомлений операций слияния (его вывод отображается в окне).</param>
    public MergerVM(IEditorCommands host, IDialogService dialogService, NotificationService notifications)
    {
        _host = host;
        _dialogService = dialogService;
        _notifications = notifications;
        _notifications.OnNotify += AppendToMergeLog;
        _localVersion = ReadCurrentLocalVersion();
    }
    
    /// <summary>
    /// Локальная версия локализации, редактируемая пользователем перед записью в Version.txt.
    /// </summary>
    public string LocalVersion
    {
        get => _localVersion;
        set
        {
            if (_localVersion == value)
            {
                return;
            }
            
            _localVersion = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Выполняется ли сейчас операция merge.
    /// </summary>
    public bool IsMerging
    {
        get => _isMerging;
        private set
        {
            if (_isMerging == value)
            {
                return;
            }
            
            _isMerging = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }
    
    /// <summary>
    /// Накопленный текст лога операций слияния для отображения в окне.
    /// </summary>
    public string MergeLog
    {
        get => _mergeLog;
        private set
        {
            if (_mergeLog == value)
            {
                return;
            }
            
            _mergeLog = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Записать локальную версию в Version.txt, если она больше удалённой.
    /// </summary>
    public ICommand SetLocalVersionCommand => new RelayCommand(() => SafeExecute.ExecuteAsync(async () =>
    {
        if (!Version.TryParse(LocalVersion, out var version))
        {
            _dialogService.ShowMessageWindow("Failed parse version!");
            return;
        }
        
        var versionService = new LocalizationVersionService();
        var remoteVersion = await versionService.GetRemoteVersion();
        if (version <= remoteVersion)
        {
            _dialogService.ShowMessageWindow("The local version is less than or equal to the remote version!");
            return;
        }
        
        versionService.SetLocalVersion(version);
        _notifications.Notify($"Set {version} as local version");
    }));
    
    /// <summary>
    /// Сохранить и выполнить merge в каталог ModPlus.
    /// </summary>
    public ICommand MergeCommand => new RelayCommand(
        () => SafeExecute.ExecuteAsync(RunMergeAsync),
        _ => !IsMerging);
    
    private async Task RunMergeAsync()
    {
        IsMerging = true;
        try
        {
            MergeLog = string.Empty;
            if (!_host.Save())
            {
                return;
            }
            
            await Task.Run(() => new LanguageRepositoryService().MergeWithWorkingDirectory(_notifications));
        }
        finally
        {
            IsMerging = false;
        }
    }
    
    private void AppendToMergeLog(IEnumerable<string> messages)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            return;
        }
        
        foreach (var message in messages)
        {
            if (string.IsNullOrEmpty(message))
            {
                continue;
            }
            
            var line = message;
            dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                MergeLog = string.IsNullOrEmpty(MergeLog) ? line : $"{MergeLog}{Environment.NewLine}{line}";
            });
        }
    }
    
    private static string ReadCurrentLocalVersion()
    {
        try
        {
            var versionFile = Path.Combine(DirectoryUtils.GetSolutionDirectory(), "LanguageFiles", "Version.txt");
            return File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}