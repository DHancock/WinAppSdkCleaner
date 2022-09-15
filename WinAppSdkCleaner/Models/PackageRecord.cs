namespace WinAppSdkCleaner.Models;

internal record class PackageRecord(Package Package, List<PackageRecord> PackagesDependantOnThis);
