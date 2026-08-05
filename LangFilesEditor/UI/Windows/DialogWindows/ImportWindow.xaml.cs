namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Helpers;
using Utils;

/// <summary>
/// Диалог ручного импорта строк перевода ниже выбранной записи.
/// </summary>
public partial class ImportWindow
{
    /// <summary>
    /// Создаёт окно импорта с подсказкой по порядку языков.
    /// </summary>
    /// <param name="languages">Список кодов языков в порядке вставки строк.</param>
    public ImportWindow(IReadOnlyList<string> languages)
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
        TbNote.Text = EditorStrings.FormatImportNote(LanguageUtils.FormatDisplayOrder(languages));
    }

    // Поле ввода многострочное, поэтому подтверждение — Ctrl+Enter (обычный Enter = перенос строки).
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control && BtAccept.IsEnabled)
        {
            DialogResult = true;
            e.Handled = true;
        }
    }

    private void Accept_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;

    private void TbText_OnTextChanged(object sender, TextChangedEventArgs e) =>
        BtAccept.IsEnabled = !string.IsNullOrEmpty(TbText.Text);

    private void BtCancel_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;
}