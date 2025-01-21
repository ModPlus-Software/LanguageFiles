namespace LangFilesEditor.Structure;

using System;
using System.Windows.Input;

/// <summary>
/// RelayCommand without parameter
/// </summary>
/// <seealso cref="ICommand" />
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<object, bool> _canExecute;

    /// <summary>
    /// Возвращает имя команды
    /// </summary>
    public string CommandName { get; }

    /// <summary>
    /// Происходит при изменениях, влияющих на то, должна выполняться данная команда или нет.
    /// </summary>
    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand" /> class.
    /// </summary>
    /// <param name="execute">The execute.</param>
    /// <param name="canExecute">The can execute.</param>
    public RelayCommand(Action execute, Func<object, bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand" /> class.
    /// </summary>
    /// <param name="execute">The execute.</param>
    /// <param name="commandName">Command name. It is used for display running command name at the bot left corner</param>
    /// <param name="canExecute">The can execute.</param>
    public RelayCommand(Action execute, string commandName, Func<object, bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        CommandName = commandName;
    }

    /// <summary>
    /// Определяет метод, который определяет, может ли данная команда выполняться в ее текущем состоянии.
    /// </summary>
    /// <param name="parameter">Данные, используемые данной командой.
    /// Если команда не требует передачи данных, этому объект может быть присвоено значение <see langword="null" />.</param>
    /// <returns>
    /// Значение <see langword="true" />, если эту команду можно выполнить; в противном случае — значение <see langword="false" />.
    /// </returns>
    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    /// <summary>
    /// Определяет метод, вызываемый при вызове данной команды.
    /// </summary>
    /// <param name="parameter">Данные, используемые данной командой.
    /// Если команда не требует передачи данных, этому объекту можно присвоить значение <see langword="null" />.</param>
    public void Execute(object parameter)
    {
        _execute();
    }
}

/// <summary>
/// Generic RelayCommand
/// </summary>
/// <seealso cref="System.Windows.Input.ICommand" />
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<object, bool> _canExecute;

    /// <summary>
    /// Происходит при изменениях, влияющих на то, должна выполняться данная команда или нет.
    /// </summary>
    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand{T}" /> class.
    /// </summary>
    /// <param name="execute">The execute.</param>
    /// <param name="canExecute">The can execute.</param>
    public RelayCommand(Action<T> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Определяет метод, который определяет, может ли данная команда выполняться в ее текущем состоянии.
    /// </summary>
    /// <param name="parameter">Данные, используемые данной командой.
    /// Если команда не требует передачи данных, этому объект может быть присвоено значение <see langword="null" />.</param>
    /// <returns>
    /// Значение <see langword="true" />, если эту команду можно выполнить; в противном случае — значение <see langword="false" />.
    /// </returns>
    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    /// <summary>
    /// Определяет метод, вызываемый при вызове данной команды.
    /// </summary>
    /// <param name="parameter">Данные, используемые данной командой.
    /// Если команда не требует передачи данных, этому объекту можно присвоить значение <see langword="null" />.</param>
    public void Execute(object parameter)
    {
        _execute((T)parameter);
    }
}