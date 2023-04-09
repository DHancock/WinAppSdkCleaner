using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class VersionsViewModel : INotifyPropertyChanged
{
    private IEnumerable<VersionRecord>? versions;

    public async Task LoadVersionInfo()
    {
        if (versions is null)
        {
            versions = await Model.sVersionsProvider;

            RaisePropertyChanged(nameof(WinAppSdkList));
            RaisePropertyChanged(nameof(ReunionList));
        }
    }

    public IEnumerable<DisplayVersion> WinAppSdkList => FilterVersionsList(SdkId.WinAppSdk);

    public IEnumerable<DisplayVersion> ReunionList => FilterVersionsList(SdkId.Reunion);

    private IEnumerable<DisplayVersion> FilterVersionsList(SdkId sdkId)
    {
        if (versions is null)
            return new List<DisplayVersion>();

        return versions.Where(v => v.SdkId == sdkId)
                        .OrderByDescending(v => v.Release, new PackageVersionComparer())
                        .Select(v => new DisplayVersion($"{v.SemanticVersion} {v.VersionTag}", ConvertToString(v.Release)));
    }

    private static string ConvertToString(PackageVersion pv)
    {
        return $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";
    }

    private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal sealed record DisplayVersion(string SemanticVersion, string PackageVersion);
}