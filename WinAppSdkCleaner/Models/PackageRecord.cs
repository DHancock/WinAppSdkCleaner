namespace WinAppSdkCleaner.Models;

internal record class PackageRecord(Package Package, List<PackageRecord> PackagesDependantOnThis, int Depth = 0);
