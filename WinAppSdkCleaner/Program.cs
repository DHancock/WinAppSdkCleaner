namespace WinAppSdkCleaner;

public static class Program
{
    [STAThread]
    static void Main()
    {
        // Create the installer mutexes with current user access. The app is installed per
        // user rather than all users. It isn't obvious what the .Net Mutex class is creating.
        const string name = "4ACA5302-CE42-4882-AA6E-FC54667A934B";

        PInvoke.CreateMutex(null, false, name);
        PInvoke.CreateMutex(null, false, "Global\\" + name);

        Trace.Listeners.Add(new ViewTraceListener());

        XamlGeneratedProgram.XamlGeneratedMain();
    }
}