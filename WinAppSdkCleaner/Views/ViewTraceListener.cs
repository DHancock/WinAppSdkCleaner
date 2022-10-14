namespace WinAppSdkCleaner.Views;

internal class ViewTraceListener : TraceListener
{
    private readonly object lockObject = new object(); 
    private StringBuilder? store = null;
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

            if (store is not null && store.Length > 0)
            {
                WriteInternal(store.ToString());
                store = null;
            }
        }
    }

    public override bool IsThreadSafe { get; } = true;

    private void WriteInternal(string message)
    {
        const int cMaxStoreLength = 1024 * 10;

        try
        {
            if (Consumer is null)
            {
                if (store is null)
                    store = new StringBuilder();

                store.Append(message);

                if (store.Length > cMaxStoreLength)
                    store.Remove(0, cMaxStoreLength / 2);
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
                DispatcherPriority.Background);
            }
        }
        catch 
        {
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
