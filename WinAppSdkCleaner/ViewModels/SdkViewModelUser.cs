using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class SdkViewModelUser : SdkViewModelBase
{
    public SdkViewModelUser() : base(Model.GetUserPackages, Model.RemoveUserPackages)
    {
    }
}
