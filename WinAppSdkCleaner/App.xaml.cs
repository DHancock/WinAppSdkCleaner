using WinAppSdkCleaner.Views;

namespace WinAppSdkCleaner;

public sealed partial class App : Application
{
    public const string cAppDisplayName = "WinAppSdk Cleaner";
    public static App Instance => (App)Current;

    private readonly SafeHandle localMutex;
    private readonly SafeHandle globalMutex;

    private MainWindow? m_window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        // Create the installer mutexes with current user access. The app is installed per
        // user rather than all users. It isn't obvious what the .Net Mutex class is creating.
        const string name = "4ACA5302-CE42-4882-AA6E-FC54667A934B";
        localMutex = PInvoke.CreateMutex(null, false, name);
        globalMutex = PInvoke.CreateMutex(null, false, "Global\\" + name);

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
