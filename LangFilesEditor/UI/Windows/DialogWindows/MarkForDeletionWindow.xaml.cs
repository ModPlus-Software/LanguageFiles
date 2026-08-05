using LangFilesEditor.Services;

namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Windows;
using Core.Abstractions;
using Helpers;
using Utils;

/// <summary>
/// Окно пометки строки на удаление в будущей версии.
/// </summary>
public partial class MarkForDeletionWindow
{
    private readonly IEditorWorkspace _host;

    /// <summary>
    /// Создаёт окно с host API.
    /// </summary>
    /// <param name="host">Рабочая область с выбранной записью перевода.</param>
    public MarkForDeletionWindow(IEditorWorkspace host)
    {
        _host = host;
        InitializeComponent();
        ShowCurrentState();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Mark_OnClick(object sender, RoutedEventArgs e)
    {
        var entry = _host.SelectedTranslationEntry;
        if (entry == null)
        {
            DialogResult = false;
            return;
        }

        if (!Version.TryParse(TbVersion.Text, out var version))
        {
            MessageBox.Show(
                EditorStrings.EnterValidVersion,
                EditorStrings.ErrorCaption,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        entry.RemovesOnVersion = version.ToString();
        entry.Comment = DeletionMarker.Build(version.ToString());
        DialogResult = true;
    }

    private void Unmark_OnClick(object sender, RoutedEventArgs e)
    {
        var entry = _host.SelectedTranslationEntry;
        if (entry == null)
        {
            DialogResult = false;
            return;
        }

        // Чужой комментарий не трогаем: снимается только пометка к удалению.
        // Кнопка и так недоступна в остальных случаях, но проверка страхует от
        // потери комментария, если состояние окна разойдётся с записью.
        if (DeletionMarker.IsMarked(entry.Comment))
        {
            entry.Comment = null;
        }

        entry.RemovesOnVersion = null;
        DialogResult = true;
    }

    /// <summary>
    /// Показывает текущее состояние выбранной записи и подставляет версию в поле ввода.
    /// </summary>
    private void ShowCurrentState()
    {
        var entry = _host.SelectedTranslationEntry;
        if (entry == null)
        {
            TbState.Text = EditorStrings.EntryNotMarkedForDeletion;
            BtUnmark.IsEnabled = false;
            return;
        }

        // Снимать можно только то, что реально записано в файл, поэтому состояние окна
        // определяет комментарий. RemovesOnVersion живёт лишь в памяти текущей сессии
        // и годится только как подсказка для поля ввода.
        var isMarked = DeletionMarker.TryGetVersion(entry.Comment, out var version);

        TbVersion.Text = isMarked ? version : entry.RemovesOnVersion ?? string.Empty;
        BtUnmark.IsEnabled = isMarked;

        if (isMarked)
        {
            TbState.Text = EditorStrings.FormatEntryMarkedForDeletion(version);
        }
        else if (!string.IsNullOrWhiteSpace(entry.Comment))
        {
            // Комментарий есть, но это не пометка к удалению — показываем его: иначе
            // непонятно, почему строка подкрашена и почему её нельзя удалить.
            TbState.Text = EditorStrings.FormatEntryComment(entry.Comment);
        }
        else
        {
            TbState.Text = EditorStrings.EntryNotMarkedForDeletion;
        }
    }
}
