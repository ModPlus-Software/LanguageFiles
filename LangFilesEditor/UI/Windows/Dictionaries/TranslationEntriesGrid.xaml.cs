namespace LangFilesEditor.UI.Windows.Dictionaries;

using MainWindow.WorkSpace;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Utils;

/// <summary>
/// Универсальный грид записей перевода с поддержкой режима workspace и атрибутов.
/// </summary>
public partial class TranslationEntriesGrid
{
    private ScrollViewer _scrollViewer;

    /// <summary>
    /// Свойство зависимости для источника данных строк грида.
    /// </summary>
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(TranslationEntriesGrid));

    /// <summary>
    /// Свойство зависимости для заголовка над гридом.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(TranslationEntriesGrid),
            new PropertyMetadata(null, OnHeaderChanged));

    /// <summary>
    /// Свойство зависимости, определяющее редактируемость колонки имени.
    /// </summary>
    public static readonly DependencyProperty IsNameColumnEditableProperty =
        DependencyProperty.Register(
            nameof(IsNameColumnEditable),
            typeof(bool),
            typeof(TranslationEntriesGrid),
            new PropertyMetadata(true, OnLayoutPropertyChanged));

    /// <summary>
    /// Свойство зависимости, переключающее макет workspace и простого списка.
    /// </summary>
    public static readonly DependencyProperty UseWorkspaceLayoutProperty =
        DependencyProperty.Register(
            nameof(UseWorkspaceLayout),
            typeof(bool),
            typeof(TranslationEntriesGrid),
            new PropertyMetadata(false, OnLayoutPropertyChanged));

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationEntriesGrid"/> class.
    /// Создаёт элемент управления грида записей перевода.
    /// </summary>
    public TranslationEntriesGrid()
    {
        InitializeComponent();
        UpdateHeaderVisibility();
        ApplyLayoutMode();
        Loaded += OnLoaded;
    }

    /// <summary>
    /// Возникает при смене выбранной строки в гриде.
    /// </summary>
    public event EventHandler<object> SelectedRowChanged;

    /// <summary>
    /// Коллекция строк для отображения в гриде.
    /// </summary>
    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Заголовок, отображаемый над гридом; пустая строка скрывает заголовок.
    /// </summary>
    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Разрешено ли редактирование колонки имени записи.
    /// </summary>
    public bool IsNameColumnEditable
    {
        get => (bool)GetValue(IsNameColumnEditableProperty);
        set => SetValue(IsNameColumnEditableProperty, value);
    }

    /// <summary>
    /// <see langword="true"/> — строки <see cref="WorkspaceGridRow"/> (модули в одной таблице);
    /// <see langword="false"/> — строки <see cref="Models.TranslationEntry"/> (атрибуты и простые списки).
    /// </summary>
    public bool UseWorkspaceLayout
    {
        get => (bool)GetValue(UseWorkspaceLayoutProperty);
        set => SetValue(UseWorkspaceLayoutProperty, value);
    }
    
    /// <summary>
    /// Прокручивает строку к верху видимой области; у конца списка — до максимума.
    /// </summary>
    /// <param name="rowItem">Элемент строки для прокрутки.</param>
    public void ScrollRowToTop(object rowItem)
    {
        if (rowItem == null)
        {
            return;
        }
        
        _scrollViewer ??= WpfUtils.FindVisualChild<ScrollViewer>(EntriesDataGrid);
        if (_scrollViewer == null)
        {
            return;
        }
        
        EntriesDataGrid.ScrollIntoView(rowItem);
        EntriesDataGrid.UpdateLayout();
        if (EntriesDataGrid.ItemContainerGenerator.ContainerFromItem(rowItem) is not DataGridRow dataGridRow)
        {
            return;
        }
        
        var transform = dataGridRow.TransformToVisual(_scrollViewer);
        var rowTop = transform.Transform(new Point(0, 0)).Y;
        var targetOffset = _scrollViewer.VerticalOffset + rowTop;
        var maxOffset = Math.Max(0, _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight);
        if (targetOffset > maxOffset)
        {
            targetOffset = maxOffset;
        }
        
        var spaceBelow = _scrollViewer.ExtentHeight - targetOffset - _scrollViewer.ViewportHeight;
        _scrollViewer.ScrollToVerticalOffset(spaceBelow > 1 ? targetOffset : maxOffset);
    }
    
    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TranslationEntriesGrid grid)
        {
            grid.ApplyLayoutMode();
        }
    }
    
    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TranslationEntriesGrid grid)
        {
            grid.UpdateHeaderVisibility();
        }
    }
    
    private void ApplyLayoutMode()
    {
        if (UseWorkspaceLayout)
        {
            EntriesDataGrid.EnableRowVirtualization = false;
            EntriesDataGrid.RowStyle = (Style)FindResource("TranslationEntriesDataGridRowStyle");
            NameColumn.CellTemplate = (DataTemplate)FindResource(
                IsNameColumnEditable ? "WorkspaceNameCellTemplate" : "WorkspaceNameReadOnlyCellTemplate");
            RuColumn.CellTemplate = (DataTemplate)FindResource("WorkspaceRuRuCellTemplate");
            UkColumn.CellTemplate = (DataTemplate)FindResource("WorkspaceUkUaCellTemplate");
            EnColumn.CellTemplate = (DataTemplate)FindResource("WorkspaceEnUsCellTemplate");
            DeColumn.CellTemplate = (DataTemplate)FindResource("WorkspaceDeDeCellTemplate");
            EsColumn.CellTemplate = (DataTemplate)FindResource("WorkspaceEsEsCellTemplate");
            ZhColumn.CellTemplate = (DataTemplate)FindResource("WorkspaceZhCnCellTemplate");
            return;
        }
        
        EntriesDataGrid.EnableRowVirtualization = true;
        EntriesDataGrid.RowStyle = (Style)FindResource("TranslationEntriesEntryRowStyle");
        NameColumn.CellTemplate = (DataTemplate)FindResource(
            IsNameColumnEditable ? "ItemNameCellTemplate" : "ItemNameReadOnlyCellTemplate");
        RuColumn.CellTemplate = (DataTemplate)FindResource("EntryRuRuCellTemplate");
        UkColumn.CellTemplate = (DataTemplate)FindResource("EntryUkUaCellTemplate");
        EnColumn.CellTemplate = (DataTemplate)FindResource("EntryEnUsCellTemplate");
        DeColumn.CellTemplate = (DataTemplate)FindResource("EntryDeDeCellTemplate");
        EsColumn.CellTemplate = (DataTemplate)FindResource("EntryEsEsCellTemplate");
        ZhColumn.CellTemplate = (DataTemplate)FindResource("EntryZhCnCellTemplate");
    }
    
    private void UpdateHeaderVisibility()
    {
        var hasHeader = !string.IsNullOrEmpty(Header);
        HeaderRow.Height = hasHeader ? GridLength.Auto : new GridLength(0);
        HeaderTextBlock.Visibility = hasHeader ? Visibility.Visible : Visibility.Collapsed;
    }
    
    private void EntriesDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedRowChanged?.Invoke(this, EntriesDataGrid.SelectedItem);
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EntriesDataGrid.PreviewMouseWheel -= OnPreviewMouseWheel;
        EntriesDataGrid.PreviewMouseWheel += OnPreviewMouseWheel;
    }
    
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _scrollViewer ??= WpfUtils.FindVisualChild<ScrollViewer>(EntriesDataGrid);
        if (_scrollViewer == null)
        {
            return;
        }
        
        var maxOffset = Math.Max(0, _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight);
        var newOffset = _scrollViewer.VerticalOffset - e.Delta;
        if (newOffset < 0)
        {
            newOffset = 0;
        }
        else if (newOffset > maxOffset)
        {
            newOffset = maxOffset;
        }
        
        _scrollViewer.ScrollToVerticalOffset(newOffset);
        e.Handled = true;
    }
}