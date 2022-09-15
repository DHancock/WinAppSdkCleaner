namespace WinAppSdkCleaner.ViewModels;

internal sealed class ViewModel
{
    public SdkViewModel SdkViewModel { get; init; }
    public SettingsViewModel SettingsViewModel { get; init; }

    public ViewModel()
    {
        SdkViewModel = new SdkViewModel();
        SettingsViewModel = new SettingsViewModel();
    }
}