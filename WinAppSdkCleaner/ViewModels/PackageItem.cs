using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class PackageItem : ItemBase
{
    private readonly BitmapImage cachedLogo;
    public PackageData PackageRecord { get; init; }

    public PackageItem(PackageData packageRecord, ItemBase parent) : base(parent)
    {
        PackageRecord = packageRecord;

        cachedLogo = new BitmapImage();
        LoadPackageLogo();

        foreach (PackageData dependentPackage in packageRecord.Dependents)
        {
            Children.Add(new PackageItem(dependentPackage, this));
        }

        Children.Sort();
    }

    public Package Package => PackageRecord.Package;

    public override string HeadingText => IsNonSdkPackage ? Package.DisplayName : GetSdkPackageDisplayName();

    private string GetSdkPackageDisplayName()
    {
        SdkItem? sdkItem = Parent as SdkItem;

        if ((sdkItem is null) && (Parent is PackageItem package))
        {
            // it's dependent on a different sdk version
            sdkItem = package.Parent as SdkItem;
        }

        Debug.Assert(sdkItem is not null);

        if ((sdkItem is not null) && (sdkItem.SdkIdentifier == SdkId.WinAppSdk))
        {
            string text;

            if (Package.Id.FullName.Contains("Main"))
            {
                text = "Main ";
            }
            else if (Package.Id.FullName.Contains("DDLM"))
            {
                text = "DDLM ";
            }
            else if (Package.Id.FullName.Contains("Singleton"))
            {
                text = "Singleton ";
            }
            else
            {
                Debug.Assert(Package.IsFramework);
                text = "Framework ";
            }

            VersionRecord vr = Model.CategorizePackageVersion(SdkId.WinAppSdk, Package.Id.Version);
            text += string.IsNullOrEmpty(vr.SemanticVersion) ? $"({vr.PackageVersionStr})" : vr.SemanticVersion;

            return text + $" - {Package.Id.Architecture.ToString().ToLower()}";
        }

        return Package.DisplayName;  // default for reunion packages
    }

    private enum Names { Title, Description, FullName, Publisher, InstalledPath, PathExists, InstalledDate, Version }

    public override List<(string property, string value)> Info
    {
        get
        {
            List<(string property, string value)> info = new();

            info.Add((Names.Title.ToString(), HeadingText));
            info.Add(GetInfo(Names.Description, Package));
            info.Add(GetInfo(Names.FullName, Package));
            info.Add(GetInfo(Names.Publisher, Package));
            info.Add(GetInfo(Names.InstalledPath, Package));
            info.Add(GetInfo(Names.PathExists, Package));
            info.Add(GetInfo(Names.InstalledDate, Package));
            info.Add(GetInfo(Names.Version, Package));

            return info;

            static (string, string) GetInfo(Names name, Package package)
            {
                string nameStr = name.ToString();

                try
                {
                    switch (name)
                    {
                        case Names.Description: return (nameStr, GetValue(package.Description));
                        case Names.FullName: return (nameStr, GetValue(package.Id.FullName));
                        case Names.Publisher: return (nameStr, GetValue(package.PublisherDisplayName));
                        case Names.InstalledPath: return (nameStr, GetValue(package.InstalledPath));
                        case Names.PathExists: return (nameStr, Directory.Exists(package.InstalledPath).ToString());
                        case Names.InstalledDate: return (nameStr, package.InstalledDate.ToString("g"));
                        case Names.Version: return (nameStr, $"{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}");
                    }
                }
                catch (Exception ex)
                {
                    return (nameStr, $"<threw {ex.GetType().Name}>");
                }

                return (nameStr, "<error>");
            }

            static string GetValue(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return "<empty>";
                }

                return value;
            }
        }
    }

    public override int OtherAppsCount => PackageRecord.OtherAppsCount;

    public override string OtherAppsCountStr => (!IsNonSdkPackage && (PackageRecord.OtherAppsCount > 0)) ? $"+{OtherAppsCount}" : string.Empty;

    public override FontWeight HeadingFontWeight
    {
        get => IsNonSdkPackage ? FontWeights.SemiBold : FontWeights.Normal;
    }

    public bool IsNonSdkPackage => (Children.Count == 0) && (PackageRecord.OtherAppsCount == 1);

    public override BitmapImage? Logo => cachedLogo;

    private async void LoadPackageLogo()
    {
        string path;

        try
        {
            path = Package.Logo.LocalPath;
        }
        catch (ArgumentException) // expected for VS deployed packages that have been orphaned
        {
            path = Path.Join(AppContext.BaseDirectory, "Resources//missing.png");
        }
        catch
        {
            return;
        }

        try
        {
            await using (Stream s = File.OpenRead(path))
            {
                await cachedLogo.SetSourceAsync(s.AsRandomAccessStream());
            }
        }
        catch
        {
        }
    }

    public override int CompareTo(ItemBase? item)
    {
        const int cBefore = -1;
        const int cAfter = 1;

        Debug.Assert(item is not null);
        Debug.Assert(item is PackageItem);

        PackageItem other = (PackageItem)item;

        if (IsNonSdkPackage != other.IsNonSdkPackage)
        {
            return IsNonSdkPackage ? cBefore : cAfter;
        }

        int result = string.Compare(HeadingText, other.HeadingText, StringComparison.CurrentCulture);

        if (result == 0)
        {
            result = string.Compare(Package.Id.FullName, other.Package.Id.FullName, StringComparison.Ordinal);
        }

        return result;
    }
}

