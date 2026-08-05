using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.MainWindow;

using System.Windows;
using System.Windows.Input;
using Core.Abstractions;
using Models;
using Helpers;
using Services;
using Utils;
using ModPlusAPI;
using ModPlusAPI.Mvvm;

/// <summary>
/// ViewModel панели инструментов core-редактора (без команд расширений).
/// </summary>
public class ToolBarVM : ObservableObject
{
    // Префикс собственного формата буфера обмена: по нему строка отличается от произвольного текста.
    private const string ClipboardEntryPrefix = "LANG_";

    private readonly IDialogService _dialogService;
    private readonly IEditorWorkspace _workspace;
    private readonly IEditorSession _session;
    private readonly IEditorCommands _commands;
    private readonly Action _requestCloseWithoutSave;
    private readonly TranslationEntryService _entryService;
    private ICommand _addRowAboveCommand;
    private ICommand _addRowBelowCommand;
    private ICommand _importRowsBelowCommand;
    private ICommand _importRowsAutoCommand;
    private ICommand _markForDeletionCommand;
    private ICommand _copyToClipboardCommand;
    private ICommand _pasteFromClipboardCommand;
    private ICommand _removeItemCommand;
    private ICommand _openSettingsCommand;
    private ICommand _openMergerCommand;
    private ICommand _closeWithoutSaveCommand;

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
        _entryService = new TranslationEntryService(session.Languages);
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
    public ICommand AddRowAboveCommand => _addRowAboveCommand ??= new RelayCommand(
        () => SafeExecute.Execute(() =>
        {
            var selectedModule = _workspace.SelectedModule;
            var index = selectedModule.Items.IndexOf(_workspace.SelectedTranslationEntry);
            if (index < 0)
            {
                return;
            }

            selectedModule.InsertTranslationEntry(
                index,
                _entryService.GetTranslationEntry(string.Empty),
                TranslationEntryAddSource.User);
        }),
        _ => HasSelectedModule && HasSelectedEntry);

    /// <summary>
    /// Добавить строку ниже выбранной.
    /// </summary>
    public ICommand AddRowBelowCommand => _addRowBelowCommand ??= new RelayCommand(
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
            var newName = _entryService.GetNewTranslationEntryName(selectedEntry.Name);
            if (SearchEngine.ContainsItemByName(selectedModule.Items, newName))
            {
                newName = string.Empty;
            }

            var newEntry = _entryService.GetTranslationEntry(newName);
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
    public ICommand ImportRowsBelowCommand => _importRowsBelowCommand ??= new RelayCommand(
        () => SafeExecute.Execute(() => _dialogService.ShowImportWindow()), _ => HasSelectedModule);

    /// <summary>
    /// Автоимпорт строк.
    /// </summary>
    public ICommand ImportRowsAutoCommand => _importRowsAutoCommand ??= new RelayCommand(
        () => SafeExecute.Execute(() => _dialogService.ShowImportWindowWithCheckbox()),
        _ => HasSelectedModule);

    /// <summary>
    /// Пометить строку на удаление в будущей версии.
    /// </summary>
    public ICommand MarkForDeletionCommand => _markForDeletionCommand ??= new RelayCommand<string>(
        version => SafeExecute.Execute(() => MarkForDeletion(version)), _ => HasSelectedEntry);

    /// <summary>
    /// Копировать строку в буфер обмена.
    /// </summary>
    public ICommand CopyToClipboardCommand => _copyToClipboardCommand ??= new RelayCommand(
        () => SafeExecute.Execute(() =>
        {
            var item = _workspace.SelectedTranslationEntry;
            var data = $"{ClipboardEntryPrefix}{item.Name}|" +
                       $"{string.Join("|", item.Values.Select(v => $"{v.Key}${v.Value.Value}"))}";
            ClipboardUtils.CopyToClipboard(data);
        }),
        _ => HasSelectedEntry);

    /// <summary>
    /// Вставить строку из буфера обмена.
    /// </summary>
    public ICommand PasteFromClipboard => _pasteFromClipboardCommand ??= new RelayCommand(
        () => SafeExecute.Execute(PasteEntryFromClipboard),
        _ => HasSelectedModule && Clipboard.ContainsText());

    /// <summary>
    /// Удалить выбранную строку.
    /// </summary>
    public ICommand RemoveItemCommand => _removeItemCommand ??= new RelayCommand(() => SafeExecute.Execute(() =>
    {
        var selectedTranslationEntry = _workspace.SelectedTranslationEntry;
        if (!string.IsNullOrEmpty(selectedTranslationEntry.Comment))
        {
            _dialogService.ShowMessageWindow(EditorStrings.RemoveCommentedEntryForbidden);
            return;
        }

        if (string.IsNullOrEmpty(selectedTranslationEntry.RemovesOnVersion))
        {
            if (!_dialogService.ShowQuestionWindow(EditorStrings.RemoveEntryQuestion))
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
    public ICommand OpenSettingsCommand => _openSettingsCommand ??= new RelayCommand(
        () => SafeExecute.Execute(() => _dialogService.ShowSettingsWindow()), _ => true);

    /// <summary>
    /// Открыть окно merge в каталог ModPlus.
    /// </summary>
    public ICommand OpenMergerCommand => _openMergerCommand ??= new RelayCommand(
        () => SafeExecute.Execute(() => _dialogService.ShowMergerWindow()), _ => true);

    /// <summary>
    /// Закрыть приложение без сохранения.
    /// </summary>
    public ICommand CloseWithoutSaveCommand => _closeWithoutSaveCommand ??= new RelayCommand(
        () => _requestCloseWithoutSave(), _ => true);

    /// <summary>
    /// Вставляет строку перевода из буфера обмена. Формат — тот же, что пишет
    /// <see cref="CopyToClipboardCommand"/>: <c>LANG_имя|язык$значение|язык$значение</c>.
    /// Чужой или повреждённый текст игнорируется, а не роняет редактор.
    /// </summary>
    private void PasteEntryFromClipboard()
    {
        // Содержимое буфера проверяется здесь, а не в CanExecute: CanExecute переспрашивается
        // на каждый ввод и фокус, а чтение буфера обмена — обращение к системному ресурсу.
        var clipboardText = ClipboardUtils.GetFromClipboard();
        var parts = clipboardText.StartsWith(ClipboardEntryPrefix, StringComparison.Ordinal)
            ? clipboardText[ClipboardEntryPrefix.Length..].Split('|')
            : Array.Empty<string>();

        if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
        {
            _dialogService.ShowMessageWindow(EditorStrings.ClipboardHasNoEntry);
            return;
        }

        var selectedModule = _workspace.SelectedModule;
        var targetTranslationEntry = _entryService.GetNewTranslationEntry(
            selectedModule.Items.LastOrDefault(),
            valuesInOrder: null,
            existingEntries: selectedModule.Items);
        targetTranslationEntry.Name = parts[0];

        foreach (var part in parts.Skip(1))
        {
            var separatorIndex = part.IndexOf('$');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var languageName = part[..separatorIndex];
            if (targetTranslationEntry.Values.TryGetValue(languageName, out var itemValue))
            {
                itemValue.Value = part[(separatorIndex + 1)..];
            }
        }

        selectedModule.AddTranslationEntry(targetTranslationEntry, TranslationEntryAddSource.User);
    }

    private void MarkForDeletion(string version)
    {
        if (string.IsNullOrEmpty(_workspace.SelectedTranslationEntry.RemovesOnVersion))
        {
            _workspace.SelectedTranslationEntry.RemovesOnVersion = version;
        }

        _dialogService.ShowMarkForDeletionWindow();
    }
}