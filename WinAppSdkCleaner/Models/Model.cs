﻿using CsWin32Lib;

// for IAsyncOperationWithProgress, contains conflicts with Rect, Point etc.
using Windows.Foundation;

namespace WinAppSdkCleaner.Models;

internal sealed class Model
{
    private static List<VersionRecord> sVersionList = new List<VersionRecord>();

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

    private static VersionRecord CategorizePackageVersion(PackageVersion version, SdkTypes sdkId, List<VersionRecord> sdkVersions)
    {
        VersionRecord? versionRecord = sdkVersions.FirstOrDefault(v => v.SdkId == sdkId && v.Release == version);

        if (versionRecord is null)
            return new VersionRecord(string.Empty, string.Empty, string.Empty, sdkId, version);

        return versionRecord;
    }

    private static void AddDependents(IReadOnlyDictionary<string, PackageRecord> lookUpTable, IEnumerable<Package> allPackages, int depth)
    {
        object lockObject = new object();
        Dictionary<string, PackageRecord> subLookUp = new Dictionary<string, PackageRecord>();

        Parallel.ForEach(allPackages, package =>
        {
            foreach (Package dependency in package.Dependencies)
            {
                // TryGetValue() is thread safe if the dictionary isn't modified by another thread
                if (lookUpTable.TryGetValue(dependency.Id.FullName, out PackageRecord? parentPackageRecord))
                {
                    lock (lockObject)
                    {
                        PackageRecord dependentPackage = new PackageRecord(package, new List<PackageRecord>(), depth);
                        parentPackageRecord!.PackagesDependentOnThis.Add(dependentPackage);

                        if (package.IsFramework)
                            subLookUp[package.Id.FullName] = dependentPackage;
                    }
                }
            }
        });

        if (subLookUp.Count > 0)
            AddDependents(subLookUp, allPackages, depth + 1);
    }

    private static List<SdkRecord> GetSDKs(List<VersionRecord> versions, bool allUsers)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<SdkRecord> sdks = new List<SdkRecord>();
        Dictionary<string, PackageRecord> sdkLookUpTable = new Dictionary<string, PackageRecord>();

        PackageManager packageManager = new PackageManager();
        IEnumerable<Package> allPackages;

        if (allUsers)
            allPackages = packageManager.FindPackages();
        else
            allPackages = packageManager.FindPackagesForUser(string.Empty);

        foreach (SdkTypes sdkId in Enum.GetValues<SdkTypes>())
        {
            IEnumerable<Package> sdkPackages = allPackages.Where(p => IsSdkName(p.Id, sdkId));

            foreach (Package package in sdkPackages.DistinctBy(p => p.Id.Version))
            {
                List<PackageRecord> sdkPackageRecords = new List<PackageRecord>();
                VersionRecord sdkVersion = CategorizePackageVersion(package.Id.Version, sdkId, versions);

                foreach (Package sdkPackage in sdkPackages.Where(p => p.Id.Version == package.Id.Version))
                {
                    PackageRecord sdkPackageRecord = new PackageRecord(sdkPackage, new List<PackageRecord>(), 0);
                    sdkPackageRecords.Add(sdkPackageRecord);

                    if (sdkPackage.IsFramework)
                        sdkLookUpTable[sdkPackage.Id.FullName] = sdkPackageRecord; // used to find dependents
                }

                sdks.Add(new SdkRecord(sdkVersion, sdkId, sdkPackageRecords));
            }
        }

        if (sdkLookUpTable.Count > 0)
            AddDependents(sdkLookUpTable, allPackages, 1);

        stopwatch.Stop();
        Trace.WriteLine($"GetSdks found {sdks.Count} SDKs, elapsed: {stopwatch.Elapsed.TotalSeconds} seconds");
        return sdks;
    }

    public static Task<List<SdkRecord>> GetSDKs()
    {
        return Task.Run(async () => GetSDKs(await GetVersionsList(), allUsers: IntegrityLevel.IsElevated()));
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
        const int cMinValidVersions = 44;

        if (sVersionList.Count > 0)
            return sVersionList;

        for (int i = 0; i < 2; i++)
        {
            try
            {
                string text = (i == 0) ? await ReadAllTextRemote() : await ReadAllTextLocal();

                if (!string.IsNullOrEmpty(text))
                {
                    JsonSerializerOptions jsOptions = new JsonSerializerOptions() { IncludeFields = true, };

                    List<VersionRecord>? versions = JsonSerializer.Deserialize<List<VersionRecord>>(text, jsOptions);

                    if ((versions is not null) && (versions.Count >= cMinValidVersions))
                    {
                        Debug.Assert(versions.DistinctBy(v => v.Release).Count() == versions.Count, "caution: duplicate package versions detected");
                        sVersionList = versions;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        Trace.WriteLine($"Versions list contains {sVersionList.Count} entries");
        return sVersionList;
    }

    private static async Task<string> ReadAllTextRemote()
    {
        try
        {
            using (HttpClient httpClient = new HttpClient())
            {
                const string path = "https://raw.githubusercontent.com/DHancock/WinAppSdkCleaner/main/WinAppSdkCleaner/versions.json";
                return await httpClient.GetStringAsync(path);
            }
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
}