namespace WinAppSdkCleaner.Models;

// the Description field is only used by WinAppSdkCleaner version 1.0.0 
// it remains in the versions.json file for backwards compatibility

internal sealed record VersionRecord(string SemanticVersion, string VersionTag, SdkId SdkId, PackageVersion Release);
