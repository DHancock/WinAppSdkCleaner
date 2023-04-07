using WinAppSdkCleaner.ViewModels;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for VersionsView.xaml
/// </summary>
public partial class VersionsView : UserControl
{
    private readonly VersionsViewModel viewModel;
    private readonly ViewCommand winAppSdkCopyCommand;
    private readonly ViewCommand reunionCopyCommand;

    public VersionsView()
    {
        InitializeComponent();

        DataContext = viewModel = new VersionsViewModel();

        winAppSdkCopyCommand = InitialiseCommand("WinAppSdkCopy", ExecuteWinAppSdkCopy, CanWinAppSdkCopy);
        reunionCopyCommand = InitialiseCommand("ReunionCopy", ExecuteReunionCopy, CanReunionCopy);

        Loaded += async (s, e) =>
        {
            await ((VersionsViewModel)DataContext).LoadVersionInfo();
        };
    }

    private ViewCommand InitialiseCommand(string key, Action<object?> execute, Func<object?, bool> canExecute)
    {
        ViewCommand command = (ViewCommand)FindResource(key);
        command.CanExecuteProc = canExecute;
        command.ExecuteProc = execute;
        return command;
    }

    private void ExecuteWinAppSdkCopy(object? param) => viewModel.ExecuteCopy(Models.SdkId.WinAppSdk);
    private bool CanWinAppSdkCopy(object? param) => viewModel.CanCopy(Models.SdkId.WinAppSdk);

    private void ExecuteReunionCopy(object? param) => viewModel.ExecuteCopy(Models.SdkId.Reunion);
    private bool CanReunionCopy(object? param) => viewModel.CanCopy(Models.SdkId.Reunion);

    private void WinAppSdkSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        winAppSdkCopyCommand.RaiseCanExecuteChanged();
    }

    private void ReunionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        reunionCopyCommand.RaiseCanExecuteChanged();
    }
}


