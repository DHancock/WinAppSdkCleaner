namespace WinAppSdkCleaner.Models;

public enum SdkTypes : int { Reunion, WinAppSdk };

// The Description field is now only used by version 1.0.0.0
// and is kept for versions.json backwards compatibility. 

internal sealed record VersionRecord(string Description, string SemanticVersion, string VersionTag, SdkTypes SdkId, PackageVersion Release);
