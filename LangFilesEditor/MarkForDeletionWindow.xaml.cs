namespace LangFilesEditor;

using System.Windows;

/// <summary>
/// Логика взаимодействия для MarkForDeletionWindow.xaml
/// </summary>
public partial class MarkForDeletionWindow
{
    public MarkForDeletionWindow()
    {
        InitializeComponent();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Mark_OnClick(object sender, RoutedEventArgs e)
    {
        if (!Version.TryParse(TbVersion.Text, out _))
        {
            MessageBox.Show("Enter valid version!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        DialogResult = true;
    }
}