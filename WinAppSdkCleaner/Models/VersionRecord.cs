namespace WinAppSdkCleaner.Models;

internal sealed record VersionRecord(string SemanticVersion, string VersionTag, SdkId SdkId, PackageVersion Release)
{
    public string PackageVersionStr => $"{Release.Major}.{Release.Minor}.{Release.Build}.{Release.Revision}";
}
