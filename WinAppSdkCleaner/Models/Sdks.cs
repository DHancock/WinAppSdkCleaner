namespace WinAppSdkCleaner.Models;

internal interface ISdk
{
    string DispalyName { get; }

    bool Match(PackageId pId);

    SdkTypes TypeId { get; }
}


internal sealed class ProjectReunion : ISdk
{
    public string DispalyName => "Project Reunion";

    public bool Match(PackageId pId)
    {
        return pId.FullName.Contains("ProjectReunion", StringComparison.OrdinalIgnoreCase);
    }
   
    public SdkTypes TypeId => SdkTypes.Reunion;
}

internal sealed class WinAppSdk : ISdk
{
    public string DispalyName => "Windows App SDK";

    public bool Match(PackageId pId)
    {
        return pId.FullName.Contains("WinAppRuntime", StringComparison.OrdinalIgnoreCase) ||
                pId.FullName.Contains("WindowsAppRuntime", StringComparison.OrdinalIgnoreCase) ||
                pId.FullName.StartsWith("Microsoft.WindowsAppSDK", StringComparison.Ordinal);  // for 1.0.0 experimental 1 only
    }

    public SdkTypes TypeId => SdkTypes.WinAppSdk;
}
