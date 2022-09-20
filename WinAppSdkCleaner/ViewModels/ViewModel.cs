using WinAppSdkCleaner.Common;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class ViewModel
{
    public SdkViewModel SdkViewModel { get; init; }
    public SettingsViewModel SettingsViewModel { get; init; }
    public string UserSdkTabHeading { get; init; }
    public ImageSource WindowIcon { get; init; }

    public ViewModel()
    {
        SdkViewModel = new SdkViewModel();
        SettingsViewModel = new SettingsViewModel();

        if (IntegrityLevel.IsElevated())
        {
            UserSdkTabHeading = "All _Users";
            WindowIcon = BitmapFrame.Create(new Uri("pack://application:,,,/resources/elevated.ico"));
        }
        else
        {
            UserSdkTabHeading = "_User Scope";
            WindowIcon = BitmapFrame.Create(new Uri("pack://application:,,,/resources/app.ico"));
        }
    }
}