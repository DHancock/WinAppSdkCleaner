namespace WinAppSdkCleaner.Models;

[DebuggerDisplay("{SdkId} {SemanticVersion, nq} {VersionTag, nq} {PackageVersionStr, nq} {SingletonVersionStr, nq}")]
internal sealed record VersionRecord(string SemanticVersion, string VersionTag, SdkId SdkId, PackageVersion Release, PackageVersion Singleton)
{
    [JsonIgnore]
    public bool IsSynthesized => string.IsNullOrEmpty(SemanticVersion);  // this sdk isn't in the versions file, it's generated from an installed framework package

    [JsonIgnore]
    public string SdkVersionStr => $"{SemanticVersion} {VersionTag}";

    [JsonIgnore]
    public string PackageVersionStr => GetVersionStr(Release);

    [JsonIgnore]
    public string SingletonVersionStr => Singleton == default ? "" : GetVersionStr(Singleton);

    [JsonIgnore]
    public string AutomationName => $"{SdkVersionStr} {PackageVersionStr} {SingletonVersionStr}";

    private static string GetVersionStr(PackageVersion pv)
    {
        StringBuilder sb = new(32);
        sb.Append(pv.Major);
        sb.Append('.');
        sb.Append(pv.Minor);
        sb.Append('.');
        sb.Append(pv.Build);

        if (pv.Revision != 0)
        {
            sb.Append('.');
            sb.Append(pv.Revision);
        }

        return sb.ToString();
    }
}
