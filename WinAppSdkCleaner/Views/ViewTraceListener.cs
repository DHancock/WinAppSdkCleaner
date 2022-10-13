namespace WinAppSdkCleaner.Views;

internal class ViewTraceListener : TraceListener
{
    private readonly object lockObject = new object(); 
    private string store = string.Empty;
    private TextBox? Consumer { get; set; }

    public static ViewTraceListener Instance = new ViewTraceListener();

    private ViewTraceListener()
    {
    }

    public void RegisterConsumer(TextBox consumer)
    {
        Debug.Assert((Consumer is null) && consumer.IsInitialized);

        lock (lockObject)
        {
            Consumer = consumer;

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
