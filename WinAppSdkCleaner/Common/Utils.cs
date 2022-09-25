using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.Common;

internal static class Utils
{
    public static string ConvertToString(PackageVersion pv)
    {
        return $"Build: {pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";
    }
}

