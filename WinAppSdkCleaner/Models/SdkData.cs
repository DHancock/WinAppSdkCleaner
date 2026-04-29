namespace WinAppSdkCleaner.Models;

internal sealed class SdkData
{
    public VersionRecord Version { get; set; }
    public ISdk Sdk { get; }
    public List<PackageData> FrameworkPackages { get; }
    public int OtherAppsCount { get; set; }
    public PackageVersion PackageVersion { get; }

    public SdkData(ISdk sdk, PackageVersion packageVersion, List<PackageData> frameworks)
    {
        PackageVersion = packageVersion;
        Sdk = sdk;
        FrameworkPackages = frameworks;

        // this may be updated when the version file has finished loading
        Version = new VersionRecord("", "", sdk.Id, packageVersion, default);
    }
}