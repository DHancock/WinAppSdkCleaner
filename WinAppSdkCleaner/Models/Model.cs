using WinAppSdkCleaner.Utilities;

namespace WinAppSdkCleaner.Models;

internal sealed class Model
{
    private static readonly JsonSerializerOptions jsonOptions = GetSerializerOptions();
    private static readonly HttpClient httpClient = new HttpClient();

    public static bool IsWinAppSdkName(PackageId id)
    {
        return id.PublisherId == "8wekyb3d8bbwe" &&
                    (id.FamilyName.Contains("winappruntime", StringComparison.OrdinalIgnoreCase) ||
                    id.FamilyName.Contains("windowsappruntime", StringComparison.OrdinalIgnoreCase));
    }

    private static void AddUnknownSdkVersions(List<VersionRecord> versions, List<Package> sdkPackages)
    {
        List<Package> otherSdkPackages = sdkPackages.FindAll(p => versions.All(v => v.Release != p.Id.Version));

        if (otherSdkPackages.Count > 0)
        {
            foreach (Package package in otherSdkPackages.DistinctBy(p => p.Id.Version))
            {
                PackageVersion pv = package.Id.Version;
                versions.Add(new VersionRecord(Utils.ConvertToString(pv), pv));
            }

            versions.Sort(Utils.VersionRecordComparer);
        }
    }

    private static IList<SdkRecord> GetPackagesForUser(List<VersionRecord> versions)
    {
        List<SdkRecord> sdks = new List<SdkRecord>();
        PackageManager packageManager = new PackageManager();

        List<Package> sdkPackages = new List<Package>(packageManager.FindPackagesForUser(string.Empty).Where(p => IsWinAppSdkName(p.Id)));

        AddUnknownSdkVersions(versions, sdkPackages);

        foreach (VersionRecord version in versions)
        {
            // Minor versions of the main, singleton and framework packages get replaced when updated.
            // The ddlm packages remain, but their dependencies are updated to reference the new frameworks

            List<Package> sdkVersionPackages = sdkPackages.FindAll(p => p.Id.Version == version.Release);

            if (sdkVersionPackages.Count > 0)
            {
                List<PackageRecord> sdkPackageRecords = new List<PackageRecord>(sdkVersionPackages.Count);

                foreach (Package package in sdkVersionPackages)
                {
                    sdkPackageRecords.Add(new PackageRecord(package, FindPackagesDependingOn(package, sdkPackages)));
                }

                sdks.Add(new SdkRecord(version, sdkPackageRecords));
            }
        }

        return sdks;
    }


    private static List<PackageRecord> FindPackagesDependingOn(Package source, List<Package> packages)
    {
        List<PackageRecord> dependantRecords = new List<PackageRecord>();

        if (source.IsFramework)  // only framework packages can have dependents
        {
            List<Package> dependants = packages.FindAll(p => p.Dependencies.Any(d => d.Id.FullName == source.Id.FullName));

            foreach (Package package in dependants)
            {
                dependantRecords.Add(new PackageRecord(package, FindPackagesDependingOn(package, packages)));
            }
        }

        return dependantRecords;
    }

    public static Task<IList<SdkRecord>> GetPackageList()
    {
        return Task.Run(async () => GetPackagesForUser(await GetVersionsList()));
    }

#if false

    // should work, but occasionally the task never completes

    private async static Task Remove(string packageFullName, bool allUsers)   
    {
        PackageManager packageManager = new PackageManager();
        RemovalOptions ro = allUsers ? RemovalOptions.RemoveForAllUsers : RemovalOptions.None;

        DeploymentResult dr = await packageManager.RemovePackageAsync(packageFullName, ro).AsTask();

        if (dr.ExtendedErrorCode is not null)
        {
            Trace.WriteLine(dr.ErrorText);

            if (dr.ExtendedErrorCode is System.Runtime.InteropServices.COMException cex)
            {
                if (cex.HResult == unchecked((int)0x80073CF1))
                {
                    // package not found, it may have been automatically removed when  
                    // the last package that depended on it was removed
                    return;
                }
            }

            throw new Exception(dr.ErrorText);
        }
    }

#else

    // works well, but is the nuclear option, error handling is limited

    private async static Task Remove(string fullName, bool allUsers)  
    {
        await Task.Run(() =>
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = "powershell.exe";
            startInfo.Arguments = $"Remove-AppxPackage -Package {fullName}";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;

            if (allUsers)
                startInfo.Arguments += " -AllUsers";

            Trace.WriteLine(startInfo.Arguments);

            Process process = new Process();

            process.StartInfo = startInfo;
            process.ErrorDataReceived += (s, e) => Trace.WriteLine(e.Data);
            process.Start();
            process.BeginErrorReadLine();

            process.WaitForExit();
        });
    }

#endif

    private async static Task RemovePackages(IEnumerable<string> packageFullNames)
    {
        if (packageFullNames.Any())
        {
            List<Task> tasks = new List<Task>();

            bool allUsers = Settings.Data.RemoveForAllUsers && IntegrityLevel.IsElevated();
            int milliSeconds = Settings.Data.TimeoutPerPackage * 1000 * packageFullNames.Count();
            Task timeOut = Task.Delay(milliSeconds);

            foreach (string fullName in packageFullNames)
            {
                tasks.Add(Remove(fullName, allUsers));
            }

            Task firstOut = await Task.WhenAny(Task.WhenAll(tasks), timeOut);

            if (firstOut == timeOut)
                throw new TimeoutException($"timed out after {milliSeconds / 1000} seconds");
        }
    }

    public async static Task Remove(List<Package> packages)
    {
        if (packages.Count > 0)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            await RemovePackages(packages.Where(p => !p.IsFramework).Select(p => p.Id.FullName));
            await RemovePackages(packages.Where(p => p.IsFramework).Select(p => p.Id.FullName));
            
            stopwatch.Stop();
            Trace.WriteLine($"elapsed: {stopwatch.Elapsed.TotalSeconds}");
        }
    }
  
    private static async Task<List<VersionRecord>> GetVersionsList()
    {
        List<VersionRecord> versionList = new List<VersionRecord>();

        try
        {
            string text = await ReadAllText();
            List<VersionRecord>? versions = JsonSerializer.Deserialize<List<VersionRecord>>(text, jsonOptions);

            if (versions is not null)
                versionList = versions;
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        if (versionList.Count > 0)
            versionList.Sort(Utils.VersionRecordComparer);

        return versionList;
    }

    private static async Task<string> ReadAllText()
    {
        string text;

        if (Settings.Data.PreferLocalVersionsFile)
        {
            text = await ReadAllTextLocal();

            if (string.IsNullOrEmpty(text))
                text = await ReadAllTextRemote();
        }
        else
        {
            text = await ReadAllTextRemote();

            if (string.IsNullOrEmpty(text))
                text = await ReadAllTextLocal();
        }

        return text;
    }



    private static async Task<string> ReadAllTextRemote()
    {
        try
        {
            string path = "https://raw.githubusercontent.com/DHancock/WinAppSdkCleaner/main/WinAppSdkCleaner/versions.json";
            return await httpClient.GetStringAsync(path);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message);
        }

        return String.Empty;
    }


    private static async Task<string> ReadAllTextLocal()
    {
        try
        {
            string path = Path.Join(Path.GetDirectoryName(typeof(App).Assembly.Location), "versions.json");
            return await File.ReadAllTextAsync(path);
        }
        catch(Exception ex)
        {
            Trace.WriteLine(ex.Message);
        }

        return String.Empty;
    }

    private static JsonSerializerOptions GetSerializerOptions()
    {
        return new JsonSerializerOptions()
        {
            WriteIndented = true,
            IncludeFields = true,
        };
    }
}