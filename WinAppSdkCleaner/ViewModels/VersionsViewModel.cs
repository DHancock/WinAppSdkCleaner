using Microsoft.UI.Xaml.Data;

using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed record DisplayVersion(string SemanticVersion, string PackageVersion);

internal partial class GroupInfo : List<DisplayVersion>
{
    public GroupInfo(string headerName, IEnumerable<DisplayVersion> items) : base(items)
    {
        Name = headerName;
    }

    public string Name { get; }
}


internal sealed partial class VersionsViewModel : INotifyPropertyChanged
{
    private bool dataAvailable = false;

    public VersionsViewModel()
    {
        Model.VersionsLoaded += (s, e) =>
        {
            Interlocked.Exchange(ref dataAvailable, true);
            RaisePropertyChanged(nameof(VersionsList));
        };
    }

    public IEnumerable<GroupInfo> VersionsList
    {
        get
        {
            if (!dataAvailable)
            {
                return new List<GroupInfo>();
            }

            IEnumerable<IGrouping<SdkId, VersionRecord>> query = from version in Model.VersionsList
                                                                         group version by version.SdkId into g
                                                                         orderby g.Key descending
                                                                         select g;

            List<GroupInfo> groups = new();
            PackageVersionComparer comparer = new PackageVersionComparer();

            foreach (IGrouping<SdkId, VersionRecord> g in query)
            {

                IEnumerable<DisplayVersion> data = g.OrderByDescending(v => v.Release, new PackageVersionComparer())
                    .Select(v => new DisplayVersion($"{v.SemanticVersion} {v.VersionTag}", v.PackageVersionStr));

                groups.Add(new GroupInfo(BuildHeaderText(g.Key), data));
            }

            return groups;
        }
    }

    private static string BuildHeaderText(SdkId sdkId)
    {
        foreach (ISdk sdk in Model.SupportedSdks)
        {
            if (sdk.Id == sdkId)
            {
                return $"{sdk.DispalyName} package versions";
            }
        }

        return $"{sdkId} package versions";
    }

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

