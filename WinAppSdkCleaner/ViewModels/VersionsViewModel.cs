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
    private readonly CollectionViewSource viewSource = new() { IsSourceGrouped = true };

    public VersionsViewModel(Microsoft.UI.Dispatching.DispatcherQueue uiDispatcher)
    {
        Model.VersionsLoaded += (s, e) =>
        {
            // the model loads the versions on a non ui thread
            uiDispatcher.TryEnqueue(() =>
            {
                if ((viewSource.View is null) || (viewSource.View.Count == 0))
                {
                    RaisePropertyChanged(nameof(VersionsView));
                }
            });
        };
    }

    public ICollectionView VersionsView
    {
        get
        {
            if (!Model.VersionListLoaded)  
            {
                // The page is being loaded before the model has retrived the versions.
                // A VersionsLoaded event is going to be raised when the model has loaded them. 
                viewSource.Source = new List<GroupInfo>();
            }
            else
            {
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

                viewSource.Source = groups;
            }

            return viewSource.View;
        }
    }

    private static string GetSdkName(SdkId sdkId)
    {
        foreach (ISdk sdk in Model.SupportedSdk)
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

