using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class SdkList : List<ItemBase>
{
    public SdkList()
    {
    }

    public SdkList(List<SdkRecord> sdks) : base()
    {
        foreach (SdkRecord sdk in sdks)
            Add(new SdkItem(sdk));
    }

    private static ItemBase? GetSelectedItem(List<ItemBase> items)
    {
        foreach (ItemBase item in items)
        {
            if (item.IsSelected)
                return item;

            ItemBase? selected = GetSelectedItem(item.Children);

            if (selected is not null)
                return selected;
        }

        return null;
    }

    public List<PackageRecord> GetDistinctSelectedPackages()
    {
        static List<PackageRecord> GetSelectedPackages(ItemBase? item)
        {
            List<PackageRecord> packages = new List<PackageRecord>();

            if (item is not null)
            {
                if (item is PackageItemBase packageItem)
                    packages.Add(packageItem.PackageRecord);

                foreach (ItemBase child in item.Children)
                    packages.AddRange(GetSelectedPackages(child));
            }

            return packages;
        }

        return GetSelectedPackages(GetSelectedItem(this)).DistinctBy(p => p.Package.Id.FullName).ToList();
    }

    public bool CanRemove() => GetSelectedItem(this) is SdkItem;

    private void GetCopyData(ItemBase item, int indent, StringBuilder sb)
    {
        if (indent > 0)
            sb.Append(new string(' ', indent * 4));

        if (item is PackageItemBase packageItem)
            sb.AppendLine(packageItem.PackageFullName);
        else
            sb.AppendLine(item.HeadingText);

        foreach (ItemBase child in item.Children)
            GetCopyData(child, indent + 1, sb);
    }

    public string GetCopyData()
    {
        StringBuilder sb = new StringBuilder();
        ItemBase? selectedItem = GetSelectedItem(this);

        if (selectedItem is not null)
            GetCopyData(selectedItem, 0, sb);

        return sb.ToString();
    }

    public bool CanCopy() => GetSelectedItem(this) is not null;

    private static ItemBase? FindItem(List<ItemBase> list, ItemBase other)
    {
        foreach (ItemBase item in list)
        {
            if (item.Equals(other))
                return item;

            ItemBase? foundItem = FindItem(item.Children, other);

            if (foundItem is not null)
                return foundItem;
        }

        return null;
    }

    public void RestoreState(List<ItemBase> otherList)
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
}