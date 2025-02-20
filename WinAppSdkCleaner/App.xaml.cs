using WinAppSdkCleaner.Views;

namespace WinAppSdkCleaner;

public sealed partial class App : Application
{
    public const string cAppDisplayName = "WinAppSdk Cleaner";
    public static App Instance => (App)Current;


    private MainWindow? m_window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow(cAppDisplayName);
        m_window.Activate();
    }

    internal static MainWindow MainWindow => Instance.m_window!;

    public static string GetAppDataPath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Join(localAppData, "winappsdkcleaner.davidhancock.net");
    }
}
