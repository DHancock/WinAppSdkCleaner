namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for TraceView.xaml
/// </summary>
internal sealed partial class TraceView : Page
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
}

