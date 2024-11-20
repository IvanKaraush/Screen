using System.Windows.Input;

namespace Screen.Command;

public class RelayCommand(Action<object> execute, Func<object?, bool>? canExecute = null) : ICommand
{
    private readonly Action<object> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public bool CanExecute(object? parameter)
    {
        return canExecute == null || canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        if (parameter != null)
        {
            _execute(parameter);
        }
        else
        {
            throw new ArgumentNullException(nameof(parameter));
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}