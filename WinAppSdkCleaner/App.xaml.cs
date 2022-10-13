using WinAppSdkCleaner.Views;

namespace WinAppSdkCleaner;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Trace.Listeners.Add(ViewTraceListener.Instance);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Trace.Listeners.Remove(ViewTraceListener.Instance);
    }
}
