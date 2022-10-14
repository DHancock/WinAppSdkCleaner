namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for TraceView.xaml
/// </summary>
public partial class TraceView : UserControl
{
    private readonly ViewCommand clearCommand;

    public TraceView()
    {
        InitializeComponent();

        clearCommand = InitialiseCommand("Clear", ExecuteClear, CanClear);

        foreach (TraceListener listener in Trace.Listeners)
        {
            if (listener is ViewTraceListener viewTraceListener)
            {
                viewTraceListener.RegisterConsumer(TraceTextBox);
                break;
            }
        }

        Loaded += (s, a) => AdjustCommandsState();
    }

    private ViewCommand InitialiseCommand(string key, Action<object?> execute, Func<object?, bool> canExecute)
    {
        ViewCommand command = (ViewCommand)FindResource(key);
        command.CanExecuteProc = canExecute;
        command.ExecuteProc = execute;
        return command;
    }

    public void ExecuteClear(object? param) => TraceTextBox.Clear();

    private bool CanClear(object? param) => TraceTextBox.Text.Length > 0;

    private void AdjustCommandsState() => clearCommand.RaiseCanExecuteChanged();

    private void TextChanged(object sender, TextChangedEventArgs e) => AdjustCommandsState();
}

