namespace WinAppSdkCleaner.Views;

internal class ViewTraceListener : TraceListener
{
    private readonly object lockObject = new object(); 
    private string store = string.Empty;
    private TextBox? Consumer { get; set; }

    public ViewTraceListener() : base(nameof(ViewTraceListener))
    {
    }

    public void RegisterConsumer(TextBox textBox)
    {
        Debug.Assert((Consumer is null) && textBox.IsInitialized);

        lock (lockObject)
        {
            Consumer = textBox;

            if (!string.IsNullOrEmpty(store))
            {
                WriteInternal(store);
                store = string.Empty;
            }
        }
    }

    public override bool IsThreadSafe { get; } = true;

    private void WriteInternal(string message)
    {
        int margin = IndentLevel * IndentSize;

        if (margin > 0)
            message = new string(' ', margin) + message;

        if (Consumer is null)
        {
            store += message;
            Debug.Assert(store.Length < 10240);
        }
        else
        {
            Consumer.Dispatcher.BeginInvoke(() => 
            {
                int selectionStart = Consumer.SelectionStart;
                int selectionLength = Consumer.SelectionLength;

                Consumer.Text += message;

                if (selectionLength > 0)
                    Consumer.Select(selectionStart, selectionLength);
                else
                    Consumer.CaretIndex = Consumer.Text.Length;
            }, 
            DispatcherPriority.Render);
        }
    }

    public override void Write(string? message)
    {
        if (message is not null)
        {
            lock (lockObject)
            {
                WriteInternal(message);
            }
        }
    }

    public override void WriteLine(string? message) => Write(message + Environment.NewLine);
}
