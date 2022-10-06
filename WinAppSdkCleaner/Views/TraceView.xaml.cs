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

        Trace.Listeners.Add(new CustomTraceListener(TraceTextBox));

        Loaded += (s, a) => AdjustCommandsState();
    }

    private ViewCommand InitialiseCommand(string key, Action<object?> execute, Func<object?, bool> canExecute)
    {
        ViewCommand command = (ViewCommand)FindResource(key);
        command.CanExecuteProc = canExecute;
        command.ExecuteProc = execute;
        return command;
    }

    public void ExecuteClear(object? param = null) => TraceTextBox.Clear();

    private bool CanClear(object? param = null) => TraceTextBox.Text.Length > 0;

    private void AdjustCommandsState() => clearCommand.RaiseCanExecuteChanged();

    private void TextChanged(object sender, TextChangedEventArgs e) => AdjustCommandsState();


    internal class CustomTraceListener : TraceListener
    {
        private TextBox Consumer { get; init; }

        public CustomTraceListener(TextBox consumer)
        {
            Consumer = consumer;
        }

        public override void Write(string? message)
        {
            if (message is not null)
            {
                Consumer.Dispatcher.Invoke(() =>
                {
                    int selectionStart = Consumer.SelectionStart;
                    int selectionLength = Consumer.SelectionLength;
                    
                    Consumer.Text += message;

                    if (selectionLength > 0)
                        Consumer.Select(selectionStart, selectionLength);
                    else
                        Consumer.CaretIndex = Consumer.Text.Length;
                });
            }
        }

        public override void WriteLine(string? message) => Write(message + Environment.NewLine);
    }
}

