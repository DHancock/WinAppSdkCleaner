namespace WinAppSdkCleaner.ViewModels;

internal sealed class PackageVersionComparer : IComparer<PackageVersion>
{
    public int Compare(PackageVersion a, PackageVersion b)
    {
        int result = a.Major - b.Major;

        if (result == 0)
        {
            result = a.Minor - b.Minor;

            if (result == 0)
            {
                result = a.Build - b.Build;

                if (result == 0)
                    result = a.Revision - b.Revision;
            }
        }

        return result;
    }
}