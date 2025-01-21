namespace LangFilesEditor;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Linq;
using Structure;

internal class MainContext : ObservableObject
{
    private readonly MainWindow _mainWindow;
    private readonly HashSet<string> _languageNames = [];
    private Node _selectedNode;
    private bool _closeWithoutSave;

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
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Close without save
    /// </summary>
    public ICommand CloseWithoutSaveCommand => new RelayCommand(() =>
    {
        _closeWithoutSave = true;
        _mainWindow.Close();
    });

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

                        item.Values[languageName] = new ItemValue { Value = xItem.Value };
                    }
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
        if (_closeWithoutSave)
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

                    foreach (var item in node.Items)
                    {
                        var xItem = xNode.Element(item.Name);
                        if (xItem == null)
                        {
                            xItem = new XElement(item.Name);
                            xNode.Add(xItem);
                            save = true;
                        }

                        if (item.Values.Count != 5)
                            Debug.Print("!");

                        if (xItem.Value != item.Values[languageName].Value)
                        {
                            xItem.SetValue(item.Values[languageName].Value);
                            save = true;
                        }
                    }
                }

                if (save)
                    xDoc.Save(file);
            }
        }
    });

    private IEnumerable<string> GetLanguageDirectories()
    {
        var languageFilesDirectory = Path.Combine(GetSolutionDirectory(), "LanguageFiles");

        return Directory.GetDirectories(languageFilesDirectory, "*", SearchOption.TopDirectoryOnly);
    }

    private void BuildColumns()
    {
        var order = new List<string> { "ru-RU", "en-US", "uk-UA", "de-DE", "es-ES" };

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

    private static DataTemplate GetDataTemplateForStringCell(string columnName)
    {
        var stringReader = new StringReader(
@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox 
        Background=""Transparent""
        BorderThickness=""0""
        Margin=""0""
        TextWrapping=""Wrap""
        Foreground=""{Binding RelativeSource={RelativeSource FindAncestor, AncestorType=DataGridRow}, Path=Foreground}""
        Text=""{Binding Path=" + columnName + @", Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"" />
</DataTemplate>");
        var xmlReader = XmlReader.Create(stringReader);
        return XamlReader.Load(xmlReader) as DataTemplate;
    }

    private static string GetColumnHeader(string languageName)
    {
        return $"{new CultureInfo(languageName).DisplayName}\n{languageName}";
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
}