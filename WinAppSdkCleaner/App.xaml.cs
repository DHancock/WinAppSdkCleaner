using WinAppSdkCleaner.Views;

namespace WinAppSdkCleaner;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ViewTraceListener traceListener = new ViewTraceListener();

    protected override void OnStartup(StartupEventArgs e)
    {
        Trace.Listeners.Add(traceListener);
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        Trace.Listeners.Remove(traceListener);
    }
}
