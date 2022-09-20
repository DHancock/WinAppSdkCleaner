using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal abstract class SdkViewModelBase : INotifyPropertyChanged
{
    private SdkList sdkList = new SdkList();
    private readonly Func<Task<List<SdkRecord>>> search;
    private readonly Func<List<PackageRecord>, Task> remove;
    private readonly bool isEnabled;
    public bool IsSelected { private get; set; }

    public SdkViewModelBase(Func<Task<List<SdkRecord>>> search, Func<List<PackageRecord>, Task> remove, bool isEnabled = true)
    {
        this.search = search;
        this.remove = remove;
        this.isEnabled = isEnabled;
    }

    public SdkList SdkList
    {
        get { return sdkList; }

        set
        {
            sdkList = value;
            RaisePropertyChanged();
        }
    }

    public async Task ExecuteSearch()
    {
        try
        {
            SdkList newList = new SdkList(await search());
            newList.RestoreState(sdkList);

            SdkList = newList;
        }
        catch
        {
            SdkList.Clear();
            throw;
        }
    }

    public bool CanSearch() => isEnabled && IsSelected;

    public async Task ExecuteRemove()
    {
        try
        {
            List<PackageRecord> packages = sdkList.GetDistinctSelectedPackages();

            if (packages.Count > 0)
            {
                await remove(packages);
            }
        }
        catch 
        {
            throw;
        }
    }

    public bool CanRemove() => isEnabled && IsSelected && sdkList.CanRemove();

    public void ExecuteCopy()
    {
        string data = SdkList.GetCopyData();

        if (!string.IsNullOrEmpty(data))
            Clipboard.SetText(data);
    }

    public bool CanCopy() => isEnabled && IsSelected && sdkList.CanCopy();


    private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}