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

    public static bool VerboseTracing
    {
        get => Settings.Data.VerboseTracing;
        set => Settings.Data.VerboseTracing = value;
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

    public static bool AlwaysRescanAfterRemove
    {
        get => Settings.Data.AlwaysRescanAfterRemove;
        set => Settings.Data.AlwaysRescanAfterRemove = value;
    }


    // scan package options 

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

    public static bool IncludeUncategorizedVersions
    {
        get => Settings.Data.IncludeUncategorizedVersions;
        set => Settings.Data.IncludeUncategorizedVersions = value;
    }


    private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
