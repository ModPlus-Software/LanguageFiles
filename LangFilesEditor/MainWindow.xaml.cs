namespace LangFilesEditor;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        Closing += OnClosing;
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        if (DataContext is MainContext mainContext &&
            mainContext.Nodes.Any(n => n.HasIncorrectData) && 
            !mainContext.CloseWithoutSave)
        {
            MessageBox.Show("There is Nodes with errors! Fix them or close without saving");
            e.Cancel = true;
        }
    }

    private void DgItems_OnSelected(object sender, RoutedEventArgs e)
    {
        // Lookup for the source to be DataGridCell
        if (e.OriginalSource.GetType() == typeof(DataGridCell))
        {
            // Starts the Edit on the row;
            DataGrid grd = (DataGrid)sender;
            grd.BeginEdit(e);
        }
    }

    private void DgItems_OnPreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
    {
        // Попытка найти TextBox внутри ячейки
        var cellContent = e.EditingElement;
        if (cellContent != null)
        {
            // Рекурсивно ищем TextBox внутри шаблона
            var textBox = FindVisualChild<TextBox>(cellContent);
            if (textBox != null)
            {
                textBox.Focus(); // Устанавливаем фокус
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }

    private void DgItems_OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Column is DataGridTemplateColumn)
        {
            // Передайте управление стандартному поведению для активации редактирования
            e.Cancel = false;
        }
    }

    private void DgItems_OnCurrentCellChanged(object sender, EventArgs e)
    {
        DgItems.BeginEdit();
    }

    // Универсальный метод для поиска дочернего элемента в визуальном дереве
    private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T t)
            {
                return t;
            }
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        return null;
    }
}