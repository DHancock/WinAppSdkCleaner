using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class SdkPackageItem : PackageItemBase
{
    public SdkPackageItem(PackageRecord packageRecord, SdkItem parent) : base(packageRecord, parent)
    {
        if (packageRecord.PackagesDependentOnThis.Count > 0)
            Children.Add(new DependentsItem(packageRecord, this));
    }

    public override string ToolTipText => Package.Id.FullName;
}