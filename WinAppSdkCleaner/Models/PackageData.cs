namespace WinAppSdkCleaner.Models;

[System.Diagnostics.DebuggerDisplay("{Package.DisplayName, nq} {Dependents.Count, nq}")]

internal sealed class PackageData
{
    public Package Package { get; init; }
    public List<PackageData> Dependents { get; } = new();
    public int OtherAppsCount { get; set; }


    public PackageData(Package package)
    {
        Package = package;
    }
}
