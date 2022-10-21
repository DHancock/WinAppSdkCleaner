using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class SdkViewModel : INotifyPropertyChanged
{
    private SdkList sdkList = new SdkList();

    public SdkViewModel()
    {
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
            SdkList newList = new SdkList(await Model.GetSDKsAsync());
            newList.RestoreState(sdkList);

            SdkList = newList;
        }
        catch
        {
            SdkList.Clear();
            throw;
        }
    }

    public async Task ExecuteRemove()
    {
        try
        {
            IEnumerable<PackageRecord> packages = sdkList.GetDistinctSelectedPackages();

            if (packages.Any())
            {
                await Model.RemovePackagesAsync(packages);
            }
        }
        catch 
        {
            throw;
        }
    }

    public bool CanRemove() => sdkList.CanRemove();

    public void ExecuteCopy()
    {
        string data = SdkList.GetCopyData();

        if (!string.IsNullOrEmpty(data))
            Clipboard.SetText(data);
    }

    public bool CanCopy() => sdkList.CanCopy();


    private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}