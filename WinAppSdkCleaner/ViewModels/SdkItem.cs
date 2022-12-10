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
            Children.Add(new PackageItem(packageRecord, this));

        Children.Sort();
    }

    public override string HeadingText
    {
        get
        {
            if (SdkRecord.Version.SemanticVersion.Length == 0)
                return $"{SdkRecord.Sdk.DispalyName} package version: {ConvertToString(Version.Release)}";

            if (SdkRecord.Version.VersionTag.Length > 0)
                return $"{SdkRecord.Sdk.DispalyName} {Version.SemanticVersion} - {Version.VersionTag}";

            return $"{SdkRecord.Sdk.DispalyName} {Version.SemanticVersion}";
        }
    }

    public override string OtherAppsCount => $"+{SdkRecord.OtherAppsCount}";

    public override Visibility OtherAppsCountVisibity => SdkRecord.OtherAppsCount > 0 ? Visibility.Visible : Visibility.Collapsed;

    public override Visibility LogoVisibity => Visibility.Collapsed;

    public override ImageSource? Logo => null;

    public override FontWeight HeadingFontWeight => FontWeights.Bold;

    public override string ToolTipText => $"Package version: {ConvertToString(Version.Release)}";

    private VersionRecord Version => SdkRecord.Version;

    private static string ConvertToString(PackageVersion pv)
    {
        return $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";
    }

    // ignores any children, it's only used to identify this tree node
    public static bool operator ==(SdkItem? x, SdkItem? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if ((x is null) || (y is null))
            return false;

        return (x.Version.SdkId == y.Version.SdkId) &&
                x.Version.Release.Equals(y.Version.Release);
    }

    public static bool operator !=(SdkItem? x, SdkItem? y) => !(x == y);
    public override int GetHashCode() => Version.GetHashCode();
    public override bool Equals(object? obj) => this == (obj as SdkItem);

    public override int CompareTo(ItemBase? item)
    {
        if (item is not SdkItem other)
            return -1;

        int result = Version.SdkId - other.Version.SdkId;

        if (result == 0)
        {
            if ((Version.SemanticVersion.Length > 0) && (other.Version.SemanticVersion.Length > 0))
                result = string.Compare(Version.SemanticVersion, other.Version.SemanticVersion);
            else
            {
                result = other.Version.SemanticVersion.Length - Version.SemanticVersion.Length;

                if (result == 0) // they are both uncategorized
                    result = string.Compare(HeadingText, other.HeadingText);
            }

            if (result == 0)
            {
                if ((Version.VersionTag.Length > 0) && (other.Version.VersionTag.Length > 0))
                    result = string.Compare(Version.VersionTag, other.Version.VersionTag);
                else
                    result = other.Version.VersionTag.Length - Version.VersionTag.Length;
            }
        }

        // it would have been simpler if the version record had a "rank" field... 
        return result;
    }
}
