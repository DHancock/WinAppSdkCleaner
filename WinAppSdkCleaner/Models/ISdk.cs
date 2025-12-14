namespace WinAppSdkCleaner.Models;

internal interface ISdk
{
    string DisplayName { get; }

    bool Match(PackageId pId);

    SdkId Id { get; }

    static bool IsMicrosoftPublisher(PackageId id)
    {
        return string.Equals(id.PublisherId, "8wekyb3d8bbwe", StringComparison.Ordinal);
    }
}

