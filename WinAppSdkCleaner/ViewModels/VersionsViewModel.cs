using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed record DisplayVersion(string SemanticVersion, string PackageVersion);

internal sealed partial class VersionsViewModel : INotifyPropertyChanged
{
    public VersionsViewModel()
    {
        Model.VersionsLoaded += (s, e) =>
        {
            RaisePropertyChanged(nameof(WinAppSdkList));
            RaisePropertyChanged(nameof(ReunionList));
        };
    }

    public IEnumerable<DisplayVersion> WinAppSdkList => FilterVersionsList(SdkId.WinAppSdk);

    public IEnumerable<DisplayVersion> ReunionList => FilterVersionsList(SdkId.Reunion);

    private static IEnumerable<DisplayVersion> FilterVersionsList(SdkId sdkId)
    {
        return Model.FilterVersionsList(sdkId)
                        .OrderByDescending(v => v.Release, new PackageVersionComparer())
                        .Select(v => new DisplayVersion($"{v.SemanticVersion} {v.VersionTag}", v.PackageVersionStr));
    }

    private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}