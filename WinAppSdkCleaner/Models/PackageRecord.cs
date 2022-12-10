namespace WinAppSdkCleaner.Models;

internal sealed class PackageRecord
{
    public Package Package { get; set; }
    public List<PackageRecord> PackagesDependentOnThis { get; set; }
    public int OtherAppsCount { get; set; }
    public int Depth { get; set; }

    public PackageRecord(Package package, List<PackageRecord> packagesDependentOnThis, int otherAppsCount, int depth)
    {
        Package = package;
        PackagesDependentOnThis = packagesDependentOnThis;
        OtherAppsCount = otherAppsCount;
        Depth = depth;
    }
}
