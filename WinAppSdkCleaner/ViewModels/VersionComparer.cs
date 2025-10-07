using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class VersionComparer : IComparer<VersionRecord>
{
    public int Compare(VersionRecord? a, VersionRecord? b)
    {
        Debug.Assert(a is not null);
        Debug.Assert(b is not null);

        return Comparer(a, b);
    }

    public static int Comparer(VersionRecord a, VersionRecord b)
    {
        int result = a.SdkId - b.SdkId;

        if (result != 0)
        {
            return result;
        }

        if (string.IsNullOrEmpty(a.SemanticVersion) || string.IsNullOrEmpty(b.SemanticVersion) || a.SemanticVersion.Equals(b.SemanticVersion))
        {
            return PackageVersionComparer(a.Release, b.Release);
        }

        return SemanticComparer(a.SemanticVersion, b.SemanticVersion);
    }


    private static int PackageVersionComparer(PackageVersion a, PackageVersion b)
    {
        int result = a.Major - b.Major;

        if (result == 0)
        {
            result = a.Minor - b.Minor;

            if (result == 0)
            {
                result = a.Build - b.Build;

                if (result == 0)
                {
                    result = a.Revision - b.Revision;
                }
            }
        }

        return result;
    }

    private static int SemanticComparer(string a, string b)
    {
        string[] splitA = a.Split('.');
        string[] splitB = b.Split('.');

        Debug.Assert(splitA.Length == 3);
        Debug.Assert(splitB.Length == 3);

        int result = Convert.ToInt32(splitA[0]) - Convert.ToInt32(splitB[0]);

        if (result == 0)
        {
            result = Convert.ToInt32(splitA[1]) - Convert.ToInt32(splitB[1]);

            if (result == 0)
            {
                result = Convert.ToInt32(splitA[2]) - Convert.ToInt32(splitB[2]);
            }
        }

        return result;
    }
}