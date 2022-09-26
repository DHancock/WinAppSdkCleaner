namespace WinAppSdkCleaner.Models;

internal sealed record class PackageRecord(Package Package, List<PackageRecord> PackagesDependentOnThis, int Depth = 0);
