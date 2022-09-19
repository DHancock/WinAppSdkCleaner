using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class PackageItem : PackageItemBase
{
    private PackageItem(PackageRecord packageRecord, ItemBase parent) : base(packageRecord.Package, parent)
    {
        foreach (PackageRecord dependentPackage in packageRecord.PackagesDependentOnThis)
            Children.Add(new PackageItem(dependentPackage, this));
    }

    public PackageItem(PackageRecord packageRecord, PackageItem parent) : this(packageRecord, (ItemBase)parent)
    {
    }

    public PackageItem(PackageRecord packageRecord, DependentsItem parent) : this(packageRecord, (ItemBase)parent)
    {
    }
}

