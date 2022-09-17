using WinAppSdkCleaner.Common;
using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner.ViewModels;

internal class SdkViewModelProvisioned : SdkViewModelBase
{
    public SdkViewModelProvisioned() : base(Model.GetProvisionedPackages, Model.RemoveProvisionedPackages, IntegrityLevel.IsElevated())
    {
    }
}

