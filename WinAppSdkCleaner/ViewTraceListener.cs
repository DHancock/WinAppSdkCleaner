using WinAppSdkCleaner.Utilities;

namespace WinAppSdkCleaner;

internal sealed partial class ViewTraceListener : TraceListener
{
    private const int cMaxCapacity = 1024 * 10;
    private const int cInitialCapacity = 1024 * 2;
    private readonly Lock lockObject;
    private readonly StringBuilder store;
    private RichTextBlock? consumer;
    private Paragraph? paragraph;
    private ScrollViewer? scrollViewer;
    private DispatcherTimer? dispatcherTimer;

    public ViewTraceListener() : base(nameof(ViewTraceListener))
    {
        lockObject = new Lock();
        store = new StringBuilder(cInitialCapacity);
    }

    private void DispatcherTimer_Tick(object? sender, object e)
    {
        lock (lockObject)
        {
            Debug.Assert(consumer is not null);
            Debug.Assert(scrollViewer is not null);
            Debug.Assert(dispatcherTimer is not null);

            if (store.Length > 0)
            {
                paragraph?.Inlines.Add(new Run() { Text = store.ToString() });
                store.Clear();
            }                                                           

            if (scrollViewer.ChangeView(0.0, scrollViewer.ExtentHeight, 1.0f))
            {
                // ChangeView will return false if it hasn't completed scrolling or if no scrolling is required.
                // So until there's enough text to actually need scrolling the timer will remain running...
                dispatcherTimer.Stop();
            }
        }
    }

    public void RegisterConsumer(RichTextBlock textBlock, ScrollViewer textScrollViewer)
    {
        lock (lockObject)
        {
            Debug.Assert(consumer is null);
            Debug.Assert(dispatcherTimer is null);
            Debug.Assert(textBlock.IsLoaded);

            consumer = textBlock;
            scrollViewer = textScrollViewer;

            // paragraphs have to be created on the ui thread
            paragraph = new Paragraph();
            consumer.Blocks.Add(paragraph);

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
            dispatcherTimer.Tick += DispatcherTimer_Tick;

            if (store.Length > 0)
            {
                dispatcherTimer.Start();
            }
        }
    }

    public void Clear()
    {
        lock (lockObject)
        {
            dispatcherTimer?.Stop();
            store.Clear();
            paragraph?.Inlines.Clear();
        }
    }

    private void WriteInternal(string? message)
    {
        lock (lockObject)
        {
            try
            {
                store.Append(message);

                if (store.Length > cMaxCapacity)
                {
                    store.Remove(0, cMaxCapacity / 2);   // very unlikely but prudent
                }

                if (dispatcherTimer?.IsEnabled == false)
                {
                    dispatcherTimer?.Start();
                }
            }
            catch
            {
            }
        }
    }

    public override void Write(string? message) => WriteInternal(message);

    public override void WriteLine(string? message) => WriteInternal(message + Environment.NewLine);
}
