using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.Utilities;

internal static class Utils
{
    public static int VersionRecordComparer(VersionRecord x, VersionRecord y)
    {
        return PackageVersionComparer(x.Release, y.Release);
    }

    public static int PackageVersionComparer(PackageVersion x, PackageVersion y)
    {
        int result = x.Major - y.Major;

        if (result == 0)
        {
            result = x.Minor - y.Minor;

            if (result == 0)
            {
                result = x.Build - y.Build;

                if (result == 0)
                    result = x.Revision - y.Revision;
            }
        }

        return result;
    }

    public static string ConvertToString(PackageVersion pv)
    {
        return $"Build: {pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";
    }
}

