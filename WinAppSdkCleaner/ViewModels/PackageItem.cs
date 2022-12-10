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
            Children.Add(new PackageItem(dependentPackage, this));

        Children.Sort();
    }

    public Package Package => PackageRecord.Package;

    public override string HeadingText => Package.DisplayName;

    public override string ToolTipText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Package.Description))
                return Package.Id.FullName;

            return Package.Description;
        }
    }
    public override string OtherAppsCount => $"+{PackageRecord.OtherAppsCount}";

    public override Visibility OtherAppsCountVisibity
    {
        get => Children.Any() && (PackageRecord.OtherAppsCount > 0) ? Visibility.Visible : Visibility.Collapsed;
    }

    public override Visibility LogoVisibity => Visibility.Visible;

    public override FontWeight HeadingFontWeight
    {
        get => IsNonSdkPackage ? FontWeights.SemiBold : FontWeights.Regular;
    }

    public override ImageSource? Logo => cachedLogo;

    private ImageSource LoadPackageLogo()
    {
        try
        {
            BitmapImage bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.DecodePixelHeight = 16;
            bitmap.UriSource = Package.Logo;
            bitmap.EndInit();

            return bitmap;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"An exception was thrown retrieving the logo for package: {Package.DisplayName} ({Package.Id.FullName})");
            Trace.WriteLine(ex.ToString());
        }

        return new BitmapImage(new Uri("pack://application:,,,/resources/unknown.png"));
    }

    // ignores any children, it's only used to identify this tree node
    public static bool operator ==(PackageItem? x, PackageItem? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if ((x is null) || (y is null))
            return false;

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

    private bool IsNonSdkPackage => (Children.Count == 0) && (PackageRecord.OtherAppsCount == 1);

    public override int CompareTo(ItemBase? item)
    {
        const int cBefore = -1;
        const int cAfter = 1;

        if (item is not PackageItem other)
            throw new ArgumentException("incompatible type", nameof(item));

        if (IsNonSdkPackage != other.IsNonSdkPackage)
            return IsNonSdkPackage ? cBefore : cAfter;

        return base.CompareTo(item);
    }
}

