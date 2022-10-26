namespace WinAppSdkCleaner.Models;

public enum SdkId { Reunion, WinAppSdk };

internal interface ISdk
{
    string DispalyName { get; }

    bool Match(PackageId pId);

    SdkId Id { get; }
}

