using WinAppSdkCleaner.Models;
using WinAppSdkCleaner.ViewModels;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for SdkView.xaml
/// </summary>
public partial class SdkView : UserControl
{
    private readonly ViewCommand searchCommand;
    private readonly ViewCommand removeCommand;
    private readonly ViewCommand copyCommand;

    private bool firstLoad = true;
    private SdkViewModel? viewModel;
    private bool isIdle = true;

    public SdkView()
    {
        InitializeComponent();

        searchCommand = InitialiseCommand("Search", ExecuteSearch, CanSearch);
        removeCommand = InitialiseCommand("Remove", ExecuteRemove, CanRemove);
        copyCommand = InitialiseCommand("Copy", ExecuteCopy, CanCopy);

        DataContextChanged += (s, e) =>
        {
            viewModel = e.NewValue as SdkViewModel;
        };

        Loaded += (s, a) =>
        {
            Debug.Assert(viewModel is not null);

            AdjustCommandsState();

            if (firstLoad)
            {
                firstLoad = false;
                ExecuteSearch();
            }
        };
    }

    private ViewCommand InitialiseCommand(string key, Action<object?> execute, Func<object?, bool> canExecute)
    {
        ViewCommand command = (ViewCommand)FindResource(key);
        command.CanExecuteProc = canExecute;
        command.ExecuteProc = execute;
        return command;
    }

    public async void ExecuteSearch(object? param = null)
    {
        try
        {
            IsIdle = false;
            await viewModel!.ExecuteSearch();
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

    private bool CanSearch(object? param = null) => IsIdle;

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
        }
        finally
        {
            await viewModel!.ExecuteSearch();
            IsIdle = true;
        }
    }

    private bool CanRemove(object? param) => IsIdle && viewModel!.CanRemove();

    private void ExecuteCopy(object? param) => viewModel!.ExecuteCopy();

    private bool CanCopy(object? param) => IsIdle && viewModel!.CanCopy();

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
        searchCommand.RaiseCanExecuteChanged();
        removeCommand.RaiseCanExecuteChanged();
        copyCommand.RaiseCanExecuteChanged();
    }

    private void SelectedTreeViewItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        AdjustCommandsState();
    }
}