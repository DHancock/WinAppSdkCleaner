using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class SdkItem : ItemBase
{
    private SdkRecord SdkRecord { get; init; }

    public SdkItem(SdkRecord sdkRecord) : base(null)
    {
        IsExpanded = true;
        SdkRecord = sdkRecord;

        foreach (PackageRecord packageRecord in sdkRecord.SdkPackages)
            Children.Add(new SdkPackageItem(packageRecord, this));

        Children.Sort((x, y) => string.Compare(x.HeadingText, y.HeadingText, StringComparison.CurrentCulture));
    }

    public override string HeadingText
    {
        get
        {
            if (SdkRecord.Version.SemanticVersion.Length == 0)
                return $"{SdkRecord.SdkId} package version: {ConvertToString(SdkRecord.Version.Release)}";

            if (SdkRecord.Version.VersionTag.Length > 0)
                return $"{SdkRecord.SdkId} {SdkRecord.Version.SemanticVersion} - {SdkRecord.Version.VersionTag}";

            return $"{SdkRecord.SdkId} {SdkRecord.Version.SemanticVersion}";
        } 
    }

    public override string ToolTipText => $"Build: {ConvertToString(SdkRecord.Version.Release)}";

    public VersionRecord Version => SdkRecord.Version;

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

        return x.SdkRecord.Version.Release.Equals(y.SdkRecord.Version.Release);
    }

    public static bool operator !=(SdkItem? x, SdkItem? y) => !(x == y);
    public override int GetHashCode() => SdkRecord.Version.GetHashCode();
    public override bool Equals(object? obj) => this == (obj as SdkItem);
}
