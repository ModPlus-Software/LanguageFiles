namespace LangFilesEditor.Structure;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

internal class Node : ObservableObject
{
    private bool _hasIncorrectData;
    private string _searchString;
    private bool _hasItemsWithSameValue;

    public Node(string name)
    {
        Name = name;
        Items = [];
        Items.CollectionChanged += ItemsOnCollectionChanged;

        Attributes = [];
        Attributes.CollectionChanged += AttributesOnCollectionChanged;
    }
    
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Attributes
    /// </summary>
    public ObservableCollection<Item> Attributes { get; }

    /// <summary>
    /// Items
    /// </summary>
    public ObservableCollection<Item> Items { get; }
    
    /// <summary>
    /// Search string
    /// </summary>
    public string SearchString
    {
        get => _searchString;
        set
        {
            if (_searchString == value)
                return;
            _searchString = value;
            OnPropertyChanged();
            Search();
        }
    }

    /// <summary>
    /// Has incorrect data
    /// </summary>
    public bool HasIncorrectData
    {
        get => _hasIncorrectData;
        set
        {
            if (_hasIncorrectData == value)
                return;
            _hasIncorrectData = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Has items with same values
    /// </summary>
    public bool HasItemsWithSameValue
    {
        get => _hasItemsWithSameValue;
        set
        {
            if (_hasItemsWithSameValue == value)
                return;
            _hasItemsWithSameValue = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Search items with duplicate names or values
    /// </summary>
    public ICommand SearchWithDuplicateNamesOrValuesCommand => new RelayCommand(() => SearchString = "*");

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }

    private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<Item>())
            {
                item.ValidateInParent += (_, _) => ValidateItems();
            }
        }
    
        ValidateItems();
    }

    private void AttributesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<Item>())
            {
                item.ValidateInParent += (_, _) => ValidateAttributes();
            }
        }

        ValidateAttributes();
    }
    
    public void ValidateItems()
    {
        ValidateItems(Items, out var hasItemsWithSameValue, out var hasIncorrectData);
        HasItemsWithSameValue = hasItemsWithSameValue;
        HasIncorrectData = hasIncorrectData;
    }

    public void ValidateAttributes()
    {
        ValidateItems(Attributes, out var hasItemsWithSameValue, out var hasIncorrectData);
        HasItemsWithSameValue = hasItemsWithSameValue;
        HasIncorrectData = hasIncorrectData;
    }

    private static void ValidateItems(ICollection<Item> items, out bool hasItemsWithSameValue, out bool hasIncorrectData)
    {
        foreach (var item in items)
        {
            item.Validate();
        }

        foreach (var item in items)
        {
            item.HasDuplicateName = false;
        }

        foreach (var group in items
                     .GroupBy(i => i.Name)
                     .Where(g => g.Count() > 1))
        {
            foreach (var item in group)
            {
                item.HasDuplicateName = true;
            }
        }

        hasItemsWithSameValue = false;

        foreach (var item in items)
        {
            item.HasDuplicateValue = false;
        }

        var filtered = items.Where(i => string.IsNullOrEmpty(i.Comment)).ToList();

        var groups = filtered.GroupBy(i => BuildValuesMultisetSignature(i, ignoreCase: false),
            StringComparer.Ordinal);

        foreach (var grp in groups.Where(g => g.Skip(1).Any()))
        {
            foreach (var it in grp)
                it.HasDuplicateValue = true;
            hasItemsWithSameValue = true;
        }


        hasIncorrectData = items.Any(i => i.IsVisibleError);
    }

    private static string BuildValuesMultisetSignature(Item item, bool ignoreCase = false)
    {
        var cmp = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var vals = item.Values.Values
            .Select(v => v?.Value ?? string.Empty)
            .OrderBy(s => s, cmp); // сортируем, чтобы одинаковые наборы давали одинаковую сигнатуру

        // Разделители берём «редкие»:
        const char sep = '\u001F';
        return string.Join(sep, vals);
    }


    private void Search()
    {
        if (string.IsNullOrEmpty(SearchString))
        {
            foreach (var item in Items)
            {
                item.IsVisible = true;
            }
        }
        else if (SearchString == "*")
        {
            foreach (var item in Items)
            {
                item.IsVisible = item.IsVisibleWarning || item.IsVisibleError;
            }
        }
        else
        {
            foreach (var item in Items)
            {
                var isVisible = false;
                foreach (var pair in item.Values)
                {
                    if (pair.Value.Value.IndexOf(SearchString, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        isVisible = true;
                        break;
                    }
                }

                item.IsVisible = isVisible;
            }
        }
    }
}