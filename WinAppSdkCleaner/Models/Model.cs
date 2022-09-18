using System.Linq;

using WinAppSdkCleaner.Common;

using Windows.Foundation.Metadata;

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


    // for each sdkpackage go through all packeages checking their dependencies to seee if the sdk is in there.

    // turn it round = for ever package go through dependents and see if they are an sdk and then add to that sdk.
    // use a dictionary for sdk package look ups

    private static List<PackageRecord> FindPackagesDependingOn(Package source, List<Package> allPackages)
    {
        List<PackageRecord> dependantRecords = new List<PackageRecord>();

        List<Package> dependants = allPackages.FindAll(p => p.Dependencies.Any(d => d.Id.FullName == source.Id.FullName));

        foreach (Package package in dependants)
        {
            dependantRecords.Add(new PackageRecord(package, new List<PackageRecord>()));// FindPackagesDependingOn(package, allPackages)));
        }

        return dependantRecords;
    }


    // if install provisioned the framework cannot be provisioned so it only installs the others
    // the framework is added as a user package and because the provisioned packages have dependencies
    // on it they get installed for that user too.
    // when deleting provisioned packages should the user dependent packages also be deleted?
    // (thats the framework package, and using For all users option?) 
    public static List<SdkRecord> GetPackages(List<VersionRecord> versions, bool userPackages)
    {
        // build 22621 does a lot of the heavy lifting here, not released yet though
        Debug.Assert(!ApiInformation.IsMethodPresent("Windows.ApplicationModel.Package", "FindRelatedPackages"));
        Debug.Assert(!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 15));


        List<SdkRecord> sdks = new List<SdkRecord>();
        PackageManager packageManager = new PackageManager();

        List<Package> allPackages = new List<Package>(packageManager.FindPackagesForUser(string.Empty));
        List<Package> sdkPackages;

        if (userPackages)
        {
            allPackages = new List<Package>(packageManager.FindPackagesForUser(string.Empty));
            sdkPackages = allPackages.FindAll(p => IsWinAppSdkName(p.Id));
        }
        else
        {
            allPackages = new List<Package>(packageManager.FindProvisionedPackages());
            sdkPackages = allPackages.FindAll(p => IsWinAppSdkName(p.Id));
        }
        
        if (sdkPackages.Count > 0)
        {
            AddUnknownSdkVersions(versions, sdkPackages);

            foreach (VersionRecord version in versions)
            {
                List<Package> sdkVersionPackages = sdkPackages.FindAll(p => p.Id.Version == version.Release);

                if (sdkVersionPackages.Count > 0)
                {
                    List<PackageRecord> sdkPackageRecords = new List<PackageRecord>(sdkVersionPackages.Count);

                    foreach (Package package in sdkVersionPackages)
                    {
                        sdkPackageRecords.Add(new PackageRecord(package, FindPackagesDependingOn(package, allPackages)));
                    }

                    sdks.Add(new SdkRecord(version, sdkPackageRecords));
                }
            }
        }

        return sdks;
    }


    public static Task<List<SdkRecord>> GetUserPackages()
    {
        return Task.Run(async () => GetPackages(await GetVersionsList(), userPackages: true));
    }

    public static Task<List<SdkRecord>> GetProvisionedPackages()
    {
        return Task.Run(async () => GetPackages(await GetVersionsList(), userPackages: false));
    }

    // Works well, but is the nuclear option, error handling is limited.
    // PackageManager.RemovePackageAsync() occasionally never completes a valid task.
    private async static Task Remove(string args)
    {
        await Task.Run(() =>
        {
            Trace.WriteLine(args);

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
            };

            Process process = new Process();
            process.StartInfo = startInfo;
            process.ErrorDataReceived += (s, e) => Trace.WriteLine(e.Data);
            process.Start();
            process.BeginErrorReadLine();

            process.WaitForExit();
        });
    }

    private async static Task RemoveUserPackages(IEnumerable<string> packageFullNames)
    {
        if (packageFullNames.Any())
        {
            List<Task> tasks = new List<Task>();

            bool allUsers = Settings.Data.RemoveForAllUsers && IntegrityLevel.IsElevated();
            int milliSeconds = Settings.Data.TimeoutPerPackage * 1000 * packageFullNames.Count();
            Task timeOut = Task.Delay(milliSeconds);

            foreach (string fullName in packageFullNames)
            {
                if (allUsers)
                    tasks.Add(Remove($"Remove-AppxPackage -Package {fullName} -AllUsers"));
                else
                    tasks.Add(Remove($"Remove-AppxPackage -Package {fullName}"));
            }

            Task firstOut = await Task.WhenAny(Task.WhenAll(tasks), timeOut);

            if (firstOut == timeOut)
                throw new TimeoutException($"Removal of user packages timed out after {milliSeconds / 1000} seconds");
        }
    }

    public async static Task RemoveUserPackages(List<Package> packages)
    {
        if (packages.Count > 0)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            await RemoveUserPackages(packages.Where(p => !p.IsFramework).Select(p => p.Id.FullName));
            await RemoveUserPackages(packages.Where(p => p.IsFramework).Select(p => p.Id.FullName));

            stopwatch.Stop();
            Trace.WriteLine($"user removal completed: {stopwatch.Elapsed.TotalSeconds} seconds");
        }
    }

    public async static Task RemoveProvisionedPackages(List<Package> packages)
    {
        if (packages.Count > 0)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            foreach (Package package in packages)  // Dism commands are strictly one in, one out
            {
                Task timeOut = Task.Delay(Settings.Data.TimeoutPerPackage * 1000);
                Task removal = Remove($"Remove-AppxProvisionedPackage -PackageName {package.Id.FullName} -Online");

                Task firstOut = await Task.WhenAny(removal, timeOut);

                if (firstOut == timeOut)
                    throw new TimeoutException($"Removal of provisioned packages timed out after {Settings.Data.TimeoutPerPackage} seconds");
            }

            stopwatch.Stop();
            Trace.WriteLine($"provisioned removal completed: {stopwatch.Elapsed.TotalSeconds} seconds");
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
        catch (Exception ex)
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