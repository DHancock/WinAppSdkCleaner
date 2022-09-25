using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class SdkItem : ItemBase
{
    private readonly VersionRecord version;

    public SdkItem(SdkRecord packageInfo) : base(null)
    {
        IsExpanded = true;
        version = packageInfo.Version;

        foreach (PackageRecord packageRecord in packageInfo.SdkPackages)
            Children.Add(new SdkPackageItem(packageRecord, this));

        Children.Sort((x, y) => string.Compare(x.HeadingText, y.HeadingText, StringComparison.CurrentCulture));
    }

    public override string HeadingText => $"WinAppSdk {version.Description}";
    public override string ToolTipText => Model.ConvertToString(version.Release);

    // ignores any children, it's only used to identify this tree node
    public static bool operator ==(SdkItem? x, SdkItem? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if ((x is null) || (y is null))
            return false;

        return x.version.Release.Equals(y.version.Release);
    }

    public static bool operator !=(SdkItem? x, SdkItem? y) => !(x == y);
    public override int GetHashCode() => version.GetHashCode();
    public override bool Equals(object? obj) => this == (obj as SdkItem);
}
