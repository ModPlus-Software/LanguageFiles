namespace LangFilesEditor.Models;

using System.Collections.ObjectModel;
using ModPlusAPI.Mvvm;

/// <summary>
/// Домен локализации — логическая группа модулей (например, AutoCAD, Revit).
/// </summary>
public class Domain : ObservableObject
{
    private string _name;
    private bool _isExpanded;
    private ObservableCollection<Module> _modules = [];

    /// <summary>
    /// Имя домена.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Является ли домен общим (Common): хранит строки, доступные всем модулям.
    /// Всё, что не попало в конкретную категорию, относится к этому домену.
    /// </summary>
    public bool IsCommon { get; init; }

    /// <summary>
    /// Развёрнут ли узел домена в навигационном дереве.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Модули домена.
    /// </summary>
    public ObservableCollection<Module> Modules
    {
        get => _modules;
        set
        {
            if (_modules == value)
            {
                return;
            }

            _modules = value;
            OnPropertyChanged();
        }
    }
}