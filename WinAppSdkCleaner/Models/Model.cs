using CsWin32Lib;

using Windows.Foundation;

namespace WinAppSdkCleaner.Models;

internal sealed class Model
{
    private static readonly JsonSerializerOptions jsonOptions = GetSerializerOptions();
    private static readonly HttpClient httpClient = new HttpClient();

    private static bool IsWinAppSdkName(PackageId id)
    {
        return (id.FullName.Contains("WinAppRuntime", StringComparison.OrdinalIgnoreCase) ||
                    id.FullName.Contains("WindowsAppRuntime", StringComparison.OrdinalIgnoreCase) ||
                    id.FullName.StartsWith("Microsoft.WindowsAppSDK", StringComparison.Ordinal));  // for 1.0.0 experimental 1 only
    }

    private static bool IsReunionName(PackageId id)
    {
        return id.FullName.Contains("ProjectReunion", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMicrosoftPublisher(PackageId id)
    {
        return string.Equals(id.PublisherId, "8wekyb3d8bbwe", StringComparison.Ordinal);
    }

    private static bool IsSdkName(PackageId id, SdkTypes sdkId)
    {
        if (IsMicrosoftPublisher(id))
        {
            switch (sdkId)
            {
                case SdkTypes.Reunion: return IsReunionName(id);
                case SdkTypes.WinAppSdk: return IsWinAppSdkName(id);
                default: throw new ArgumentOutOfRangeException(nameof(sdkId));
            }
        }

        return false;
    }

    private static void AddUnknownSdkVersions(List<VersionRecord> sdkVersions, List<Package> sdkPackages, SdkTypes sdkId)
    {
        List<Package> otherSdkPackages = sdkPackages.FindAll(p => sdkVersions.All(v => v.Release != p.Id.Version));

        if (otherSdkPackages.Count > 0)
        {
            foreach (Package package in otherSdkPackages.DistinctBy(p => p.Id.Version))
            {
                sdkVersions.Add(new VersionRecord(string.Empty, string.Empty, string.Empty, sdkId, package.Id.Version));
            }
        }
    }

    private static void AddDependents(ConcurrentDictionary<string, PackageRecord> lookUpTable, List<Package> allPackages, int depth)
    {
        ConcurrentDictionary<string, PackageRecord> subLookUp = new ConcurrentDictionary<string, PackageRecord>();

        Parallel.ForEach(allPackages, package =>
        {
            foreach (Package dependency in package.Dependencies)
            {
                if (lookUpTable.TryGetValue(dependency.Id.FullName, out PackageRecord? parentPackageRecord))
                {
                    PackageRecord dependentPackage = new PackageRecord(package, new List<PackageRecord>(), depth);
                    parentPackageRecord!.PackagesDependentOnThis.Add(dependentPackage);

                    if (package.IsFramework)
                        subLookUp[package.Id.FullName] = dependentPackage;
                }
            }
        });

        if (!subLookUp.IsEmpty)
            AddDependents(subLookUp, allPackages, depth + 1);
    }

    private static List<SdkRecord> GetPackages(List<VersionRecord> versions, bool allUsers)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<SdkRecord> sdks = new List<SdkRecord>();
        ConcurrentDictionary<string, PackageRecord> sdkLookUpTable = new ConcurrentDictionary<string, PackageRecord>();

        PackageManager packageManager = new PackageManager();
        List<Package> allPackages;

        if (allUsers)
            allPackages = packageManager.FindPackages().ToList();
        else
            allPackages = packageManager.FindPackagesForUser(string.Empty).ToList();

        foreach (SdkTypes sdkId in Enum.GetValues<SdkTypes>())
        {
            List<Package> sdkPackages = allPackages.FindAll(p => IsSdkName(p.Id, sdkId));

            if (sdkPackages.Count > 0)
            {
                List<VersionRecord> sdkVersions = versions.FindAll(v => v.SdkId == sdkId);
                AddUnknownSdkVersions(sdkVersions, sdkPackages, sdkId);

                foreach (VersionRecord version in sdkVersions)
                {
                    List<Package> sdkVersionPackages = sdkPackages.FindAll(p => p.Id.Version == version.Release);

                    if (sdkVersionPackages.Count > 0)
                    {
                        List<PackageRecord> sdkPackageRecords = new List<PackageRecord>(sdkVersionPackages.Count);

                        foreach (Package package in sdkVersionPackages)
                        {
                            PackageRecord sdkPackage = new PackageRecord(package, new List<PackageRecord>());
                            sdkPackageRecords.Add(sdkPackage);

                            if (package.IsFramework)
                                sdkLookUpTable[package.Id.FullName] = sdkPackage; // used to find dependents
                        }

                        sdks.Add(new SdkRecord(version, sdkId, sdkPackageRecords));
                    }
                }
            }
        }

        if (!sdkLookUpTable.IsEmpty)
            AddDependents(sdkLookUpTable, allPackages, 1);

        stopwatch.Stop();
        Trace.WriteLine($"Get packages found {sdks.Count} SDKs, elapsed: {stopwatch.Elapsed.TotalSeconds} seconds");
        return sdks;
    }

    public static Task<List<SdkRecord>> GetPackages()
    {
        return Task.Run(async () => GetPackages(await GetVersionsList(), allUsers: IntegrityLevel.IsElevated()));
    }

    private async static Task Remove(string fullName, bool allUsers)
    {
        await Task.Run(() =>
        {
            Trace.WriteLine($"Remove package: {fullName}");

            PackageManager packageManager = new PackageManager();
            IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation;

            using (ManualResetEvent opCompletedEvent = new ManualResetEvent(false))
            {
                if (allUsers)
                    deploymentOperation = packageManager.RemovePackageAsync(fullName, RemovalOptions.RemoveForAllUsers);
                else
                    deploymentOperation = packageManager.RemovePackageAsync(fullName);

                deploymentOperation.Completed = (depProgress, status) => { opCompletedEvent.Set(); };

                opCompletedEvent.WaitOne();
            }

            Trace.WriteLine($"Removal of {fullName}, status: {deploymentOperation.Status}");

            if (deploymentOperation.Status == AsyncStatus.Error)
            {
                DeploymentResult deploymentResult = deploymentOperation.GetResults();
                Trace.WriteLine($"  {deploymentOperation.ErrorCode}");
                Trace.WriteLine($"  {deploymentResult.ErrorText}");
            }
        });
    }

    private async static Task RemovePackages(IEnumerable<string> packageFullNames, bool allUsers)
    {
        const int cTimeoutPerPackage = 10; // seconds

        if (packageFullNames.Any())
        {
            List<Task> tasks = new List<Task>();

            int milliSeconds = cTimeoutPerPackage * 1000 * packageFullNames.Count();
            Task timeOut = Task.Delay(milliSeconds);

            foreach (string fullName in packageFullNames)
            {
                tasks.Add(Remove(fullName, allUsers));
            }

            Task firstOut = await Task.WhenAny(Task.WhenAll(tasks), timeOut);

            if (firstOut == timeOut)
                throw new TimeoutException($"Removal of sdk timed out after {milliSeconds / 1000} seconds");
        }
    }

    public async static Task RemovePackages(List<PackageRecord> packagesRecords)
    {
        if (packagesRecords.Count > 0)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool allUsers = IntegrityLevel.IsElevated();

            // when removing for all users, any provisioned packages will also be removed
            await RemovePackages(packagesRecords.Where(p => !p.Package.IsFramework).Select(p => p.Package.Id.FullName), allUsers);

            // Now for the frameworks, this is more complicated because there may be
            // framework packages that depend on other frameworks (assuming that's even possible).
            List<PackageRecord> frameworks = packagesRecords.Where(p => p.Package.IsFramework).ToList();

            if (frameworks.Count > 0)
            {
                List<int> depths = frameworks.DistinctBy(p => p.Depth).Select(p => p.Depth).ToList();
                depths.Sort((x, y) => y - x);

                foreach (int depth in depths)
                {
                    // remove batches of framework packages in order of depth, deepest first
                    await RemovePackages(frameworks.Where(p => p.Depth == depth).Select(p => p.Package.Id.FullName), allUsers);
                }
            }

            stopwatch.Stop();
            Trace.WriteLine($"Remove packages completed: {stopwatch.Elapsed.TotalSeconds} seconds");
        }
    }

    private static async Task<List<VersionRecord>> GetVersionsList()
    {
        List<VersionRecord> versionList = new List<VersionRecord>();

        try
        {
            string text = await ReadAllTextRemote();
#if DEBUG
            text = string.Empty;  // always use the embedded file, using the current schema         
#endif
            if (string.IsNullOrEmpty(text))
                text = await ReadAllTextLocal();

            if (!string.IsNullOrEmpty(text))
            {
                List<VersionRecord>? versions = JsonSerializer.Deserialize<List<VersionRecord>>(text, jsonOptions);

                if (versions is not null)
                {
                    Debug.Assert(versions.DistinctBy(v => v.Release).Count() == versions.Count, "caution: duplicate package versions detected");
                    versionList = versions;
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        Trace.WriteLine($"Versions list contains {versionList.Count} entries");
        return versionList;
    }

    private static async Task<string> ReadAllTextRemote()
    {
        try
        {
            const string path = "https://raw.githubusercontent.com/DHancock/WinAppSdkCleaner/main/WinAppSdkCleaner/versions.json";
            return await httpClient.GetStringAsync(path);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return string.Empty;
    }

    private static async Task<string> ReadAllTextLocal()
    {
        try
        {
            using (Stream? stream = typeof(App).Assembly.GetManifestResourceStream("WinAppSdkCleaner.versions.json"))
            {
                if (stream is not null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return string.Empty;
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