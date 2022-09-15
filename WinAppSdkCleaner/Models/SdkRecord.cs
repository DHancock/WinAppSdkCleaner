namespace WinAppSdkCleaner.Models;

internal sealed record SdkRecord(VersionRecord Version, List<PackageRecord> SdkPackages);
