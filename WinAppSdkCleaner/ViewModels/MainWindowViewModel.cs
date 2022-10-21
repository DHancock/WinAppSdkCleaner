namespace WinAppSdkCleaner.ViewModels;

internal sealed class MainWindowViewModel
{
    public string SdkTabHeading { get; init; }
    public ImageSource WindowIcon { get; init; }

    public MainWindowViewModel()
    {
        if (Models.IntegrityLevel.IsElevated)
        {
            SdkTabHeading = "All _Users";
            WindowIcon = BitmapFrame.Create(new Uri("pack://application:,,,/resources/elevated.ico"));
        }
        else
        {
            SdkTabHeading = "_User Scope";
            WindowIcon = BitmapFrame.Create(new Uri("pack://application:,,,/resources/normal.ico"));
        }
    }
}