﻿// for IAsyncOperationWithProgress, contains conflicts with Rect, Point etc.
using Windows.Foundation;

namespace WinAppSdkCleaner.Models;

internal static class Model
{
    private static readonly AsyncLazy<IEnumerable<VersionRecord>> sVersionsProvider =
        new AsyncLazy<IEnumerable<VersionRecord>>(async () => await GetVersionsListAsync());

    private static readonly IEnumerable<ISdk> sdkTypes =
        new List<ISdk>() { new ProjectReunion(), new WinAppSdk() };


    private static bool IsMicrosoftPublisher(PackageId id)
    {
        return string.Equals(id.PublisherId, "8wekyb3d8bbwe", StringComparison.Ordinal);
    }

    private static async Task<VersionRecord> CategorizePackageVersionAsync(PackageVersion packageVersion, ISdk sdk)
    {
        IEnumerable<VersionRecord> versions = await sVersionsProvider;
        VersionRecord? versionRecord = versions.FirstOrDefault(v => v.SdkId == sdk.TypeId && v.Release == packageVersion);

        if (versionRecord is null)
            return new VersionRecord(string.Empty, string.Empty, string.Empty, sdk.TypeId, packageVersion);

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
                // TryGetValue() is thread safe as long as the dictionary isn't modified by another thread
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

    public static async Task<IEnumerable<SdkRecord>> GetSDKsAsync()
    {
        Trace.WriteLine($"{nameof(GetSDKsAsync)} entry, allUsers: {IntegrityLevel.IsElevated}");
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<SdkRecord> sdks = new List<SdkRecord>();
        Dictionary<string, PackageRecord> lookUpTable = new Dictionary<string, PackageRecord>();

        PackageManager packageManager = new PackageManager();
        IEnumerable<Package> allPackages;

        if (IntegrityLevel.IsElevated)
            allPackages = packageManager.FindPackages();
        else
            allPackages = packageManager.FindPackagesForUser(string.Empty);

        foreach (ISdk sdk in sdkTypes)
        {
            var query = from package in allPackages
                        where IsMicrosoftPublisher(package.Id) && sdk.Match(package.Id)
                        group package by package.Id.Version;

            foreach (IGrouping<PackageVersion, Package> group in query)
            {
                List<PackageRecord> packageRecords = new List<PackageRecord>();
                VersionRecord sdkVersion = await CategorizePackageVersionAsync(group.Key, sdk);

                foreach (Package package in group)
                {
                    PackageRecord packageRecord = new PackageRecord(package, new List<PackageRecord>(), 0);
                    packageRecords.Add(packageRecord);

                    if (package.IsFramework)
                        lookUpTable[package.Id.FullName] = packageRecord; // used to find dependents
                }

                sdks.Add(new SdkRecord(sdkVersion, sdk, packageRecords));
            }
        }

        if (lookUpTable.Count > 0)
            AddDependents(lookUpTable, allPackages, 1);

        stopwatch.Stop();
        Trace.WriteLine($"{nameof(GetSDKsAsync)} found {sdks.Count} SDKs, elapsed: {stopwatch.Elapsed.TotalSeconds} seconds");
        return sdks;
    }

    private async static Task RemoveAsync(string fullName, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            Trace.WriteLine($"Remove package: {fullName}");

            PackageManager packageManager = new PackageManager();
            IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation;

            using (ManualResetEventSlim opCompletedEvent = new ManualResetEventSlim(false))
            {
                if (IntegrityLevel.IsElevated)
                    deploymentOperation = packageManager.RemovePackageAsync(fullName, RemovalOptions.RemoveForAllUsers);
                else
                    deploymentOperation = packageManager.RemovePackageAsync(fullName);

                deploymentOperation.Completed = (depProgress, status) => 
                { 
                    opCompletedEvent.Set(); 
                };

                try
                {
                    opCompletedEvent.Wait(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Trace.WriteLine($"Removal of {fullName}, status: Wait() canceled due to time out");
                    return;
                }
            }

            Trace.WriteLine($"Removal of {fullName}, status: {deploymentOperation.Status}");

            if (deploymentOperation.Status == AsyncStatus.Error)
            {
                DeploymentResult deploymentResult = deploymentOperation.GetResults();
                Trace.WriteLine($"  {deploymentOperation.ErrorCode}");
                Trace.WriteLine($"  {deploymentResult.ErrorText}");
            }
        },
        CancellationToken.None);
    }

    private async static Task RemoveBatchAsync(IEnumerable<PackageRecord> packageRecords)
    {
        const int cTimeoutPerPackage = 10 * 1000; // milliseconds

        if (packageRecords.Any())
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                int milliSeconds = 0;
                List<Task> tasks = new List<Task>();

                foreach (PackageRecord packageRecord in packageRecords)
                {
                    tasks.Add(RemoveAsync(packageRecord.Package.Id.FullName, cts.Token));
                    milliSeconds += cTimeoutPerPackage;
                }

                Task timeOut = Task.Delay(milliSeconds);

                Task firstOut = await Task.WhenAny(Task.WhenAll(tasks), timeOut);

                if (firstOut == timeOut)
                {
                    cts.Cancel();
                    throw new TimeoutException($"Removal of sdk timed out after {milliSeconds / 1000} seconds");
                }
            }
        }
    }

    public async static Task RemovePackagesAsync(IEnumerable<PackageRecord> packageRecords)
    {
        Trace.WriteLine($"{nameof(RemovePackagesAsync)} entry");
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        // when removing for all users, any provisioned packages will also be removed
        await RemoveBatchAsync(packageRecords.Where(p => !p.Package.IsFramework));

        // now for the frameworks, this is more complicated because there may be
        // framework packages that depend on other frameworks (assuming that's even possible).
        var query = from packageRecord in packageRecords
                    where packageRecord.Package.IsFramework
                    orderby packageRecord.Depth descending
                    group packageRecord by packageRecord.Depth;

        foreach (IGrouping<int, PackageRecord> batch in query)
        {
            // remove batches of framework packages in order of depth, deepest first
            await RemoveBatchAsync(batch);
        }

        stopwatch.Stop();
        Trace.WriteLine($"{nameof(RemovePackagesAsync)}, elapsed: {stopwatch.Elapsed.TotalSeconds} seconds");
    }

    private static async Task<IEnumerable<VersionRecord>> GetVersionsListAsync()
    {
        Trace.WriteLine($"  {nameof(GetVersionsListAsync)} entry");
        Stopwatch stopwatch = Stopwatch.StartNew();

        const int cMinValidVersions = 44;
        List<VersionRecord> versionsList = new List<VersionRecord>();
        JsonSerializerOptions jsOptions = new JsonSerializerOptions() { IncludeFields = true, };

        for (int i = 0; i < 2; i++)
        {
            try
            {
                string text = (i == 0) ? await ReadAllTextRemoteAsync() : await ReadAllTextLocalAsync();

                if (!string.IsNullOrEmpty(text))
                {
                    List<VersionRecord>? versions = JsonSerializer.Deserialize<List<VersionRecord>>(text, jsOptions);

                    if ((versions is not null) && (versions.Count >= cMinValidVersions))
                    {
                        Debug.Assert(versions.DistinctBy(v => v.Release).Count() == versions.Count, "caution: duplicate package versions detected");
                        versionsList = versions;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        stopwatch.Stop();
        Trace.WriteLine($"  {nameof(GetVersionsListAsync)} found {versionsList.Count} versions, elapsed: {stopwatch.Elapsed.TotalSeconds} seconds");
        return versionsList;
    }

    private static async Task<string> ReadAllTextRemoteAsync()
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

    private static async Task<string> ReadAllTextLocalAsync()
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

    // Thread safe asynchronous lazy initialization 
    // based on the following:
    // https://devblogs.microsoft.com/pfxteam/asynclazyt/?WT.mc_id=DT-MVP-5000058
    // https://blog.stephencleary.com/2012/08/asynchronous-lazy-initialization.html

    private sealed class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) : base(() => Task.Run(valueFactory))
        {
        }

        public AsyncLazy(Func<Task<T>> taskFactory) : base(() => Task.Run(taskFactory))
        {
        }

        public TaskAwaiter<T> GetAwaiter() => Value.GetAwaiter(); 
    }
}