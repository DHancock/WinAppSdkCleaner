using Microsoft.UI.Dispatching;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace WinAppSdkCleaner;

public static class Program
{
    [STAThread]
    static void Main()
    {
        // Create the installer mutexes with current user access. The app is installed per
        // user rather than all users. It isn't obvious what the .Net Mutex class is creating.
        const string name = "4ACA5302-CE42-4882-AA6E-FC54667A934B";

        SafeHandle localMutex = PInvoke.CreateMutex(null, false, name);
        SafeHandle globalMutex = PInvoke.CreateMutex(null, false, "Global\\" + name);

        Trace.Listeners.Add(new ViewTraceListener());

        Application.Start((p) =>
        {
            DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}