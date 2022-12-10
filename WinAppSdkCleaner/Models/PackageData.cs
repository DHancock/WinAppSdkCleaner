namespace WinAppSdkCleaner.Models;

internal sealed class PackageData
{
    public Package Package { get; set; }
    public List<PackageData> PackagesDependentOnThis { get; set; }
    public int OtherAppsCount { get; set; }
    public int Depth { get; set; }

    public PackageData(Package package, List<PackageData> packagesDependentOnThis, int otherAppsCount, int depth)
    {
        Package = package;
        PackagesDependentOnThis = packagesDependentOnThis;
        OtherAppsCount = otherAppsCount;
        Depth = depth;
    }
}
