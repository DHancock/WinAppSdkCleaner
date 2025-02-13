using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed partial class SdkViewModel : INotifyPropertyChanged
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
            SdkList = new SdkList(await Model.GetSDKsAsync());
        }
        catch
        {
            SdkList = new();
            throw;
        }
    }

    public async Task ExecuteRemove(SdkItem sdk)
    {
        try
        {
            await ExecuteSearch();

            int index = SdkList.BinarySearch(sdk);

            if (index >= 0)
            {
                IEnumerable<PackageData> packages = SdkList.GetDistinctPackages(SdkList[index]);

                if (packages.Any())
                {
                    await Model.RemovePackagesAsync(packages);
                }
            }
        }
        catch 
        {
            throw;
        }
    }

    public static void ExecuteCopy(ItemBase item)
    {
        string data = SdkList.GetCopyData(item);

        if (!string.IsNullOrEmpty(data))
        {
            DataPackage dp = new DataPackage();
            dp.SetText(data);
            Clipboard.SetContent(dp);
        }
    }

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
