using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class SdkItem : ItemBase
{
    private SdkData SdkRecord { get; init; }

    public SdkItem(SdkData sdkRecord) : base(null)
    {
        IsExpanded = true;
        SdkRecord = sdkRecord;

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
            if (string.IsNullOrEmpty(SdkRecord.Version.SemanticVersion))
            {
                return $"{SdkRecord.Sdk.DispalyName} ({Version.PackageVersionStr})";
            }

            if (!string.IsNullOrEmpty(SdkRecord.Version.VersionTag))
            {
                return $"{SdkRecord.Sdk.DispalyName} {Version.SemanticVersion} - {Version.VersionTag}";
            }

            return $"{SdkRecord.Sdk.DispalyName} {Version.SemanticVersion}";
        }
    }

    public override string OtherAppsCount => $"+{SdkRecord.OtherAppsCount}";

    public bool HasOtherApps => SdkRecord.OtherAppsCount > 0;

    public override Visibility OtherAppsCountVisibility => HasOtherApps ? Visibility.Visible : Visibility.Collapsed;

    public override Visibility LogoVisibility => Visibility.Collapsed;

    public override ImageSource? Logo => null;

    public override FontWeight HeadingFontWeight => FontWeights.Bold;

    public override string ToolTipText => Version.PackageVersionStr;

    public SdkId SdkIdentifier => SdkRecord.Sdk.Id;

    private VersionRecord Version => SdkRecord.Version;

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
