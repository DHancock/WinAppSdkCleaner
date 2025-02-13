using WinAppSdkCleaner.Models;
using WinAppSdkCleaner.Utils;

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

    public static IEnumerable<PackageData> GetDistinctPackages(ItemBase item)
    {
        static List<PackageData> GetPackages(ItemBase? item)
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
                    packages.AddRange(GetPackages(child));
                }
            }

            return packages;
        }

        return GetPackages(item).DistinctBy(p => p.Package.Id.FullName);
    }

    public static string GetCopyData(ItemBase item)
    {
        StringBuilder sb = new StringBuilder();

        EnumerateTree (item, sb, 0);

        sb.Append(Environment.NewLine);

        IEnumerable<PackageData> allPackages = GetDistinctPackages(item);
        IEnumerable<string> frameworks = allPackages.Where(p => p.Package.IsFramework).Select(p => BuildPS(p));
        IEnumerable<string> others = allPackages.Where(p => !p.Package.IsFramework).Select(p => BuildPS(p));

        if (others.Any())
        {
            sb.AppendLine("# dependent packages:");
            sb.AppendJoin(Environment.NewLine, others);
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
        }

        if (frameworks.Any())
        {
            sb.AppendLine("# framework packages:");
            sb.AppendJoin(Environment.NewLine, frameworks);
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
        }

        return sb.ToString();

        static void EnumerateTree(ItemBase item, StringBuilder sb, int depth)
        {
            sb.AppendLine($"{new string('\t', depth)}{item.HeadingText}");

            foreach (ItemBase child in item.Children)
            {
                EnumerateTree(child, sb, depth + 1);
            }
        }

        static string BuildPS(PackageData p)
        {
            string allUsers = IntegrityLevel.IsElevated ? " -AllUsers" : string.Empty;
            return $"Remove-AppxPackage -Package '{p.Package.Id.FullName}'{allUsers} -Verbose # {p.Package.DisplayName}";
        }
    }
}
