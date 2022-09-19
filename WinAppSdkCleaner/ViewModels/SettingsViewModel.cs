using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class SettingsViewModel : INotifyPropertyChanged
{
    // remove packages options

    public static bool RemoveForAllUsers
    {
        get => Settings.Data.RemoveForAllUsers;
        set => Settings.Data.RemoveForAllUsers = value;
    }

    public int TimeoutPerPackage
    {
        get => Settings.Data.TimeoutPerPackage;
        set
        {
            Settings.Data.TimeoutPerPackage = value;
            RaisePropertyChanged(); // to update the associated TextBlock
        }
    }


    // search package options 

    public static bool PreferLocalVersionsFileOn
    {
        get => Settings.Data.PreferLocalVersionsFile;
        set => Settings.Data.PreferLocalVersionsFile = value;
    }

    public static bool PreferLocalVersionsFileOff
    {
        get => !Settings.Data.PreferLocalVersionsFile;
        set => Settings.Data.PreferLocalVersionsFile = !value;
    }

    public static bool AutoSearchWhenSwitchingTabs
    {
        get => Settings.Data.AutoSearchWhenSwitchingTabs;
        set => Settings.Data.AutoSearchWhenSwitchingTabs = value;
    }

    private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
