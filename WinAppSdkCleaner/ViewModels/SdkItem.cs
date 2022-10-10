using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class SdkItem : ItemBase, IComparable<ItemBase>
{
    private SdkRecord SdkRecord { get; init; }

    public SdkItem(SdkRecord sdkRecord) : base(null)
    {
        IsExpanded = true;
        SdkRecord = sdkRecord;

        foreach (PackageRecord packageRecord in sdkRecord.SdkPackages)
            Children.Add(new SdkPackageItem(packageRecord, this));

        Children.Sort();
    }

    public override string HeadingText
    {
        get
        {
            if (SdkRecord.Version.SemanticVersion.Length == 0)
                return $"{ConvertToString(SdkRecord.SdkId)} package version: {ConvertToString(SdkRecord.Version.Release)}";

            if (SdkRecord.Version.VersionTag.Length > 0)
                return $"{ConvertToString(SdkRecord.SdkId)} {SdkRecord.Version.SemanticVersion} - {SdkRecord.Version.VersionTag}";

            return $"{ConvertToString(SdkRecord.SdkId)} {SdkRecord.Version.SemanticVersion}";
        } 
    }

    public override string ToolTipText => $"Package version: {ConvertToString(SdkRecord.Version.Release)}";

    public VersionRecord Version => SdkRecord.Version;

    private static string ConvertToString(PackageVersion pv)
    {
        return $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";
    }

    private static string ConvertToString(SdkTypes sdkId)
    {
        switch (sdkId)
        {
            case SdkTypes.Reunion: return "Project Reunion";
            case SdkTypes.WinAppSdk: return "Windows App SDK";
            default: throw new ArgumentOutOfRangeException(nameof(sdkId));
        }
    }

    // ignores any children, it's only used to identify this tree node
    public static bool operator ==(SdkItem? x, SdkItem? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if ((x is null) || (y is null))
            return false;

        return (x.SdkRecord.Version.SdkId == y.SdkRecord.Version.SdkId) &&
                x.SdkRecord.Version.Release.Equals(y.SdkRecord.Version.Release);
    }

    public static bool operator !=(SdkItem? x, SdkItem? y) => !(x == y);
    public override int GetHashCode() => SdkRecord.Version.GetHashCode();
    public override bool Equals(object? obj) => this == (obj as SdkItem);

    public new int CompareTo(ItemBase? item)
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

        return result;
    }
}
