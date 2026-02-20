namespace LangFilesEditor;

using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private HashSet<DataGrid> _buildedDataGrids;

    public MainWindow()
    {
        InitializeComponent();

        _buildedDataGrids = [];

        Closing += OnClosing;
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        if (DataContext is MainContext { IsSaveNotPossible: true })
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
            var grd = (DataGrid)sender;
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
            var textBox = Utils.FindVisualChild<TextBox>(cellContent);
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
        ((DataGrid)sender).BeginEdit();
    }

    private void DgItems_OnLoaded(object sender, RoutedEventArgs e)
    {
        BuildColumns((DataGrid)sender);
    }

    private void DgAttributes_OnLoaded(object sender, RoutedEventArgs e)
    {
        BuildColumns((DataGrid)sender);
    }

    private void BuildColumns(DataGrid dataGrid)
    {
        if (!_buildedDataGrids.Add(dataGrid))
            return;
        foreach (var languageName in MainContext.LanguageOrder)
        {
            dataGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = GetColumnHeader(languageName),
                CellTemplate = GetDataTemplateForStringCell(dataGrid, $"Values[{languageName}].Value"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }
    }

    private DataTemplate GetDataTemplateForStringCell(DataGrid dataGrid, string bindingPath)
    {
        var dataTemplate = (DataTemplate)GetParentGrid(dataGrid).Resources["ItemValueCellTemplate"];
        var xaml = XamlWriter.Save(dataTemplate!);
        xaml = xaml.Replace(
            "{DynamicResource PLACEHOLDER}",
            "{Binding Path=" + bindingPath + ", Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}");

        var stringReader = new StringReader(xaml);

        var xmlReader = XmlReader.Create(stringReader);

        return (DataTemplate)XamlReader.Load(xmlReader);
    }

    private static string GetColumnHeader(string languageName)
    {
        return $"{new CultureInfo(languageName).DisplayName}\n{languageName}";
    }

    private static Grid GetParentGrid(DependencyObject child)
    {
        var parentObject = ((FrameworkElement) child).Parent;

        if (parentObject == null)
            return null;

        if (parentObject is Grid parent && parent.Name == "GridNodeContent")
            return parent;
        
        return GetParentGrid(parentObject);
    }
}