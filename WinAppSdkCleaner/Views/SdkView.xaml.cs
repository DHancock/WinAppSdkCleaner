using WinAppSdkCleaner.ViewModels;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for SdkView.xaml
/// </summary>
public partial class SdkView : UserControl
{
    private readonly ViewCommand rescanCommand;
    private readonly ViewCommand removeCommand;
    private readonly ViewCommand copyCommand;
    
    private SdkViewModel? viewModel;
    private bool isIdle = true;

    public SdkView()
    {
        InitializeComponent();

        rescanCommand = InitialiseCommand("Rescan", ExecuteRescan, CanRescan);
        removeCommand = InitialiseCommand("Remove", ExecuteRemove, CanRemove);
        copyCommand = InitialiseCommand("Copy", ExecuteCopy, CanCopy);

        DataContextChanged += (s, e) => viewModel = (SdkViewModel)e.NewValue;
        Loaded += (s, a) => ExecuteRescan(null);
    }

    private ViewCommand InitialiseCommand(string key, Action<object?> execute, Func<object?, bool> canExecute)
    {
        ViewCommand command = (ViewCommand)FindResource(key);
        command.CanExecuteProc = canExecute;
        command.ExecuteProc = execute;
        return command;
    }

    public async void ExecuteRescan(object? param)
    {
        try
        {
            IsIdle = false;
            await viewModel!.ExecuteRescan();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsIdle = true;
        }
    }

    private bool CanRescan(object? param) => IsIdle;

    private async void ExecuteRemove(object? param)
    {
        try
        {
            IsIdle = false;
            await viewModel!.ExecuteRemove();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            await viewModel!.ExecuteRescan();
        }
        finally
        {
            IsIdle = true;
        }
    }

    private bool CanRemove(object? param) => viewModel!.CanRemove() && IsIdle;

    private void ExecuteCopy(object? param) => viewModel!.ExecuteCopy();

    private bool CanCopy(object? param) => viewModel!.CanCopy() && IsIdle;

    private bool IsIdle
    {
        get => isIdle;

        set
        {
            isIdle = value;
            BusyIndicator.IsIndeterminate = !value;
            AdjustCommandsState();
        }
    }

    private void AdjustCommandsState()
    {
        rescanCommand.RaiseCanExecuteChanged();
        removeCommand.RaiseCanExecuteChanged();
        copyCommand.RaiseCanExecuteChanged();
    }

    private void SelectedTreeViewItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        AdjustCommandsState();
    }
}