using WinAppSdkCleaner.Utilites;

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

