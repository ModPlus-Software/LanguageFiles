namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Windows;
using System.Windows.Controls;
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
        TbNote.Text = $"Вставьте текст (вместе с тегами), содержащий перевод фраз в порядке: {LanguageUtils.FormatDisplayOrder(languages)}." +
                      $" Перевод для каждого языка должен быть на новой строке." +
                      $" Можно добавлять перевод сразу для нескольких новых строк. Пустые строки игнорируются";
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