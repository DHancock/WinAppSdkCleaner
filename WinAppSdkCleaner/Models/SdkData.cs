namespace WinAppSdkCleaner.Models;

internal sealed class SdkData
{
    public VersionRecord Version { get; init; }
    public ISdk Sdk { get; init; }
    public List<PackageData> SdkPackages { get; init; }
    public int OtherAppsCount { get; set; }


    public SdkData(VersionRecord version, ISdk sdk, List<PackageData> sdkPackages)
    {
        Version = version;
        Sdk = sdk;
        SdkPackages = sdkPackages;
    }
}