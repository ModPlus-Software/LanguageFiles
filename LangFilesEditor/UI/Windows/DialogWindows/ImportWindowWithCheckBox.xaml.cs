namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Windows;
using System.Windows.Controls;
using Helpers;
using Utils;

/// <summary>
/// Диалог автоматического импорта строк перевода с привязкой к XML-тегам.
/// </summary>
public partial class ImportWindowWithCheckbox
{
    /// <summary>
    /// Создаёт окно автоимпорта с подсказкой по порядку языков.
    /// </summary>
    /// <param name="languages">Список кодов языков в порядке вставки строк.</param>
    public ImportWindowWithCheckbox(IReadOnlyList<string> languages)
    {
        InitializeComponent();
        TbNote.Text = EditorStrings.FormatImportWithTagsNote(LanguageUtils.FormatDisplayOrder(languages));
    }

    private void Accept_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void TbText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        BtAccept.IsEnabled = !string.IsNullOrEmpty(TbText.Text);
    }

    private void BtCancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}