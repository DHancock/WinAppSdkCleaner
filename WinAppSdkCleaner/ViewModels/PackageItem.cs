using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class PackageItem : ItemBase
{
    private readonly BitmapImage cachedLogo;
    private readonly PackageData packageData;

    public PackageItem(PackageData packageData, ItemBase parent) : base(parent)
    {
        this.packageData = packageData;

        cachedLogo = new BitmapImage();
        LoadPackageLogo();

        foreach (PackageData dependentPackage in packageData.Dependents)
        {
            Children.Add(new PackageItem(dependentPackage, this));
        }

        Children.Sort();
    }

    public Package Package => packageData.Package;

    public override string HeadingText
    {
        get
        {
            if (field == default)
            {
                field = IsNonSdkPackage ? Package.DisplayName : GetSdkPackageDisplayName();
            }

            return field;
        }
    }

    private string GetSdkPackageDisplayName()
    {
        SdkItem? sdkItem = Parent as SdkItem;

        if ((sdkItem is null) && (Parent is PackageItem package))
        {
            // it's dependent on a different sdk version
            sdkItem = package.Parent as SdkItem;
        }

        Debug.Assert(sdkItem is not null);

        if (sdkItem is not null)
        {
            string text;
            bool isSingleton = false;

            if (Package.Id.FullName.Contains("Main"))
            {
                text = "Main";
            }
            else if (Package.Id.FullName.Contains("DDLM"))
            {
                text = "DDLM";
            }
            else if (Package.Id.FullName.Contains("Singleton"))
            {
                text = "Singleton";
                isSingleton = true;
            }
            else
            {
                Debug.Assert(Package.IsFramework);
                text = "Framework";
            }

            VersionRecord vr = Model.CategorizePackageVersion(sdkItem.SdkIdentifier, Package.Id.Version, isSingleton);

            if (vr.IsSynthesized) // there isn't an corresponding entry in the versions file
            {
                text += $" {vr.PackageVersionStr}";
            }
            else
            {
                text += $" {vr.SemanticVersion}";
            }

            if (!string.IsNullOrEmpty(vr.VersionTag))
            {
                text += $" {vr.VersionTag}";
            }

            return text + $" - {Package.Id.Architecture.ToString().ToLower()}";
        }

        return Package.DisplayName; 
    }

    public override List<(string property, string value)> Info
    {
        get
        {
            List<(string property, string value)> info = new(8);

            info.Add(("Title", HeadingText));
            info.Add(("Description", GetInfo(Package, p => p.Description)));
            info.Add(("Full Name", GetInfo(Package, p => p.Id.FullName)));
            info.Add(("Publisher", GetInfo(Package, p => p.PublisherDisplayName)));
            info.Add(("Installed Path", GetInfo(Package, p => p.InstalledPath)));
            info.Add(("Path Exists", GetInfo(Package, p => Directory.Exists(p.InstalledPath).ToString())));
            info.Add(("Installed Date", GetInfo(Package, p => p.InstalledDate.ToString("g"))));
            info.Add(("Version", GetInfo(Package, p => $"{p.Id.Version.Major}.{p.Id.Version.Minor}.{p.Id.Version.Build}.{p.Id.Version.Revision}")));

            return info;

            static string GetInfo(Package package, Func<Package, string> dataProvider)
            {
                try
                {
                    string value = dataProvider(package);
                    return string.IsNullOrWhiteSpace(value) ? "<empty>" : value;
                }
                catch (Exception ex)
                {
                    return $"<threw {ex.GetType().Name}>";
                }
            }
        }
    }

    public override int OtherAppsCount => packageData.OtherAppsCount;

    public override string OtherAppsCountStr => (!IsNonSdkPackage && (packageData.OtherAppsCount > 0)) ? $"+{OtherAppsCount}" : string.Empty;

    public bool IsNonSdkPackage => (Children.Count == 0) && (packageData.OtherAppsCount == 1);

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
            await using (FileStream fs = File.OpenRead(path))
            {
                await cachedLogo.SetSourceAsync(fs.AsRandomAccessStream());
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
            return IsNonSdkPackage ? cAfter : cBefore;
        }

        int result = string.Compare(HeadingText, other.HeadingText, StringComparison.CurrentCulture);

        if (result == 0)
        {
            result = string.Compare(Package.Id.FullName, other.Package.Id.FullName, StringComparison.Ordinal);
        }

        return result;
    }
}

