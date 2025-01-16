using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed partial class SdkList : List<ItemBase>
{
    public SdkList()
    {
    }

    public SdkList(IEnumerable<SdkData> sdks) : base()
    {
        foreach (SdkData sdk in sdks)
        {
            Add(new SdkItem(sdk));
        }

        Sort();
    }

    private static ItemBase? GetSelectedItem(IEnumerable<ItemBase> items)
    {
        foreach (ItemBase item in items)
        {
            if (item.IsSelected)
            {
                return item;
            }

            ItemBase? selected = GetSelectedItem(item.Children);

            if (selected is not null)
            {
                return selected;
            }
        }

        return null;
    }

    public IEnumerable<PackageData> GetDistinctSelectedPackages()
    {
        static List<PackageData> GetSelectedPackages(ItemBase? item)
        {
            List<PackageData> packages = new List<PackageData>();

            if (item is not null)
            {
                if (item is PackageItem packageItem)
                {
                    packages.Add(packageItem.PackageRecord);
                }

                foreach (ItemBase child in item.Children)
                {
                    packages.AddRange(GetSelectedPackages(child));
                }
            }

            return packages;
        }

        return GetSelectedPackages(GetSelectedItem(this)).DistinctBy(p => p.Package.Id.FullName);
    }

    public bool CanRemove() => GetSelectedItem(this) is SdkItem;

    public string GetCopyData()
    {
        IEnumerable<PackageData> allPackages = GetDistinctSelectedPackages();
        IEnumerable<string> frameworks = allPackages.Where(p => p.Package.IsFramework).Select(p => p.Package.Id.FullName);
        IEnumerable<string> others = allPackages.Where(p => !p.Package.IsFramework).Select(p => p.Package.Id.FullName);

        StringBuilder sb = new StringBuilder();

        if (frameworks.Any())
        {
            sb.AppendLine("Framework packages:");
            sb.AppendJoin(Environment.NewLine, frameworks);
            sb.Append(Environment.NewLine);
        }

        if (others.Any())
        {
            if (frameworks.Any())
            {
                sb.Append(Environment.NewLine);
                sb.AppendLine("Dependent packages:");
            }

            sb.AppendJoin(Environment.NewLine, others);
        }

        return sb.ToString();
    }

    public bool CanCopy() => GetSelectedItem(this) is not null;

    private static ItemBase? FindItem(IEnumerable<ItemBase> list, ItemBase other)
    {
        foreach (ItemBase item in list)
        {
            if (item.Equals(other))
            {
                return item;
            }

            ItemBase? foundItem = FindItem(item.Children, other);

            if (foundItem is not null)
            {
                return foundItem;
            }
        }

        return null;
    }

    public void RestoreState(IEnumerable<ItemBase> otherList)
    {
        foreach (ItemBase otherItem in otherList)
        {
            ItemBase? thisItem = FindItem(this, otherItem);

            if (thisItem is not null)
            {
                thisItem.IsSelected = otherItem.IsSelected;
                thisItem.IsExpanded = otherItem.IsExpanded;
                thisItem.IsEnabled = otherItem.IsEnabled;
            }

            RestoreState(otherItem.Children);
        }
    }

    public bool SelectedSdkHasDependentApps => (GetSelectedItem(this) is SdkItem item) && item.HasOtherApps;
}
