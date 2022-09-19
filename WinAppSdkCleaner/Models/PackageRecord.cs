namespace WinAppSdkCleaner.Models;

internal record class PackageRecord(Package Package, List<PackageRecord> PackagesDependentOnThis, int Depth = 0);
