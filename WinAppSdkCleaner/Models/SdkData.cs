namespace WinAppSdkCleaner.Models;

internal sealed class SdkData
{
    public VersionRecord Version { get; set; }
    public ISdk Sdk { get; set; }
    public List<PackageData> SdkPackages { get; set; }
    public int OtherAppsCount { get; set; }


    public SdkData(VersionRecord version, ISdk sdk, List<PackageData> sdkPackages, int otherAppsCount = 0)
    {
        Version = version;
        Sdk = sdk;
        SdkPackages = sdkPackages;
        OtherAppsCount = otherAppsCount;
    }
}