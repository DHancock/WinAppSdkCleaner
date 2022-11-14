namespace WinAppSdkCleaner.Models;

internal sealed record SdkRecord(VersionRecord Version, ISdk Sdk, List<PackageRecord> SdkPackages, int OtherAppsCount = 0);