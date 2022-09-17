using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal abstract class SdkViewModelBase : INotifyPropertyChanged
{
    private SdkList sdkList = new SdkList();
    private readonly Func<Task<List<SdkRecord>>> scan;
    private readonly Func<List<Package>, Task> remove;
    private readonly bool isEnabled;
    public bool IsSelected { private get; set; }

    public SdkViewModelBase(Func<Task<List<SdkRecord>>> scan, Func<List<Package>, Task> remove, bool isEnabled = true)
    {
        this.scan = scan;
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

    public async Task ExecuteRescan()
    {
        try
        {
            SdkList newList = new SdkList(await scan());
            newList.RestoreState(sdkList);

            SdkList = newList;
        }
        catch
        {
            SdkList.Clear();
            throw;
        }
    }

    public bool CanRescan() => isEnabled && IsSelected;

    public async Task ExecuteRemove()
    {
        try
        {
            List<Package> packages = sdkList.GetDistinctSelectedPackages();

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