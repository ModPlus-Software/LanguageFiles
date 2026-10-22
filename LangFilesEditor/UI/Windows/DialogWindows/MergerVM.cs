namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Core.Abstractions;
using Services.Loggers;
using ModPlusAPI;
using ModPlusAPI.Mvvm;

/// <summary>
/// ViewModel окна слияния локализации с установленным ModPlus.
/// </summary>
/// <remarks>
/// Версия в редакторе не задаётся: в собранные файлы пишется версия, опубликованная в хранилище.
/// </remarks>
public class MergerVM : ObservableObject
{
    private readonly IEditorCommands _host;
    private readonly ILanguageRepository _repository;
    private readonly NotificationService _notifications;
    private ICommand _mergeCommand;
    private string _mergeLog = string.Empty;
    private bool _isMerging;

    /// <summary>
    /// Создаёт VM merger.
    /// </summary>
    /// <param name="host">Команды редактора с сохранением изменений.</param>
    /// <param name="repository">Репозиторий языковых файлов, выполняющий слияние.</param>
    /// <param name="notifications">Сервис уведомлений операций слияния (его вывод отображается в окне).</param>
    public MergerVM(
        IEditorCommands host,
        ILanguageRepository repository,
        NotificationService notifications)
    {
        _host = host;
        _repository = repository;
        _notifications = notifications;
        _notifications.OnNotify += AppendToMergeLog;
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
    /// Сохранить и выполнить merge в каталог ModPlus.
    /// </summary>
    public ICommand MergeCommand => _mergeCommand ??= new RelayCommand(
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

            await Task.Run(() => _repository.MergeWithWorkingDirectory(_notifications));
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
}