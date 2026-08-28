using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal sealed partial class SdkViewModel : INotifyPropertyChanged
{
    public SdkViewModel()
    {
    }

    public SdkList SdkList
    {
        get { return field ?? new SdkList(); }

        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    public async Task ExecuteSearchAsync()
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

    public async Task ExecuteRemoveAsync(SdkItem sdk)
    {
        int index = SdkList.BinarySearch(sdk); // use the latest backing data

        if (index >= 0)
        {
            IEnumerable<Package> packages = SdkList.GetPackages(SdkList[index]);

            if (packages.Any())
            {
                await Model.RemovePackagesAsync(packages);
            }
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
