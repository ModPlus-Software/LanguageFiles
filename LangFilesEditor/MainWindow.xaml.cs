﻿namespace LangFilesEditor;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

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
        DgItems.BeginEdit();
    }
}