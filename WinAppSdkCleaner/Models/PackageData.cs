namespace WinAppSdkCleaner.Models;

internal sealed class PackageData
{
    public Package Package { get; init; }
    public List<PackageData> PackagesDependentOnThis { get; init; }
    public int OtherAppsCount { get; set; }


    public PackageData(Package package, List<PackageData> packagesDependentOnThis)
    {
        Package = package;
        PackagesDependentOnThis = packagesDependentOnThis;
    }
}
