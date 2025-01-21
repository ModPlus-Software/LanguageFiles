namespace LangFilesEditor.Structure;

using System.Collections.ObjectModel;

internal class Node(string name)
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Items
    /// </summary>
    public ObservableCollection<Item> Items { get; } = [];

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}