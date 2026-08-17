using WinAppSdkCleaner.Utilities;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for TraceView.xaml
/// </summary>
internal sealed partial class TraceView : Page, IPageItem
{
    private RelayCommand ClearCommand { get; }

    public TraceView()
    {
        InitializeComponent();

        ClearCommand = new RelayCommand(ExecuteClear, CanClear);

        RegisterConsumer();

        Loaded += (s, a) => AdjustCommandsState();
    }


    private void RegisterConsumer()
    {
        TraceTextBox.Loaded += TraceTextBox_Loaded;

        void TraceTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            TraceTextBox.Loaded -= TraceTextBox_Loaded;

            TraceTextBox.ContextFlyout.Opening += ContextFlyout_Opening;

            foreach (TraceListener listener in Trace.Listeners)
            {
                if (listener is ViewTraceListener viewTraceListener)
                {
                    viewTraceListener.RegisterConsumer(TraceTextBox);
                    return;
                }
            }

            TraceTextBox.Text = "failed to find trace listener";
        };
    }

    private static void ContextFlyout_Opening(object? sender, object e)
    {
        if ((sender is TextCommandBarFlyout tcbf) && (tcbf.Target is TextBox tb))
        {
            foreach (ICommandBarElement icbe in tcbf.SecondaryCommands)
            {
                if ((icbe is AppBarButton abb) && (abb.ActualTheme != tb.ActualTheme))
                {
                    // fix the menu item's text colour for theme changes after the context flyout was created
                    // (this will also fix each menu item's tool tip colours)
                    abb.RequestedTheme = tb.ActualTheme;
                }
            }
        }
    }

    public void ExecuteClear(object? param) => TraceTextBox.Text = string.Empty;

    private bool CanClear(object? param) => TraceTextBox.Text.Length > 0;

    private void AdjustCommandsState() => ClearCommand.RaiseCanExecuteChanged();

    private void TextChanged(object sender, TextChangedEventArgs e) => AdjustCommandsState();

    public int PassthroughCount => 2;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(TraceTextBox);
        rects[1] = Utils.GetPassthroughRect(ClearButton);
    }
}

