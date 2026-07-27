using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.MainWindow;

using System.Windows;
using System.Windows.Input;
using Core.Abstractions;
using Models;
using Services;
using Utils;
using ModPlusAPI;
using ModPlusAPI.Mvvm;

/// <summary>
/// ViewModel панели инструментов core-редактора (без команд расширений).
/// </summary>
public class ToolBarVM : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IEditorWorkspace _workspace;
    private readonly IEditorSession _session;
    private readonly IEditorCommands _commands;
    private readonly Action _requestCloseWithoutSave;
    
    /// <summary>
    /// Создаёт toolbar с core-командами и командами расширений.
    /// </summary>
    /// <param name="dialogService">Сервис диалогов для импорта и подтверждений.</param>
    /// <param name="workspace">Рабочая область с текущим выбором.</param>
    /// <param name="session">Сессия данных с языками, признаком занятости и командами сохранения.</param>
    /// <param name="requestCloseWithoutSave">Запрос закрытия окна без сохранения (обрабатывается shell).</param>
    /// <param name="extensionCommands">Команды, зарегистрированные расширениями.</param>
    public ToolBarVM(
        IDialogService dialogService,
        IEditorWorkspace workspace,
        IEditorSession session,
        Action requestCloseWithoutSave,
        IReadOnlyList<LangFilesEditorToolbarCommand> extensionCommands = null)
    {
        _dialogService = dialogService;
        _requestCloseWithoutSave = requestCloseWithoutSave;
        _workspace = workspace;
        _session = session;
        _commands = session;
        ExtensionCommands = extensionCommands;
        _workspace.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(IEditorWorkspace.SelectedModule)
                or nameof(IEditorWorkspace.SelectedTranslationEntry)
                or nameof(IEditorWorkspace.SelectedDomain))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        };
        _session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(IEditorSession.IsOperationInProgress))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        };
    }
    
    /// <summary>
    /// Команды, зарегистрированные расширениями.
    /// </summary>
    public IReadOnlyList<LangFilesEditorToolbarCommand> ExtensionCommands { get; }
    
    private bool HasSelectedModule => _workspace.SelectedModule != null;
    
    private bool HasSelectedEntry => _workspace.SelectedTranslationEntry != null;
    
    /// <summary>
    /// Добавить строку выше выбранной.
    /// </summary>
    public ICommand AddRowAboveCommand => new RelayCommand(
        () => SafeExecute.Execute(() =>
        {
            var selectedModule = _workspace.SelectedModule;
            var index = selectedModule.Items.IndexOf(_workspace.SelectedTranslationEntry);
            selectedModule.InsertTranslationEntry(
                index,
                new TranslationEntryService(_session.Languages).GetTranslationEntry(string.Empty),
                TranslationEntryAddSource.User);
        }),
        _ => HasSelectedModule && HasSelectedEntry);
    
    /// <summary>
    /// Добавить строку ниже выбранной.
    /// </summary>
    public ICommand AddRowBelowCommand => new RelayCommand(
        () => SafeExecute.Execute(() =>
        {
            var selectedModule = _workspace.SelectedModule;
            var selectedEntry = _workspace.SelectedTranslationEntry;
            var index = selectedModule.Items.IndexOf(selectedEntry);
            if (index < 0)
            {
                return;
            }
            
            // Имя — следующий суффикс от выделенной строки (один шаг), без «перепрыгивания»
            // через уже занятые имена дальше по списку.
            var entryService = new TranslationEntryService(_session.Languages);
            var newName = entryService.GetNewTranslationEntryName(selectedEntry.Name);
            if (SearchEngine.ContainsItemByName(selectedModule.Items, newName))
            {
                newName = string.Empty;
            }
            
            var newEntry = entryService.GetTranslationEntry(newName);
            var insertIndex = index + 1;
            if (insertIndex >= selectedModule.Items.Count)
            {
                selectedModule.AddTranslationEntry(newEntry, TranslationEntryAddSource.User);
            }
            else
            {
                selectedModule.InsertTranslationEntry(
                    insertIndex,
                    newEntry,
                    TranslationEntryAddSource.User);
            }
        }), _ => HasSelectedModule && HasSelectedEntry);
    
    /// <summary>
    /// Импорт строк ниже выбранной.
    /// </summary>
    public ICommand ImportRowsBelowCommand => new RelayCommand(
        () => SafeExecute.Execute(() => _dialogService.ShowImportWindow()), _ => HasSelectedModule);
    
    /// <summary>
    /// Автоимпорт строк.
    /// </summary>
    public ICommand ImportRowsAutoCommand => new RelayCommand(
        () => SafeExecute.Execute(() => _dialogService.ShowImportWindowWithCheckbox()),
        _ => HasSelectedModule);
    
    /// <summary>
    /// Пометить строку на удаление в будущей версии.
    /// </summary>
    public ICommand MarkForDeletionCommand => new RelayCommand<string>(
        version => SafeExecute.Execute(() => MarkForDeletion(version)), _ => HasSelectedEntry);
    
    /// <summary>
    /// Копировать строку в буфер обмена.
    /// </summary>
    public ICommand CopyToClipboardCommand => new RelayCommand(() => SafeExecute.Execute(() =>
    {
        var item = _workspace.SelectedTranslationEntry;
        var data = $"LANG_{item.Name}|" +
                   $"{string.Join("|", item.Values.Select(v => $"{v.Key}${v.Value.Value}"))}";
        ClipboardUtils.CopyToClipboard(data);
    }), _ => HasSelectedEntry);
    
    /// <summary>
    /// Вставить строку из буфера обмена.
    /// </summary>
    public ICommand PasteFromClipboard => new RelayCommand(() =>
    {
        var selectedModule = _workspace.SelectedModule;
        var entryService = new TranslationEntryService(_session.Languages);
        var targetTranslationEntry = entryService
            .GetNewTranslationEntry(
                selectedModule.Items.LastOrDefault(),
                valuesInOrder: null,
                existingEntries: selectedModule.Items);
        var data = ClipboardUtils.GetFromClipboard().Replace("LANG_", string.Empty).Split('|');
        targetTranslationEntry.Name = data[0];
        foreach (var s in data.Skip(1))
        {
            var value = s.Split('$');
            targetTranslationEntry.Values[value[0]].Value = value[1];
        }
            
        selectedModule.AddTranslationEntry(targetTranslationEntry, TranslationEntryAddSource.User);
    }, _ => HasSelectedModule
            && Clipboard.ContainsText()
            && ClipboardUtils.GetFromClipboard().StartsWith("LANG_"));
    
    /// <summary>
    /// Удалить выбранную строку.
    /// </summary>
    public ICommand RemoveItemCommand => new RelayCommand(() => SafeExecute.Execute(() =>
    {
        var selectedTranslationEntry = _workspace.SelectedTranslationEntry;
        if (!string.IsNullOrEmpty(selectedTranslationEntry.Comment))
        {
            _dialogService.ShowMessageWindow("Позиции, отмеченные комментарием, удалять нельзя!");
            return;
        }
            
        if (string.IsNullOrEmpty(selectedTranslationEntry.RemovesOnVersion))
        {
            var question = "Нельзя удалять строки из локализации, если плагин уже в релизе!" +
                           " Такие строки следует отмечать комментарием с todo." +
                           "\nТочно удалить?";
            if (!_dialogService.ShowQuestionWindow(question))
            {
                return;
            }
        }
            
        var selectedModule = _workspace.SelectedModule;
        _commands.TrackItemForRemoval(selectedModule, selectedTranslationEntry);
        selectedModule.RemoveTranslationEntry(selectedTranslationEntry);
    }), _ => HasSelectedEntry);
    
    /// <summary>
    /// Открыть окно настроек.
    /// </summary>
    public ICommand OpenSettingsCommand => new RelayCommand(
        () => SafeExecute.Execute(() => _dialogService.ShowSettingsWindow()), _ => true);
    
    /// <summary>
    /// Открыть окно merge в каталог ModPlus.
    /// </summary>
    public ICommand OpenMergerCommand => new RelayCommand(
        () => SafeExecute.Execute(() => _dialogService.ShowMergerWindow()), _ => true);
    
    /// <summary>
    /// Закрыть приложение без сохранения.
    /// </summary>
    public ICommand CloseWithoutSaveCommand => new RelayCommand(
        () => _requestCloseWithoutSave(), _ => true);
    
    private void MarkForDeletion(string version)
    {
        if (string.IsNullOrEmpty(_workspace.SelectedTranslationEntry.RemovesOnVersion))
        {
            _workspace.SelectedTranslationEntry.RemovesOnVersion = version;
        }
        
        _dialogService.ShowMarkForDeletionWindow();
    }
}