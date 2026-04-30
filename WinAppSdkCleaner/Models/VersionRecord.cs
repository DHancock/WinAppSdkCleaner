namespace WinAppSdkCleaner.Models;

[System.Diagnostics.DebuggerDisplay("{SdkId} {SemanticVersion, nq} {VersionTag, nq} {PackageVersionStr, nq} {SingletonVersionStr, nq}")]
internal sealed record VersionRecord(string SemanticVersion, string VersionTag, SdkId SdkId, PackageVersion Release, PackageVersion Singleton)
{
    public bool IsSynthesized => string.IsNullOrEmpty(SemanticVersion);
    public string SdkVersionStr => $"{SemanticVersion} {VersionTag}";
    public string PackageVersionStr => GetVersionStr(Release);
    public string SingletonVersionStr => Singleton == default ? "" : GetVersionStr(Singleton);
    private static string GetVersionStr(PackageVersion pv) => $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";
    public string AutomationName => $"{SdkVersionStr} {PackageVersionStr} {SingletonVersionStr}";
}
