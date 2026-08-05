namespace LangFilesEditor.Models;

using System.Windows.Input;
using ModPlusAPI.Mvvm;

/// <summary>
/// Строка раскрывающегося списка диагностики: модуль и число проблем в нём.
/// </summary>
public sealed class DiagnosticModuleEntry : ObservableObject
{
    private int _count;

    /// <summary>
    /// Создаёт запись для модуля.
    /// </summary>
    /// <param name="module">Модуль, в котором найдены проблемы.</param>
    /// <param name="severity">Категория диагностики для этой записи.</param>
    /// <param name="selectCommand">Команда перехода к модулю по клику.</param>
    public DiagnosticModuleEntry(Module module, DiagnosticSeverity severity, ICommand selectCommand)
    {
        Module = module;
        Severity = severity;
        SelectCommand = selectCommand;
    }

    /// <summary>
    /// Категория диагностики (ошибка, предупреждение, обновление).
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// Модуль с проблемами.
    /// </summary>
    public Module Module { get; }

    /// <summary>
    /// Имя модуля для отображения.
    /// </summary>
    public string ModuleName => Module.Name;

    /// <summary>
    /// Команда перехода к модулю; параметр — <see cref="Module"/>.
    /// </summary>
    public ICommand SelectCommand { get; }

    /// <summary>
    /// Число проблем данной категории в модуле.
    /// </summary>
    public int Count
    {
        get => _count;
        set
        {
            if (_count == value)
            {
                return;
            }

            _count = value;
            OnPropertyChanged();
        }
    }
}