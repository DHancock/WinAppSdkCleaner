namespace WinAppSdkCleaner.Models;

internal interface ISdk
{
    string DisplayName { get; }

    bool Match(PackageId pId);

    SdkId Id { get; }
}

