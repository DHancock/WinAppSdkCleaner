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

        if (result == 0)
        {
            if (string.IsNullOrEmpty(a.SemanticVersion) || string.IsNullOrEmpty(b.SemanticVersion))
            {
                // one or both don't appear in the versions file
                result = PackageVersionComparer(a.Release, b.Release);
            }
            else
            {
                result = SemanticComparer(a.SemanticVersion, b.SemanticVersion);

                if (result == 0)
                {
                    bool aTagIsEmpty = string.IsNullOrEmpty(a.VersionTag);
                    bool bTagIsEmpty = string.IsNullOrEmpty(b.VersionTag);

                    if (aTagIsEmpty) // a is stable release
                    {
                        if (!bTagIsEmpty) // b is either experimental or preview release
                        {
                            result = 1;
                        }
                    }
                    else if (bTagIsEmpty)
                    {
                        result = -1;
                    }
                    else
                    {
                        result = SemanticComparer(a.VersionTag, b.VersionTag);
                    }
                }
            }
        }

        return result;
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
        return PInvoke.StrCmpLogical(a, b);
    }
}