using CsWin32Lib;

namespace WinAppSdkCleaner.ViewModels;

internal sealed class ViewModel
{
    public string SdkTabHeading { get; init; }
    public ImageSource WindowIcon { get; init; }

    public ViewModel()
    {
        if (IntegrityLevel.IsElevated())
        {
            SdkTabHeading = "All _Users";
            WindowIcon = BitmapFrame.Create(new Uri("pack://application:,,,/resources/elevated.ico"));
        }
        else
        {
            SdkTabHeading = "_User Scope";
            WindowIcon = BitmapFrame.Create(new Uri("pack://application:,,,/resources/app.ico"));
        }
    }
}