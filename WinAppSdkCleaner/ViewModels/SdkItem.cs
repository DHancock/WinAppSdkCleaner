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

    public override FontWeight HeadingFontWeight => FontWeights.SemiBold;
    public override int OtherAppsCount => sdkData.OtherAppsCount;
    public override string OtherAppsCountStr => (OtherAppsCount > 0) ? $"+{OtherAppsCount}" : string.Empty;
    public override string ToolTipText => Version.PackageVersionStr;
    public override ImageSource? Logo => null;

    public SdkId SdkIdentifier => sdkData.Sdk.Id;
    private VersionRecord Version => sdkData.Version;

    

    public override int CompareTo(ItemBase? item)
    {
        Debug.Assert(item is not null);
        Debug.Assert(item is SdkItem);

        int result = VersionComparer.Comparer(Version, ((SdkItem)item).Version);

        if (!Settings.Instance.SortAscending)
        {
            result *= -1; 
        }

        return result;
    }
}
