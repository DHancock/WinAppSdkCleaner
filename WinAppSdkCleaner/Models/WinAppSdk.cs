namespace WinAppSdkCleaner.Models;

internal sealed class WinAppSdk : ISdk
{
    public string DisplayName => "Win App SDK";

    public bool Match(PackageId pId)
    {
        return pId.FullName.Contains("WinAppRuntime", StringComparison.OrdinalIgnoreCase) ||
                pId.FullName.Contains("WindowsAppRuntime", StringComparison.OrdinalIgnoreCase) ||
                pId.FullName.StartsWith("Microsoft.WindowsAppSDK", StringComparison.Ordinal);  // for 1.0.0 experimental 1 only
    }

    public SdkId Id => SdkId.WinAppSdk;
}

