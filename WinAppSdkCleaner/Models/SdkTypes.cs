namespace WinAppSdkCleaner.Models;

internal interface ISdk
{
    string DispalyName { get; }

    Func<PackageId, bool> Match { get; }

    SdkTypes TypeId { get; }
}


internal sealed class ProjectReunion : ISdk
{
    public string DispalyName => "Project Reunion";

    public Func<PackageId, bool> Match => (p) =>
    {
        return p.FullName.Contains("ProjectReunion", StringComparison.OrdinalIgnoreCase);
    };
   
    public SdkTypes TypeId => SdkTypes.Reunion;
}

internal sealed class WinAppSdk : ISdk
{
    public string DispalyName => "Windows App SDK";

    public Func<PackageId, bool> Match => (p) =>
    {
        return p.FullName.Contains("WinAppRuntime", StringComparison.OrdinalIgnoreCase) ||
                p.FullName.Contains("WindowsAppRuntime", StringComparison.OrdinalIgnoreCase) ||
                p.FullName.StartsWith("Microsoft.WindowsAppSDK", StringComparison.Ordinal);  // for 1.0.0 experimental 1 only
    };

    public SdkTypes TypeId => SdkTypes.WinAppSdk;
}
