namespace WinAppSdkCleaner.Models;

internal sealed class PackageData
{
    public Package Package { get; init; }
    public List<PackageData> Dependents { get; init; }
    public int OtherAppsCount { get; set; }


    public PackageData(Package package, List<PackageData> dependents)
    {
        Package = package;
        Dependents = dependents;
    }
}
