namespace WinAppSdkCleaner.Models;

internal static class IntegrityLevel
{
    public static readonly bool IsElevated = GetIsElevated();

    private static bool GetIsElevated()
    {
        try
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        catch (PlatformNotSupportedException)
        {
            return false; 
        }
    }
}
