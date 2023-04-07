using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class VersionsViewModel : INotifyPropertyChanged
{
    private List<DisplayVersion>? winAppSdkList;
    private List<DisplayVersion>? reunionList;

    public async Task LoadVersionInfo()
    {
        if (winAppSdkList is null)
        {
            IEnumerable<VersionRecord> versions = await Model.sVersionsProvider;

            WinAppSdkList = FilterVersionsList(versions, SdkId.WinAppSdk);
            ReunionList = FilterVersionsList(versions, SdkId.Reunion);
        }
    }

    public List<DisplayVersion> WinAppSdkList
    {
        get => winAppSdkList ?? new List<DisplayVersion>();

        set 
        {
            winAppSdkList = value;
            RaisePropertyChanged();
        }
    }

    public List<DisplayVersion> ReunionList
    {
        get => reunionList ?? new List<DisplayVersion>();

        set
        {
            reunionList = value;
            RaisePropertyChanged();
        }
    }

    private static List<DisplayVersion> FilterVersionsList(IEnumerable<VersionRecord> versions, SdkId sdkId)
    {
        return versions.Where(v => v.SdkId == sdkId)
                        .OrderByDescending(v => v.Release, new PackageVersionComparer())
                        .Select(v => new DisplayVersion($"{v.SemanticVersion} {v.VersionTag}", ConvertToString(v.Release))).ToList();
    }

    private static string ConvertToString(PackageVersion pv)
    {
        return $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";
    }

    public void ExecuteCopy(SdkId sdkId)
    {
        switch (sdkId)
        {
            case SdkId.WinAppSdk: Copy(WinAppSdkList); break;
            case SdkId.Reunion: Copy(ReunionList); break;
            default:
                Debug.Fail($"unknown SdkId: {sdkId}");
                break;
        }
    }

    private void Copy(List<DisplayVersion> list) 
    { 
        IEnumerable<string> selected = list.Where(p => p.IsSelected).Select(p => $"{p.SemanticVersion}\t{p.PackageVersion}");

        StringBuilder sb = new StringBuilder();
        sb.AppendJoin(Environment.NewLine, selected);

        if (sb.Length > 0) 
            Clipboard.SetText(sb.ToString());
    }

    public bool CanCopy(SdkId sdkId)
    {
        switch (sdkId)
        {
            case SdkId.WinAppSdk: return WinAppSdkList.Any(x => x.IsSelected);
            case SdkId.Reunion: return ReunionList.Any(x => x.IsSelected);
        }

        Debug.Fail($"unknown SdkId: {sdkId}");
        return false;
    }


    private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    internal sealed record DisplayVersion(string SemanticVersion, string PackageVersion, bool IsSelected = false);
}
