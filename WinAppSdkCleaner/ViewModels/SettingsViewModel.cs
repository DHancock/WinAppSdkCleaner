using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class SettingsViewModel : INotifyPropertyChanged
{
    // remove packages options

    public int TimeoutPerPackage
    {
        get => Settings.Data.TimeoutPerPackage;
        set
        {
            Settings.Data.TimeoutPerPackage = value;
            RaisePropertyChanged(); // to update the associated TextBlock
        }
    }

    private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
