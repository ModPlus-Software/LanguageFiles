namespace LangFilesEditor;

using JetBrains.Annotations;
using Structure;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using Windows;

internal partial class MainContext(MainWindow mainWindow) : ObservableObject
{
    private readonly Dictionary<string, List<string>> _itemsToRemove = [];
    private Node _selectedNode;

    /// <summary>
    /// Порядок языков
    /// </summary>
    public static List<string> LanguageOrder = ["ru-RU", "uk-UA", "en-US", "de-DE", "es-ES", "zh-CN"];

    /// <summary>
    /// Nodes
    /// </summary>
    public ObservableCollection<Node> Nodes { get; } = [];

    /// <summary>
    /// Edit nodes
    /// </summary>
    public ObservableCollection<Node> EditNodes { get; } = [];

    /// <summary>
    /// Selected node
    /// </summary>
    public Node SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (_selectedNode == value)
                return;
            _selectedNode = value;
            value?.Validate();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsVisibleSelectedNodeContent));

            if (value != null)
            {
                if (!EditNodes.Contains(value))
                    EditNodes.Add(value);
            }
        }
    }

    /// <summary>
    /// Is visible <see cref="SelectedNode"/> content
    /// </summary>
    public bool IsVisibleSelectedNodeContent => SelectedNode != null;

    /// <summary>
    /// Close without saving
    /// </summary>
    public bool CloseWithoutSave { get; private set; }

    /// <summary>
    /// Есть ли ошибки, не позволяющие вызвать сохранение.
    /// </summary>
    public bool IsSaveNotPossible => !CloseWithoutSave && Nodes.Any(n => n.HasIncorrectData);

    /// <summary>
    /// Close without save
    /// </summary>
    public ICommand CloseWithoutSaveCommand => new RelayCommand(() =>
    {
        CloseWithoutSave = true;
        mainWindow.Close();
    });

    /// <summary>
    /// Close active editor tab
    /// </summary>
    public ICommand CloseEditorTabCommand => new RelayCommand(() => Utils.SafeExecute(() =>
    {
        EditNodes.Remove(SelectedNode);
    }));

    /// <summary>
    /// Add row above
    /// </summary>
    public ICommand AddRowAboveCommand => new RelayCommand(() => Utils.SafeExecute(() =>
    {
        var dataGrid = GetSelectedTabDataGrid();
        if (dataGrid.SelectedItem != null)
        {
            var index = dataGrid.Items.IndexOf(dataGrid.SelectedItem);
            SelectedNode.Items.Insert(index, GetNewItem(string.Empty));
        }
        else
        {
            SelectedNode.Items.Add(GetNewItem(GetNewItemName(SelectedNode.Items.LastOrDefault())));
        }
    }), _ => SelectedNode != null);

    /// <summary>
    /// Add row below
    /// </summary>
    public ICommand AddRowBelowCommand => new RelayCommand(() => Utils.SafeExecute(() =>
    {
        var dataGrid = GetSelectedTabDataGrid();
        if (dataGrid.SelectedItem != null)
        {
            var index = dataGrid.Items.IndexOf(dataGrid.SelectedItem) + 1;
            if (index == SelectedNode.Items.Count)
                SelectedNode.Items.Add(GetNewItem(GetNewItemName(SelectedNode.Items.LastOrDefault())));
            else
                SelectedNode.Items.Insert(index, GetNewItem(GetNewItemName(SelectedNode.Items[index - 1])));
        }
        else
        {
            SelectedNode.Items.Add(GetNewItem(GetNewItemName(SelectedNode.Items.LastOrDefault())));
        }
    }), _ => SelectedNode != null);

    /// <summary>
    /// Import rows below
    /// </summary>
    public ICommand ImportRowsBelowCommand => new RelayCommand(() => Utils.SafeExecute(() =>
    {
        var win = new ImportWindow()
        {
            Owner = mainWindow
        };
        if (win.ShowDialog() != true)
            return;

        var resultRows = new List<List<string>>();

        var rows = win.TbText.Text.Split(["\r\n", "\n"], StringSplitOptions.TrimEntries);

        var index = 0;
        var resultRow = new List<string>();
        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row))
                continue;

            if (index == LanguageOrder.Count)
                index = 0;

            if (index == 0)
            {
                if (resultRow.Count == LanguageOrder.Count)
                {
                    resultRows.Add(resultRow);
                }

                resultRow = [];
            }

            resultRow.Add(row);

            index++;
        }

        if (resultRow.Count == LanguageOrder.Count)
        {
            resultRows.Add(resultRow);
        }

        if (resultRows.Count == 0)
            return;

        var dataGrid = GetSelectedTabDataGrid();

        if (dataGrid.SelectedItem != null)
        {
            index = dataGrid.Items.IndexOf(dataGrid.SelectedItem) + 1;
            if (index == SelectedNode.Items.Count)
            {
                var previousName = SelectedNode.Items.LastOrDefault()?.Name ?? string.Empty;
                foreach (var row in resultRows)
                {
                    previousName = GetNewItemName(previousName);
                    SelectedNode.Items.Add(GetNewItem(previousName, row));
                }
            }
            else
            {
                var previousName = SelectedNode.Items[index - 1].Name;
                foreach (var row in resultRows)
                {
                    previousName = GetNewItemName(previousName);
                    SelectedNode.Items.Insert(index, GetNewItem(previousName, row));
                    index++;
                }
            }
        }
        else
        {
            var previousName = SelectedNode.Items.LastOrDefault()?.Name ?? string.Empty;
            foreach (var row in resultRows)
            {
                previousName = GetNewItemName(previousName);
                SelectedNode.Items.Add(GetNewItem(previousName, row));
            }
        }
    }), _ => SelectedNode != null);

    /// <summary>
    /// Import rows below
    /// </summary>
    public ICommand ImportRowsAutoCommand => new RelayCommand(() => Utils.SafeExecute(() =>
    {
        var win = new ImportWindowWithCheckbox()
        {
            Owner = mainWindow
        };
        if (win.ShowDialog() != true)
            return;

        var sortedRows = GetRowsFromCopyPaste(win.TbText.Text);
        if (sortedRows.Count == 0)
            return;

        var dataGrid = GetSelectedTabDataGrid();

        foreach (var key in sortedRows.Keys)
        {
            Utils.GetTagValueAndNumber(key, out string value, out var rowNumber);
            var number = SearchLastRowWithTagValue(value, out int index);
            if (index == -1)
            {
                index = dataGrid.Items.Count - 1;
                number = 0;
            }

            string startName;
            if (win.CbAutoNumbering.IsChecked.HasValue && win.CbAutoNumbering.IsChecked.Value)
            {
                startName = $"{value}{number}";
                startName = GetNewItemName(startName);
            }
            else
            {
                startName = $"{value}{rowNumber}";
            }

            SelectedNode.Items.Insert(index + 1, GetNewItem(startName, sortedRows[key]));
        }
    }), _ => SelectedNode != null);

    /// <summary>
    /// Copy row to clipboard
    /// </summary>
    public ICommand CopyToClipboardCommand => new RelayCommand(() => Utils.SafeExecute(
        () =>
        {
            var item = (Item)GetSelectedTabDataGrid().SelectedItem;
            var data = $"LANG_{item.Name}|{string.Join("|", item.Values.Select(v => $"{v.Key}${v.Value.Value}"))}";
            Clipboard.Clear();
            Utils.CopyToClipboard(data);
        }), _ => SelectedNode != null && GetSelectedTabDataGrid().SelectedItem != null);

    /// <summary>
    /// Paste from clipboard
    /// </summary>
    public ICommand PasteFromClipboard => new RelayCommand(
        () =>
        {
            Item targetItem;
            if (GetSelectedTabDataGrid().SelectedItem is Item selectedItem)
            {
                targetItem = selectedItem;
            }
            else
            {
                targetItem = GetNewItem(GetNewItemName(SelectedNode.Items.LastOrDefault()));
                SelectedNode.Items.Add(targetItem);
            }

            var data = Utils.GetFromClipboard().Replace("LANG_", string.Empty);
            var split = data.Split('|');
            targetItem.Name = split[0];
            foreach (var s in split.Skip(1))
            {
                var value = s.Split('$');
                targetItem.Values[value[0]].Value = value[1];
            }
        },
        _ => SelectedNode != null && Clipboard.ContainsText() && Utils.GetFromClipboard().StartsWith("LANG_"));

    /// <summary>
    /// Remove item
    /// </summary>
    public ICommand RemoveItemCommand => new RelayCommand(
        () => Utils.SafeExecute(() =>
        {
            var item = (Item)GetSelectedTabDataGrid().SelectedItem;
            if (!string.IsNullOrEmpty(item.Comment))
            {
                MessageBox.Show("Позиции, отмеченные комментарием, удалять нельзя!");
                return;
            }

            var result = MessageBox.Show(
                "Нельзя удалять строки из локализации, если плагин уже в релизе! Такие строки следует отмечать комментарием с todo.\nТочно удалить?",
                "Внимание!",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;

            if (!_itemsToRemove.TryGetValue(SelectedNode.Name, out var items))
            {
                items = [];
                _itemsToRemove[SelectedNode.Name] = items;
            }

            items.Add(item.Name);
            SelectedNode.Items.Remove(item);
        }),
        _ => SelectedNode != null && GetSelectedTabDataGrid().SelectedItem != null);

    /// <summary>
    /// Mark item for deletion with comment
    /// </summary>
    public ICommand MarkForDeletionCommand => new RelayCommand(
        () => Utils.SafeExecute(() =>
    {
        var item = (Item)GetSelectedTabDataGrid().SelectedItem;
        var version = string.Empty;

        var match = Regex.Match(item.Comment ?? string.Empty, "\\b\\d+\\.\\d+\\.\\d+\\.\\d+\\b");
        if (match.Success)
            version = match.Value;

        var win = new MarkForDeletionWindow
        {
            Owner = mainWindow,
            TbVersion =
            {
                Text = version
            }
        };

        if (win.ShowDialog() == false)
            return;

        item.Comment = $"todo remove after {win.TbVersion.Text}";

        SelectedNode.Validate();

    }), _ => SelectedNode != null && GetSelectedTabDataGrid().SelectedItem != null);

    public void Load() => Utils.SafeExecute(() =>
    {
        var nodes = new Dictionary<string, Node>();

        foreach (var languageName in LanguageOrder)
        {
            var languageDirectory = GetLanguageDirectory(languageName);

            foreach (var file in Directory.GetFiles(languageDirectory, "*.xml"))
            {
                var xDoc = XElement.Load(file);

                foreach (var xNode in xDoc.Elements())
                {
                    if (!nodes.TryGetValue(xNode.Name.LocalName, out var node))
                    {
                        node = new Node(xNode.Name.LocalName);
                        nodes[xNode.Name.LocalName] = node;
                    }

                    foreach (var xItem in xNode.Elements())
                    {
                        var item = node.Items.FirstOrDefault(i => i.Name == xItem.Name.LocalName);
                        if (item == null)
                        {
                            item = new Item { Name = xItem.Name.LocalName };
                            if (xItem.PreviousNode is XComment xComment)
                            {
                                item.Comment = xComment.Value;
                            }

                            node.Items.Add(item);
                        }

                        item.Add(languageName, new ItemValue { Value = xItem.Value });
                    }
                }
            }
        }

        foreach (var node in nodes.Select(p => p.Value))
        {
            foreach (var nodeItem in node.Items)
            {
                if (nodeItem.Values.Count != LanguageOrder.Count)
                {
                    MessageBox.Show($"Wrong count of values with key {nodeItem.Name} in node {node.Name}. Fix it in files and restart program");
                    CloseWithoutSave = true;
                    mainWindow.Close();
                    return;
                }
            }
        }
        
        foreach (var p in nodes.OrderBy(n => !char.IsUpper(n.Key[0])).ThenBy(n => n.Key))
        {
            Nodes.Add(p.Value);
        }

        LocalVersion = GetLocalVersion()?.ToString();
    });

    public void Save() => Utils.SafeExecute(() =>
    {
        if (IsSaveNotPossible || CloseWithoutSave)
            return;

        foreach (var languageName in LanguageOrder)
        {
            var languageDirectory = GetLanguageDirectory(languageName);

            foreach (var file in Directory.GetFiles(languageDirectory, "*.xml"))
            {
                var save = false;
                var xDoc = XElement.Load(file);

                foreach (var node in Nodes)
                {
                    var xNode = xDoc.Element(node.Name);
                    if (xNode == null)
                        continue;

                    if (_itemsToRemove.TryGetValue(node.Name, out var items))
                    {
                        foreach (var itemName in items)
                        {
                            if (xNode.Element(itemName) is { } xItem)
                            {
                                xItem.Remove();
                                save = true;
                            }
                        }
                    }

                    XElement previousXItem = null;
                    foreach (var item in node.Items)
                    {
                        if (item.HasIncorrectData)
                            continue;

                        var xItem = xNode.Element(item.Name);
                        if (xItem == null)
                        {
                            xItem = new XElement(item.Name);
                            if (previousXItem == null)
                                xNode.AddFirst(xItem);
                            else
                                previousXItem.AddAfterSelf(xItem);
                            save = true;
                        }

                        previousXItem = xItem;

                        if (xItem.Value != item.Values[languageName].Value)
                        {
                            xItem.SetValue(item.Values[languageName].Value);
                            save = true;
                        }

                        if (!string.IsNullOrEmpty(item.Comment))
                        {
                            if (xItem.PreviousNode is XComment xComment)
                                xComment.Value = item.Comment;
                            else
                                xItem.AddBeforeSelf(new XComment(item.Comment));

                            save = true;
                        }
                    }
                }

                if (save)
                {
                    var settings = new XmlWriterSettings
                    {
                        Indent = true,
                        NewLineOnAttributes = true
                    };

                    using (var writer = XmlWriter.Create(file, settings))
                    {
                        xDoc.WriteTo(writer);
                    }
                }
            }
        }

        _itemsToRemove.Clear();
    });

    private Dictionary<string, List<string>> GetRowsFromCopyPaste(string rawCopyPaste)
    {
        Dictionary<string, List<string>> result = [];
        var rows = rawCopyPaste.Split(["\r\n", "\n"], StringSplitOptions.TrimEntries);

        foreach (var row in rows)
        {
            var tag = Utils.GetXmlRowTagContents(row);
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var content = Utils.StripXmlRowOfTag(row);
            if (result.ContainsKey(tag))
            {
                result[tag].Add(content);
            }
            else
            {
                result.Add(tag, [content]);
            }
        }

        foreach (var key in result.Keys)
        {
            if (result[key].Count != LanguageOrder.Count)
            {
                result.Remove(key);
            }
        }

        return result;
    }

    private int SearchLastRowWithTagValue(string tagValue, out int index)
    {
        index = -1;
        var biggestValue = -1;

        if (SelectedNode == null || string.IsNullOrWhiteSpace(tagValue))
        {
            return -1;
        }

        foreach (var item in SelectedNode.Items)
        {
            var name = item.Name;
            Utils.GetTagValueAndNumber(name, out string value, out int number);

            if (value.Equals(tagValue) && SelectedNode.Items.IndexOf(item) > index)
            {
                biggestValue = number;
                index = SelectedNode.Items.IndexOf(item);
            }
        }

        return biggestValue;
    }

    private Item GetNewItem(string name, List<string> valuesInOrder = null)
    {
        var item = new Item
        {
            Name = name
        };

        if (valuesInOrder != null)
        {
            for (var i = 0; i < valuesInOrder.Count; i++)
            {
                item.Add(LanguageOrder[i], new ItemValue { Value = valuesInOrder[i] });
            }
        }
        else
        {
            foreach (var languageName in LanguageOrder)
            {
                item.Add(languageName, new ItemValue());
            }
        }

        return item;
    }

    private static string GetLanguageDirectory(string language)
    {
        return Path.Combine(GetSolutionDirectory(), "LanguageFiles", language);
    }

    private static string GetSolutionDirectory()
    {
        var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var parent = Directory.GetParent(executingDirectory!);
        while (parent!.Name != "LanguageFiles" && parent.Name != "Core_LanguageFiles")
        {
            parent = Directory.GetParent(parent.FullName);
        }

        return parent.FullName;
    }

    private static string GetNewItemName([CanBeNull] Item previousItem)
    {

        if (previousItem is null)
            return string.Empty;
        return GetNewItemName(previousItem.Name);
    }

    private static string GetNewItemName([CanBeNull] string previousItemName)
    {
        if (string.IsNullOrEmpty(previousItemName))
            return string.Empty;
        return Regex.Replace(previousItemName, "\\d+$", match => (int.Parse(match.Value) + 1).ToString());
    }

    private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T tChild)
                return tChild;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }

        return null;
    }

    private DataGrid GetSelectedTabDataGrid()
    {
        if (mainWindow.TcEditors.SelectedItem == null)
            return null;

        // Находим ContentPresenter, который отображает контент выбранной вкладки
        var contentPresenter = FindVisualChild<ContentPresenter>(mainWindow.TcEditors);
        if (contentPresenter == null)
            return null;

        // Попробуем получить DataGrid по имени, если оно задано
        if (contentPresenter.ContentTemplate != null &&
            contentPresenter.ContentTemplate.FindName("PART_DataGrid", contentPresenter) is DataGrid namedGrid)
            return namedGrid;

        // Если имени нет — ищем DataGrid визуально
        return FindVisualChild<DataGrid>(contentPresenter);
    }

}