using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class PackageItem : ItemBase
{
    private readonly ImageSource cachedLogo;
    public PackageData PackageRecord { get; init; }

    public PackageItem(PackageData packageRecord, ItemBase parent) : base(parent)
    {
        PackageRecord = packageRecord;
        cachedLogo = LoadPackageLogo();

        foreach (PackageData dependentPackage in packageRecord.PackagesDependentOnThis)
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

            VersionRecord vr = Model.CategorizePackageVersion(sdkItem.SdkIdentifier, Package.Id.Version);
            text += string.IsNullOrEmpty(vr.SemanticVersion) ? $"({vr.PackageVersionStr})" : vr.SemanticVersion;

            return text + $" - {Package.Id.Architecture.ToString().ToLower()}";
        }

        return Package.DisplayName;  // default for reunion packages
    }

    public override string ToolTipText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Package.Description))
            {
                return Package.Id.FullName;
            }

            return Package.Description;
        }
    }

    public override int OtherAppsCount => PackageRecord.OtherAppsCount;
    public override string OtherAppsCountStr => (!IsNonSdkPackage && (PackageRecord.OtherAppsCount > 0)) ? $"+{OtherAppsCount}" : string.Empty;

    public FontWeight HeadingFontWeight
    {
        get => IsNonSdkPackage ? FontWeights.SemiBold : FontWeights.Normal;
    }

    public bool IsNonSdkPackage => (Children.Count == 0) && (PackageRecord.OtherAppsCount == 1);

    public ImageSource Logo => cachedLogo;

    private BitmapImage LoadPackageLogo()
    {
        try                                        
        {
            return new BitmapImage()
            {
                DecodePixelHeight = 20,
                UriSource = Package.Logo,
            };
        }
        catch  // expected, especially for VS deployed packages
        {
        }

        return new BitmapImage(new Uri("ms-appx:///Resources/missing.png"));
    }

    // ignores any children, it's only used to identify this tree node
    public static bool operator ==(PackageItem? x, PackageItem? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if ((x is null) || (y is null))
        {
            return false;
        }

        if (string.Equals(x.Package.Id.FullName, y.Package.Id.FullName, StringComparison.Ordinal))
        {
            // this node can occur in multiple places, check parents 
            return x.Parent!.Equals(y.Parent);
        }

        return false;
    }
    public static bool operator !=(PackageItem? x, PackageItem? y) => !(x == y);
    public override int GetHashCode() => Package.GetHashCode();
    public override bool Equals(object? obj) => this == (obj as PackageItem);

    public override int CompareTo(ItemBase? item)
    {
        const int cBefore = -1;
        const int cAfter = 1;

        if (item is not PackageItem other)
        {
            throw new ArgumentException("incompatible type", nameof(item));
        }

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

