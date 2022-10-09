namespace WinAppSdkCleaner.Models;

internal sealed record SdkRecord(VersionRecord Version, SdkTypes SdkId, List<PackageRecord> SdkPackages);