using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal abstract class PackageItemBase : ItemBase
{
    public PackageRecord PackageRecord { get; init; }

    public PackageItemBase(PackageRecord packageRecord, ItemBase parent) : base(parent)
    {
        PackageRecord = packageRecord;
    }

    public Package Package => PackageRecord.Package;
    public override string HeadingText => Model.IsWinAppSdkName(Package.Id) ? Package.Description : Package.DisplayName ;
    public override string ToolTipText => Package.Id.FullName;

    public ImageSource Logo
    {
        get
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
                Trace.WriteLine(ex.ToString());
            }

            return new BitmapImage(new Uri("pack://application:,,,/resources/unknown.png"));
        }
    }

    // ignores any children, it's only used to identify this tree node
    public static bool operator ==(PackageItemBase? x, PackageItemBase? y)
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
    public static bool operator !=(PackageItemBase? x, PackageItemBase? y) => !(x == y);
    public override int GetHashCode() => Package.GetHashCode();
    public override bool Equals(object? obj) => this == (obj as PackageItemBase);
}

