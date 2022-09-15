using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class SdkList : ObservableCollection<ItemBase>
{
    public SdkList()
    {
    }

    public SdkList(IList<SdkRecord> sdks) : base()
    {
        foreach (SdkRecord sdk in sdks)
            Add(new SdkItem(sdk));
    }

    private static ItemBase? GetSelectedItem(ObservableCollection<ItemBase> items)
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

    public List<Package> GetDistinctSelectedPackages()
    {
        static List<Package> GetSelectedPackages(ItemBase? item)
        {
            List<Package> packages = new List<Package>();

            if (item is not null)
            {
                if (item is PackageItemBase packageItem)
                    packages.Add(packageItem.Package);

                foreach (ItemBase child in item.Children)
                    packages.AddRange(GetSelectedPackages(child));
            }

            return packages;
        }

        return GetSelectedPackages(GetSelectedItem(this)).DistinctBy(p => p.Id.FullName).ToList();
    }

    private static bool ItemContainsPackage(ItemBase item, Package package)
    {
        if (item is PackageItemBase packageItem)
            return string.Equals(packageItem.Package.Id.FullName, package.Id.FullName, StringComparison.Ordinal);

        return false;
    }

    private static List<ItemBase> FindAllItems(ObservableCollection<ItemBase> items, Package package)
    {
        List<ItemBase> foundItems = new List<ItemBase>();

        foreach (ItemBase item in items)
        {
            if (ItemContainsPackage(item, package))
                foundItems.Add(item);

            foundItems.AddRange(FindAllItems(item.Children, package));
        }

        return foundItems;
    }

    private void DeleteItem(ItemBase item)
    {
        ItemBase? parent = item.Parent;

        if (parent is not null) 
        {
            parent.Children.Remove(item);

            if (parent.Children.Count == 0) // remove if it's an empty container
            {
                if (parent is DependentsItem dependentsItem)  
                {
                    parent = parent.Parent;
                    Debug.Assert(parent is not null);
                    parent.Children.Remove(dependentsItem);
                }
                else if (parent is SdkItem)
                {
                    this.Remove(parent); 
                }
            }
        }
    }

    public void Delete(List<Package> packages)
    {
        foreach (Package package in packages)
        {
            foreach(ItemBase item in FindAllItems(this, package))
            {
                DeleteItem(item);
            }
        }
    }

    public bool CanRemove() => GetSelectedItem(this) is not null;


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

    private static ItemBase? FindItem(ObservableCollection<ItemBase> list, ItemBase other)
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

    public void RestoreState(ObservableCollection<ItemBase> otherList)
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