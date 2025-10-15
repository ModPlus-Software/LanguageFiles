namespace LangFilesEditor.Windows;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Логика взаимодействия для ImportWindow.xaml
/// </summary>
public partial class ImportWindowWithCheckbox
{
    public ImportWindowWithCheckbox()
    {
        InitializeComponent();

        TbNote.Text = $"Вставьте текст, содержащий перевод фраз в порядке: {PrintLanguageOrder()}. Перевод для каждого языка должен быть на новой строке. Можно добавлять перевод сразу для нескольких новых строк. Пустые строки игнорируются";
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

    private string PrintLanguageOrder()
    {
        return string.Join(", ", MainContext.LanguageOrder.Select(l => CultureInfo.GetCultureInfo(l).DisplayName));
    }
}