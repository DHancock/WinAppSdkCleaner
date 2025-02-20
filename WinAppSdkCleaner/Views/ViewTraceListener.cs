using WinAppSdkCleaner.Utils;

namespace WinAppSdkCleaner.Views;

internal sealed partial class ViewTraceListener : TraceListener
{
    private readonly Lock lockObject;
    private readonly StringBuilder store;
    private TextBox? consumer;
    private ScrollViewer? scrollViewer;
    private readonly DispatcherTimer dispatcherTimer;

    public ViewTraceListener() : base(nameof(ViewTraceListener))
    {
        lockObject = new Lock();

        dispatcherTimer = new DispatcherTimer();
        dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
        dispatcherTimer.Tick += DispatcherTimer_Tick;

        store = new StringBuilder(1000);
    }

    private void DispatcherTimer_Tick(object? sender, object e)
    {
        lock (lockObject)
        {
            if ((consumer is not null) && (scrollViewer is not null))
            {
                if (store.Length > 0)
                {
                    int start = consumer.SelectionStart;
                    int length = consumer.SelectionLength;

                    consumer.Text = string.Concat(consumer.Text.AsSpan(), store.ToString().AsSpan());

                    consumer.SelectionStart = start;
                    consumer.SelectionLength = length;

                    store.Clear();
                }

                if (scrollViewer.ChangeView(0.0, scrollViewer.ExtentHeight, 1.0f))
                {
                    dispatcherTimer.Stop();
                }
            }
        }
    }

    public void RegisterConsumer(TextBox textBox)
    {
        lock (lockObject)
        {
            Debug.Assert(consumer is null);
            Debug.Assert(textBox.IsLoaded);

            consumer = textBox;
            scrollViewer = consumer.FindChild<ScrollViewer>();

            if (store.Length > 0)
            {
                dispatcherTimer.Start();
            }
        }
    }

    public override bool IsThreadSafe { get; } = true;

    public override void Write(string? message)
    {
        lock (lockObject)
        {
            try
            {
                store.Append(message);

                if (!dispatcherTimer.IsEnabled && (consumer is not null))
                {
                    dispatcherTimer.Start();
                }
            }
            catch
            {
            }
        }
    }

    public override void WriteLine(string? message) => Write(message + Environment.NewLine);
}
