using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed record DisplayVersion(string SdkName, string SdkVersion, string PackageVersion)
{
    public string AutomationName => $"{SdkName} version {SdkVersion} package version {PackageVersion}";
};

internal sealed partial class GroupInfo : List<DisplayVersion>
{
    public GroupInfo(string sdkName, IEnumerable<DisplayVersion> items) : base(items)
    {
        Name = $"{sdkName} package versions";
    }

    public string Name { get; }

    public override string ToString() => Name;   // the narrator uses this to read the group headers
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

            foreach (IGrouping<SdkId, VersionRecord> g in query)
            {
                string sdkName = GetSdkName(g.Key);

                // the downloaded sdk file is already sorted
                IEnumerable<DisplayVersion> data = g.Reverse().Select(v => new DisplayVersion(sdkName, $"{v.SemanticVersion} {v.VersionTag}", v.PackageVersionStr));

                groups.Add(new GroupInfo(sdkName, data));
            }

            return groups;
        }
    }

    private static string GetSdkName(SdkId sdkId)
    {
        foreach (ISdk sdk in Model.SupportedSdks)
        {
            if (sdk.Id == sdkId)
            {
                return sdk.DisplayName;
            }
        }

        return sdkId.ToString();
    }

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

