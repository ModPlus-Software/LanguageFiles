namespace LangFilesEditor;

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
using JetBrains.Annotations;
using Structure;

internal partial class MainContext : ObservableObject
{
    private readonly MainWindow _mainWindow;
    private readonly HashSet<string> _languageNames = [];
    private Node _selectedNode;

    public MainContext(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        Nodes = [];
    }

    /// <summary>
    /// Sections
    /// </summary>
    public ObservableCollection<Node> Nodes { get; }

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
        _mainWindow.Close();
    });

    /// <summary>
    /// Add row above
    /// </summary>
    public ICommand AddRowAboveCommand => new RelayCommand(() => Utils.SafeExecute(() =>
    {
        if (_mainWindow.DgItems.SelectedItems.Count > 0)
        {
            var index = _mainWindow.DgItems.Items.IndexOf(_mainWindow.DgItems.SelectedItems[0]!);
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
        if (_mainWindow.DgItems.SelectedItems.Count > 0)
        {
            var index = _mainWindow.DgItems.Items.IndexOf(_mainWindow.DgItems.SelectedItems[^1]!) + 1;
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
    /// Copy row to clipboard
    /// </summary>
    public ICommand CopyToClipboardCommand => new RelayCommand(() => Utils.SafeExecute(
        () =>
        {
            var item = (Item)_mainWindow.DgItems.SelectedItem;
            var data = $"LANG_{item.Name}|{string.Join("|", item.Values.Select(v => $"{v.Key}${v.Value.Value}"))}";
            Clipboard.Clear();
            Utils.CopyToClipboard(data);
        }), _ => SelectedNode != null && _mainWindow.DgItems.SelectedItem != null);

    /// <summary>
    /// Paste from clipboard
    /// </summary>
    public ICommand PasteFromClipboard => new RelayCommand(
        () =>
        {
            Item targetItem;
            if (_mainWindow.DgItems.SelectedItem is Item selectedItem)
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

    public void Load() => Utils.SafeExecute(() =>
    {
        var nodes = new Dictionary<string, Node>();

        foreach (var languageDirectory in GetLanguageDirectories())
        {
            var languageName = new DirectoryInfo(languageDirectory).Name;
            _languageNames.Add(languageName);

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
                if (nodeItem.Values.Count != _languageNames.Count)
                {
                    MessageBox.Show($"Wrong count of values with key {nodeItem.Name} in node {node.Name}. Fix it in files and restart program");
                    CloseWithoutSave = true;
                    _mainWindow.Close();
                    return;
                }
            }
        }

        BuildColumns();

        foreach (var p in nodes.OrderBy(n => n.Key))
        {
            Nodes.Add(p.Value);
        }
    });

    public void Save() => Utils.SafeExecute(() =>
    {
        if (CloseWithoutSave)
            return;

        foreach (var languageDirectory in GetLanguageDirectories())
        {
            var languageName = new DirectoryInfo(languageDirectory).Name;

            foreach (var file in Directory.GetFiles(languageDirectory, "*.xml"))
            {
                var save = false;
                var xDoc = XElement.Load(file);

                foreach (var node in Nodes)
                {
                    var xNode = xDoc.Element(node.Name);
                    if (xNode == null)
                        continue;

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
    });

    private Item GetNewItem(string name)
    {
        var item = new Item
        {
            Name = name
        };
        foreach (var languageName in _languageNames)
        {
            item.Add(languageName, new ItemValue());
        }

        return item;
    }

    private void BuildColumns()
    {
        var order = new List<string> { "ru-RU", "uk-UA", "en-US", "de-DE", "es-ES" };

        foreach (var languageName in _languageNames.OrderBy(order.IndexOf))
        {
            _mainWindow.DgItems.Columns.Add(new DataGridTemplateColumn
            {
                Header = GetColumnHeader(languageName),
                CellTemplate = GetDataTemplateForStringCell($"Values[{languageName}].Value"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }
    }

    private  DataTemplate GetDataTemplateForStringCell(string bindingPath)
    {
        var dataTemplate = (DataTemplate)_mainWindow.DgItems.Resources["ItemValueCellTemplate"];
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

    private static IEnumerable<string> GetLanguageDirectories()
    {
        var languageFilesDirectory = Path.Combine(GetSolutionDirectory(), "LanguageFiles");

        return Directory.GetDirectories(languageFilesDirectory, "*", SearchOption.TopDirectoryOnly);
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
        return Regex.Replace(previousItem.Name, "\\d+$", match => (int.Parse(match.Value) + 1).ToString());
    }
}