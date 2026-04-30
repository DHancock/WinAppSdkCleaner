using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed partial class GroupInfo : List<VersionRecord>
{
    public GroupInfo(string sdkName, IEnumerable<VersionRecord> items) : base(items)
    {
        Name = sdkName;
        SingletonVersion = items.Any(vr => !string.IsNullOrWhiteSpace(vr.SingletonVersionStr)) ? "Singleton" : "";
    }

    public string Name { get; }
    public string FrameworkVersion { get; } = "Framework";
    public string SingletonVersion { get; }

    public override string ToString() => $"{Name} {FrameworkVersion} {SingletonVersion}" ; // the narrator uses this to read the group headers
}


internal sealed partial class VersionsViewModel : INotifyPropertyChanged
{
    private readonly CollectionViewSource viewSource = new() { IsSourceGrouped = true };

    public VersionsViewModel(Microsoft.UI.Dispatching.DispatcherQueue uiDispatcher)
    {
        Model.VersionsLoaded += (s, e) =>
        {
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
                // A VersionsLoaded event will be raised when the model has retrieved the versions file. 
                viewSource.Source = new List<GroupInfo>();
            }
            else
            {
                List<GroupInfo> groups = new();

                foreach (ISdk sdk in Model.SupportedSdk.Reverse())
                {
                    string sdkName = GetSdkName(sdk.Id);

                    // filter out any synthesized version records
                    IEnumerable<VersionRecord> query = Model.VersionsList
                                                       .Where(vr => vr.SdkId == sdk.Id && !vr.IsSynthesized)
                                                       .Reverse();

                    groups.Add(new GroupInfo(sdkName, query));
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

        Debug.Fail("failed to find sdk display name");
        return sdkId.ToString();
    }

    public static void ExecuteCopy(IList<object> selectedItems)
    {
        // the selected items are in the time order that they were selected in
        // need to convert them back to sorted list ordering
        StringBuilder sb = new StringBuilder();

        foreach (VersionRecord vr in selectedItems.Cast<VersionRecord>().OrderDescending(new VersionRecordComparer()))
        {
            sb.Append(vr.SdkVersionStr);
            sb.Append('\t');
            sb.Append(vr.PackageVersionStr);
            sb.Append('\t');
            sb.AppendLine(vr.SingletonVersionStr);
        }

        if (sb.Length > 0)
        {
            DataPackage dp = new DataPackage();
            dp.SetText(sb.ToString());
            Clipboard.SetContent(dp);
        }
    }

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

