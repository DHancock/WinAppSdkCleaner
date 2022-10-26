namespace WinAppSdkCleaner.Models;

internal sealed record VersionRecord(string SemanticVersion, string VersionTag, SdkId SdkId, PackageVersion Release);
