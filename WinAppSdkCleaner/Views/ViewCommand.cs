namespace WinAppSdkCleaner.Views;

internal sealed partial class ViewCommand : ICommand
{
    public Action<object?>? ExecuteProc { get; set; }
    public Func<object?, bool>? CanExecuteProc { get; set; }

    public bool CanExecute(object? parameter)
    {
        return (CanExecuteProc is null) || CanExecuteProc(parameter);
    }

    public void Execute(object? parameter)
    {
        if (ExecuteProc is null)
            throw new NotImplementedException(nameof(ExecuteProc));

        ExecuteProc(parameter);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public event EventHandler? CanExecuteChanged;
}
