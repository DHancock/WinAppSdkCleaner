namespace WinAppSdkCleaner.Models;

internal sealed class SdkData
{
    public VersionRecord Version { get; set; }
    public ISdk Sdk { get; }
    public List<PackageData> FrameworkPackages { get; } = new();
    public int OtherAppsCount { get; set; }
    public PackageVersion PackageVersion { get; }

    public SdkData(ISdk sdk, PackageVersion packageVersion)
    {
        PackageVersion = packageVersion;
        Sdk = sdk;

        // this may be updated when the version file has finished loading
        Version = new VersionRecord("", "", sdk.Id, packageVersion, default);
    }
}