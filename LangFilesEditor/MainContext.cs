﻿namespace LangFilesEditor;

using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Linq;
using Windows;
using JetBrains.Annotations;
using Structure;

internal partial class MainContext(MainWindow mainWindow) : ObservableObject
{
    private readonly Dictionary<string, List<string>> _itemsToRemove = [];
    private Node _selectedNode;

    /// <summary>
    /// Порядок языков
    /// </summary>
    public static List<string> LanguageOrder = ["ru-RU", "uk-UA", "en-US", "de-DE", "es-ES", "zh-CN"];


    /// <summary>
    /// Sections
    /// </summary>
    public ObservableCollection<Node> Nodes { get; } = [];

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
    /// Close without save
    /// </summary>
    public ICommand CloseWithoutSaveCommand => new RelayCommand(() =>
    {
        CloseWithoutSave = true;
        mainWindow.Close();
    });

    /// <summary>
    /// Add row above
    /// </summary>
    public ICommand AddRowAboveCommand => new RelayCommand(() => Utils.SafeExecute(() =>
    {
        if (mainWindow.DgItems.SelectedItem != null)
        {
            var index = mainWindow.DgItems.Items.IndexOf(mainWindow.DgItems.SelectedItem);
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
        if (mainWindow.DgItems.SelectedItem != null)
        {
            var index = mainWindow.DgItems.Items.IndexOf(mainWindow.DgItems.SelectedItem) + 1;
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

        if (mainWindow.DgItems.SelectedItem != null)
        {
            index = mainWindow.DgItems.Items.IndexOf(mainWindow.DgItems.SelectedItem) + 1;
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
    /// Copy row to clipboard
    /// </summary>
    public ICommand CopyToClipboardCommand => new RelayCommand(() => Utils.SafeExecute(
        () =>
        {
            var item = (Item)mainWindow.DgItems.SelectedItem;
            var data = $"LANG_{item.Name}|{string.Join("|", item.Values.Select(v => $"{v.Key}${v.Value.Value}"))}";
            Clipboard.Clear();
            Utils.CopyToClipboard(data);
        }), _ => SelectedNode != null && mainWindow.DgItems.SelectedItem != null);

    /// <summary>
    /// Paste from clipboard
    /// </summary>
    public ICommand PasteFromClipboard => new RelayCommand(
        () =>
        {
            Item targetItem;
            if (mainWindow.DgItems.SelectedItem is Item selectedItem)
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
            var item = (Item)mainWindow.DgItems.SelectedItem;
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
        _ => SelectedNode != null && mainWindow.DgItems.SelectedItem != null);

    /// <summary>
    /// Mark item for deletion with comment
    /// </summary>
    public ICommand MarkForDeletionCommand => new RelayCommand(
        () => Utils.SafeExecute(() =>
    {
        var item = (Item)mainWindow.DgItems.SelectedItem;
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

    }), _ => SelectedNode != null && mainWindow.DgItems.SelectedItem != null);

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

        BuildColumns();

        foreach (var p in nodes.OrderBy(n => !char.IsUpper(n.Key[0])).ThenBy(n => n.Key))
        {
            Nodes.Add(p.Value);
        }

        LocalVersion = GetLocalVersion()?.ToString();
    });

    public void Save() => Utils.SafeExecute(() =>
    {
        if (CloseWithoutSave)
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

    private void BuildColumns()
    {
        foreach (var languageName in LanguageOrder)
        {
            mainWindow.DgItems.Columns.Add(new DataGridTemplateColumn
            {
                Header = GetColumnHeader(languageName),
                CellTemplate = GetDataTemplateForStringCell($"Values[{languageName}].Value"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }
    }

    private DataTemplate GetDataTemplateForStringCell(string bindingPath)
    {
        var dataTemplate = (DataTemplate)mainWindow.DgItems.Resources["ItemValueCellTemplate"];
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
}