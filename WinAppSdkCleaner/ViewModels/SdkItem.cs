using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class SdkItem : ItemBase
{
    private readonly SdkData sdkData;

    public SdkItem(SdkData sdkRecord) : base(null)
    {
        sdkData = sdkRecord;

        foreach (PackageData packageRecord in sdkRecord.SdkPackages)
        {
            Children.Add(new PackageItem(packageRecord, this));
        }

        Children.Sort();
    }

    public override string HeadingText
    {
        get
        {
            if (string.IsNullOrEmpty(sdkData.Version.SemanticVersion))
            {
                return $"{sdkData.Sdk.DisplayName} ({Version.PackageVersionStr})";
            }

            if (!string.IsNullOrEmpty(sdkData.Version.VersionTag))
            {
                return $"{sdkData.Sdk.DisplayName} {Version.SemanticVersion} - {Version.VersionTag}";
            }

            return $"{sdkData.Sdk.DisplayName} {Version.SemanticVersion}";
        }
    }

    public override int OtherAppsCount => sdkData.OtherAppsCount;
    public override string OtherAppsCountStr => (OtherAppsCount > 0) ? $"+{OtherAppsCount}" : string.Empty;
    public override string ToolTipText => Version.PackageVersionStr;
    public SdkId SdkIdentifier => sdkData.Sdk.Id;

    private VersionRecord Version => sdkData.Version;

    // ignores any children, it's only used to identify this tree node
    public static bool operator ==(SdkItem? x, SdkItem? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if ((x is null) || (y is null))
        {
            return false;
        }

        return (x.Version.SdkId == y.Version.SdkId) &&
                x.Version.Release.Equals(y.Version.Release);
    }

    public static bool operator !=(SdkItem? x, SdkItem? y) => !(x == y);
    public override int GetHashCode() => Version.GetHashCode();
    public override bool Equals(object? obj) => this == (obj as SdkItem);

    public override int CompareTo(ItemBase? item)
    {
        if (item is not SdkItem other)
        {
            return -1;
        }

        int result = Version.SdkId - other.Version.SdkId;

        if (result == 0)
        {
            result = new PackageVersionComparer().Compare(Version.Release, other.Version.Release);
        }

        return result;
    }
}
