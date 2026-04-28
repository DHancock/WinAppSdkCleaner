using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class SdkItem : ItemBase
{
    private readonly SdkData sdkData;

    public SdkItem(SdkData sdkData) : base(null)
    {
        this.sdkData = sdkData;

        foreach (PackageData packageData in sdkData.FrameworkPackages)
        {
            Children.Add(new PackageItem(packageData, this));
        }

        Children.Sort();
    }

    public override string HeadingText
    {
        get
        {
            if (string.IsNullOrEmpty(sdkData.Version.SemanticVersion)) // this sdk isn't in the versions file
            {
                return $"{sdkData.Sdk.DisplayName} {sdkData.Version.PackageVersionStr}";
            }

            string heading = $"{sdkData.Sdk.DisplayName} {sdkData.Version.SemanticVersion}";

            if (!string.IsNullOrEmpty(sdkData.Version.VersionTag))
            {
                heading += $" - {sdkData.Version.VersionTag}";
            }

            return heading;
        }
    }

    public override FontWeight HeadingFontWeight => FontWeights.SemiBold;
    public override int OtherAppsCount => sdkData.OtherAppsCount;
    public override string OtherAppsCountStr => (OtherAppsCount > 0) ? $"+{OtherAppsCount}" : string.Empty;

    public override List<(string property, string value)> Info
    {
        get
        {
            List<(string property, string value)> info = new();

            info.Add(("Title", HeadingText));
            info.Add(("Version", sdkData.Version.PackageVersionStr));

            return info;
        }
    }

    public override ImageSource? Logo => null;

    public SdkId SdkIdentifier => sdkData.Sdk.Id;

    public override int CompareTo(ItemBase? item)
    {
        Debug.Assert(item is not null);
        Debug.Assert(item is SdkItem);

        int result = VersionRecordComparer.Comparer(sdkData.Version, ((SdkItem)item).sdkData.Version);

        if (!Settings.Instance.SortAscending)
        {
            result *= -1; 
        }

        return result;
    }
}
