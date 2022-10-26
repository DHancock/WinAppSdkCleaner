namespace WinAppSdkCleaner.Models;

internal sealed class ProjectReunion : ISdk
{
    public string DispalyName => "Project Reunion";

    public bool Match(PackageId pId)
    {
        return pId.FullName.Contains("ProjectReunion", StringComparison.OrdinalIgnoreCase);
    }

    public SdkId Id => SdkId.Reunion;
}

