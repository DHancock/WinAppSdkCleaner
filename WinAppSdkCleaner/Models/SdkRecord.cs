namespace WinAppSdkCleaner.Models;

internal sealed record SdkRecord(VersionRecord Version, ISdk Sdk, IEnumerable<PackageRecord> SdkPackages, int OtherAppsCount = 0);