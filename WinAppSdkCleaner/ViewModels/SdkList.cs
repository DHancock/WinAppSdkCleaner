using WinAppSdkCleaner.Models;
using WinAppSdkCleaner.Utilites;

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

    public static IEnumerable<Package> GetDistinctPackages(ItemBase item)
    {
        return GetPackages(item).DistinctBy(p => p.Id.FullName);

        static List<Package> GetPackages(ItemBase item)
        {
            List<Package> packages = new List<Package>();

            if (item is PackageItem packageItem)
            {
                packages.Add(packageItem.Package);
            }

            foreach (ItemBase child in item.Children)
            {
                packages.AddRange(GetPackages(child));
            }

            return packages;
        }
    }

    public static string GetCopyData(ItemBase item)
    {
        StringBuilder sb = new StringBuilder();

        IEnumerable<Package> packages = GetDistinctPackages(item);
        IEnumerable<string> frameworks = packages.Where(p => p.IsFramework).Select(p => BuildPS(p));
        IEnumerable<string> others = packages.Where(p => !p.IsFramework).Select(p => BuildPS(p));

        if (others.Any())
        {
            sb.AppendJoin(Environment.NewLine, others);
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
        }

        if (frameworks.Any())
        {
            sb.AppendJoin(Environment.NewLine, frameworks);
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
        }

        return sb.ToString();

        static string BuildPS(Package p)
        {
            string allUsers = IntegrityLevel.IsElevated ? " -AllUsers" : string.Empty;
            return $"Remove-AppxPackage -Package '{p.Id.FullName}'{allUsers} -Verbose # {p.DisplayName}";
        }
    }
}
