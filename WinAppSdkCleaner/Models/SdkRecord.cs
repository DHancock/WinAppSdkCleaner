namespace WinAppSdkCleaner.Models;

internal sealed class SdkRecord
{
    public VersionRecord Version { get; set; }
    public ISdk Sdk { get; set; }
    public List<PackageRecord> SdkPackages { get; set; }
    public int OtherAppsCount { get; set; }


    public SdkRecord(VersionRecord version, ISdk sdk, List<PackageRecord> sdkPackages, int otherAppsCount = 0)
    {
        Version = version;
        Sdk = sdk;
        SdkPackages = sdkPackages;
        OtherAppsCount = otherAppsCount;
    }
}