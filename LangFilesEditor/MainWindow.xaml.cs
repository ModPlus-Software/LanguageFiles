namespace LangFilesEditor;

using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using HandyControl.Data;
using HandyControl.Tools;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private HashSet<DataGrid> _buildedDataGrids;

    public MainWindow()
    {
        InitializeComponent();
        ConfigHelper.Instance.SetLang("en");

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
        var dataTemplate = (DataTemplate)dataGrid.Resources["ItemValueCellTemplate"];
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

    private void DataGrid_OnPreviewKeyDown(object sender, KeyEventArgs e) => Utils.SafeExecute(
        () =>
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.None && e.KeyboardDevice.Modifiers != ModifierKeys.Control)
            {
                return;
            }

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            var verticalChange = key == Key.Down ? 1 : key == Key.Up ? -1 : 0;
            var horizontalChange = key == Key.Right ? 1 : key == Key.Left ? -1 : 0;

            if (verticalChange == 0 && horizontalChange == 0)
            {
                return;
            }

            if (sender is DataGrid grid)
            {
                // Get the row and column indices of the cell you want to select
                int rowIndex = grid.Items.IndexOf(grid.CurrentCell.Item);
                int columnIndex = grid.CurrentCell.Column.DisplayIndex;  

                var textBox = GetCellTextbox(grid, rowIndex, columnIndex);
                if (textBox == null
                    || (!IsTextboxMovableVertical(textBox, verticalChange)
                        && !IsTextboxMovableHorizontal(textBox, horizontalChange)))
                {
                    return;
                }

                rowIndex += verticalChange;
                if (rowIndex >= grid.Items.Count)
                {
                    rowIndex = 0;
                }

                if (rowIndex < 0)
                {
                    rowIndex = grid.Items.Count - 1;
                }

                columnIndex += horizontalChange;
                if (columnIndex >= grid.Columns.Count)
                {
                    columnIndex = 0;
                }

                if (columnIndex < 0)
                {
                    columnIndex = grid.Columns.Count - 1;
                }

                var datainfo = new DataGridCellInfo(grid.Items[rowIndex], grid.Columns[columnIndex]);
                grid.SelectedItem = grid.Items[rowIndex];
                grid.CurrentCell = datainfo;

                var content = grid.Columns[columnIndex].GetCellContent(grid.CurrentItem);
                if (content == null) return;

                // Ищем TextBox внутри контента
                var newYextBox = GetVisualChild<TextBox>(content);
                if (newYextBox == null) return;

                // Устанавливаем фокус и каретку
                newYextBox.Focus();
                newYextBox.CaretIndex = horizontalChange > 0 || verticalChange > 0 ? 0 : newYextBox.Text.Length;

                e.Handled = true;
            }
        });

    private bool IsTextboxMovableVertical(TextBox textBox, int verticalChange)
    {
        if (verticalChange == 0)
        {
            return false;
        }

        var positiveVariant = textBox.GetLineIndexFromCharacterIndex(textBox.CaretIndex) == (textBox.LineCount - 1);
        var negativeVariant = textBox.GetLineIndexFromCharacterIndex(textBox.CaretIndex) == 0;
        return verticalChange >= 0 ? positiveVariant : negativeVariant;
    }

    private bool IsTextboxMovableHorizontal(TextBox textBox, int horizontalChange)
    {
        if (horizontalChange == 0)
        {
            return false;
        }

        var positiveVariant = textBox.CaretIndex == textBox.Text.Length;
        var negativeVariant = textBox.CaretIndex == 0;
        return horizontalChange >= 0 ? positiveVariant : negativeVariant;
    }

    private TextBox GetCellTextbox(DataGrid grid, int rowIndex, int columnIndex)
    {
        // Get the DataGridRow
        DataGridRow rowData = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(rowIndex);

        if (rowData == null)
        {
            return null;
        }

        // Get the DataGridCell
        DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowData);
        if (presenter == null)
        {
            return null;
        }

        DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);

        if (cell == null)
        {
            // The cell might not be materialized if it's outside the visible area
            grid.ScrollIntoView(grid.Items[rowIndex], grid.Columns[columnIndex]);
            cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
        }

        if (cell == null)
        {
            return null;
        }

        // Find the TextBox within the cell's visual tree
        var textBox = GetVisualChild<TextBox>(cell);
        return textBox;
    }

    private static T GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }
            else
            {
                T childOfChild = GetVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
        }

        return null;
    }

    private void ColorPicker_OnConfirmed(object sender, FunctionEventArgs<Color> e)
    {
        try
        {
            var value = e.Info;
            Properties.Settings.Default.AutoRowColor = System.Drawing.Color.FromArgb(value.R, value.G, value.B);
        }
        catch
        {
            // ignored
        }
    }
}