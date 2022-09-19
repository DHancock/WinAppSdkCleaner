using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class DependentsItem : ItemBase
{
    public DependentsItem(PackageRecord packageRecord, SdkPackageItem parent) : base(parent)
    {
        foreach (PackageRecord dependentPackage in packageRecord.PackagesDependentOnThis)
            Children.Add(new PackageItem(dependentPackage, this));
    }

    public override string HeadingText => "Dependent packages";
    public override string ToolTipText => "Packages that depend on this framework package";


    // ignores any children, it's only used to identify this tree node
    public static bool operator ==(DependentsItem? x, DependentsItem? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if ((x is null) || (y is null))
            return false;

        return x.Parent!.Equals(y.Parent); // it's a container, compare parents
    }

    public static bool operator !=(DependentsItem? x, DependentsItem? y) => !(x == y);
    public override int GetHashCode() => Children.GetHashCode();
    public override bool Equals(object? obj) => this == (obj as DependentsItem);
}
