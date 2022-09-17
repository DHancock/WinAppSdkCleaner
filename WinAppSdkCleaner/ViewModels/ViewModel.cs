namespace WinAppSdkCleaner.ViewModels;

internal sealed class ViewModel
{
    public SdkViewModelBase SdkViewModelUser { get; init; }
    public SdkViewModelBase SdkViewModelProvisioned { get; init; }
    public SettingsViewModel SettingsViewModel { get; init; }

    public ViewModel()
    {
        SdkViewModelUser = new SdkViewModelUser();
        SdkViewModelProvisioned = new SdkViewModelProvisioned();
        SettingsViewModel = new SettingsViewModel();
    }
}