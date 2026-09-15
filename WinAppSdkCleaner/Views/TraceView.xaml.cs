using WinAppSdkCleaner.Utilities;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for TraceView.xaml
/// </summary>
internal sealed partial class TraceView : Page, IPageItem
{
    private ViewTraceListener? viewTraceListener;

    public TraceView()
    {
        InitializeComponent();
        RegisterConsumer();
    }

    private void RegisterConsumer()
    {
        TraceTextBlock.Loaded += TraceTextBlock_Loaded;

        void TraceTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            TraceTextBlock.Loaded -= TraceTextBlock_Loaded;
            TraceTextBlock.ContextFlyout.Opening += ContextFlyout_Opening;

            foreach (TraceListener listener in Trace.Listeners)
            {
                if (listener is ViewTraceListener traceListener)
                {
                    viewTraceListener = traceListener;
                    viewTraceListener.RegisterConsumer(TraceTextBlock, TraceScrollViewer);
                    break;
                }
            }
        };
    }

    private static void ContextFlyout_Opening(object? sender, object e)
    {
        if ((sender is TextCommandBarFlyout tcbf) && (tcbf.Target is RichTextBlock rtb))
        {
            foreach (ICommandBarElement icbe in tcbf.SecondaryCommands)
            {
                if ((icbe is AppBarButton abb) && (abb.ActualTheme != rtb.ActualTheme))
                {
                    // fix the menu item's text colour for theme changes after the context flyout was created
                    // (this will also fix each menu item's tool tip colours)
                    abb.RequestedTheme = rtb.ActualTheme;
                }
            }
        }
    }

    public int PassthroughCount => 2;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(TraceScrollViewer);
        rects[1] = Utils.GetPassthroughRect(ClearButton);
    }

    public bool InvokeKeyboardAccelerator(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        return false;
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        viewTraceListener?.Clear();
    }
}

