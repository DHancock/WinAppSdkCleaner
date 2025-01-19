// for IAsyncOperationWithProgress, contains conflicts with Rect, Point etc.
using Windows.Foundation;

namespace WinAppSdkCleaner.Models;

internal static class Model
{
    private static readonly Dictionary<ulong, VersionRecord> sVersionsLookUp = new();

    public static event EventHandler? VersionsLoaded;

    private static void OnVersionsLoaded(EventArgs e)
    {
        VersionsLoaded?.Invoke(null, e);
    }

    private static bool IsMicrosoftPublisher(PackageId id)
    {
        return string.Equals(id.PublisherId, "8wekyb3d8bbwe", StringComparison.Ordinal);
    }

    public static IEnumerable<VersionRecord> FilterVersionsList(SdkId sdkId)
    {
        return sVersionsLookUp.Values.Where(v => v.SdkId == sdkId);
    }

    public static VersionRecord CategorizePackageVersion(SdkId sdkId, PackageVersion packageVersion)
    {
        if (sVersionsLookUp.TryGetValue(MakeKey(sdkId, packageVersion), out VersionRecord? versionRecord))
        {
            Debug.Assert(versionRecord is not null);
            return versionRecord;
        }

        return new VersionRecord(string.Empty, string.Empty, sdkId, packageVersion);
    }

    private static void AddDependents(Dictionary<string, PackageData> lookUpTable, IEnumerable<Package> allPackages)
    {
        object lockObject = new object();
        Dictionary<string, PackageData> subLookUp = new Dictionary<string, PackageData>();

        Parallel.ForEach(allPackages, package =>
        {
            foreach (Package dependency in package.Dependencies)
            {
                // TryGetValue() is thread safe as long as the dictionary isn't modified by another thread
                if (lookUpTable.TryGetValue(dependency.Id.FullName, out PackageData? parentPackageRecord))
                {
                    lock (lockObject)
                    {
                        PackageData dependentPackage = new PackageData(package, new List<PackageData>());
                        parentPackageRecord!.PackagesDependentOnThis.Add(dependentPackage);

                        if (package.IsFramework)
                        {
                            subLookUp[package.Id.FullName] = dependentPackage;
                        }
                    }
                }
            }
        });

        if (subLookUp.Count > 0)
        {
            AddDependents(subLookUp, allPackages);
        }
    }

    public static async Task<IEnumerable<SdkData>> GetSDKsAsync()
    {
        Task<List<SdkData>> sdksTask = Task.Run(GetSDKPackages);
        Task versionsTask = Task.Run(GetVersionsListAsync);

        await Task.WhenAll(sdksTask, versionsTask);

        CategorizePackageVersions(sdksTask.Result);

        Trace.WriteLine($"Found {sdksTask.Result.Count} SDKs, allUsers: {IntegrityLevel.IsElevated}");

        return sdksTask.Result;
    }

    private static ulong MakeKey(SdkId sdkId, PackageVersion version)
    {
        return (ulong)sdkId << 48 | (ulong)version.Major << 32 | (ulong)version.Minor << 16 | version.Build ;
    }

    private static void CategorizePackageVersions(IEnumerable<SdkData> sdks)
    {
        foreach (SdkData sdk in sdks)
        {
            if (sVersionsLookUp.TryGetValue(MakeKey(sdk.Sdk.Id, sdk.Version.Release), out VersionRecord? versionRecord))
            {
                Debug.Assert(versionRecord is not null);
                sdk.Version = versionRecord;
            }
        }
    }

    private static List<SdkData> GetSDKPackages()
    {
        List<SdkData> sdkList = new List<SdkData>();
        Dictionary<string, PackageData> lookUpTable = new Dictionary<string, PackageData>();
        List<ISdk> sdkTypes = [new ProjectReunion(), new WinAppSdk()];

        PackageManager packageManager = new PackageManager();
        IEnumerable<Package> allPackages;

        if (IntegrityLevel.IsElevated)
        {
            allPackages = packageManager.FindPackages();
        }
        else
        {
            allPackages = packageManager.FindPackagesForUser(string.Empty);
        }

        foreach (ISdk sdk in sdkTypes)
        {
            IEnumerable<IGrouping<PackageVersion, Package>> query;
            
            query = from package in allPackages
                    where (package.SignatureKind != PackageSignatureKind.System) && IsMicrosoftPublisher(package.Id) && sdk.Match(package.Id)
                    group package by package.Id.Version;

            foreach (IGrouping<PackageVersion, Package> group in query)
            {
                List<PackageData> packageList = new List<PackageData>();

                foreach (Package package in group)
                {
                    // check that it's not a staged package
                    if (IntegrityLevel.IsElevated && !IsInstalled(package, packageManager.FindUsers(package.Id.FullName)))
                    {
                        continue;
                    }

                    PackageData packageData = new PackageData(package, new List<PackageData>());
                    packageList.Add(packageData);

                    if (package.IsFramework)
                    {
                        lookUpTable[package.Id.FullName] = packageData; // used to find dependents
                    }
                }

                if (packageList.Count > 0)
                {
                    VersionRecord version = new VersionRecord(string.Empty, string.Empty, sdk.Id, Release: group.Key);
                    sdkList.Add(new SdkData(version, sdk, packageList));
                }
            }
        }

        if (lookUpTable.Count > 0)
        {
            AddDependents(lookUpTable, allPackages);
            CalculateDependentAppCounts(sdkTypes, sdkList);
        }

        return sdkList;
    }

    private static bool IsInstalled(Package package, IEnumerable<PackageUserInformation> collection)
    {
        Debug.Assert(collection.Count() == 1);
        PackageUserInformation? userInfo = collection.FirstOrDefault();

        if (userInfo is not null)
        {
            if (userInfo.InstallState == PackageInstallState.Installed)
            {
                return true;
            }

            // It's most likely that the framework package's install state has been converted to "Staged" by the package manager
            // when it was removed for some (all?) users. Staged packages cannot be deleted by this program, so omit it from the results. 
            // The "Staged" packages do seem to be automatically deleted after some time (reboot?) so I assume it's a temporary cached state. 
            Trace.WriteLine($"\tomitting package: {package.Id.FullName} install state: {userInfo.InstallState} sid: {userInfo.UserSecurityId}");
        }
        else
        {
            Trace.WriteLine($"\tomitting package: {package.Id.FullName} - unable to determine package install state");
        }

        return false;
    }

    private static void CalculateDependentAppCounts(IEnumerable<ISdk> sdkTypes, IEnumerable<SdkData> sdkList)
    {
        foreach (ISdk sdk in sdkTypes)
        {
            foreach (SdkData sdkData in sdkList)
            {
                if (sdkData.Sdk.Id == sdk.Id)
                {
                    sdkData.OtherAppsCount = IdentifyOtherApps(sdk, sdkData.SdkPackages);
                }
            }
        }
    }

    private static int IdentifyOtherApps(ISdk sdk, List<PackageData> packageList)
    {
        int total = 0;

        foreach (PackageData packageData in packageList)
        {
            int count = 0;

            if (!(sdk.Match(packageData.Package.Id) && IsMicrosoftPublisher(packageData.Package.Id)))
            {
                count += 1;
            }

            if (packageData.PackagesDependentOnThis.Count > 0)
            {
                count += IdentifyOtherApps(sdk, packageData.PackagesDependentOnThis);
            }

            total += count;
            packageData.OtherAppsCount = count;
        }

        return total;
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
                {
                    deploymentOperation = packageManager.RemovePackageAsync(fullName, RemovalOptions.RemoveForAllUsers);
                }
                else
                {
                    deploymentOperation = packageManager.RemovePackageAsync(fullName);
                }

                deploymentOperation.Completed = (depProgress, status) =>
                {
                    // status errors are processed later
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

            // concatenate trace lines and write once otherwise they could be interleaved with another tasks
            StringBuilder sb = new StringBuilder();

            try
            {
                sb.AppendLine($"Removal of {fullName}, status: {deploymentOperation.Status}");

                if (deploymentOperation.Status == AsyncStatus.Error)
                {
                    DeploymentResult deploymentResult = deploymentOperation.GetResults();
                    sb.AppendLine($"\tError Text:{deploymentResult.ErrorText}");
                    sb.AppendLine($"\tException: {deploymentOperation.ErrorCode}");

                    if ((deploymentOperation.ErrorCode is COMException cex) && ((UInt32)cex.ErrorCode == 0x80073CF3))
                    {
                        sb.AppendLine($"\t\tError 0x80073CF3 usually means that an app, installed on a different user account has a dependency on this framework package.");
                        sb.AppendLine($"\t\tUnfortunately the PackageManager will only list an app's dependencies for the current user, even when specifying all users.");
                        sb.AppendLine($"\t\tAs the app's dependency on this framework package cannot be determined, that app cannot be removed first.");
                        sb.AppendLine($"\t\tIt can also occur if a packaged app crashes while the WinAppSdk is being installed as a package dependency.");

                        if (IntegrityLevel.IsElevated)  // FindUsers() requires elevation
                        {
                            List<string> users = new List<string>(packageManager.FindUsers(fullName).Select(pui => pui.UserSecurityId));

                            if (users.Count > 0)
                            {
                                sb.AppendLine($"\t\tThere {(users.Count > 1 ? $"are {users.Count} users" : "is 1 user")} registered for this package: ");

                                foreach (string sid in users)
                                {
                                    try
                                    {
                                        sb.AppendLine($"\t\t{new SecurityIdentifier(sid).Translate(typeof(NTAccount))}\t{sid}");
                                    }
                                    catch
                                    {
                                        sb.AppendLine($"\t\t{sid}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Trace.Write(sb.ToString());
            }
        },
        CancellationToken.None);
    }



    private async static Task RemoveBatchAsync(IEnumerable<PackageData> packageRecords)
    {
        const int cTimeoutPerPackage = 10 * 1000; // milliseconds

        if (packageRecords.Any())
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                int milliSeconds = 0;
                List<Task> tasks = new List<Task>();

                foreach (PackageData packageRecord in packageRecords)
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

    public async static Task RemovePackagesAsync(IEnumerable<PackageData> packageRecords)
    {
        Trace.WriteLine($"{nameof(RemovePackagesAsync)} entry");
        Stopwatch stopwatch = Stopwatch.StartNew();

        // when removing for all users, any provisioned packages will also be removed
        await RemoveBatchAsync(packageRecords.Where(p => !p.Package.IsFramework));

        // now that the frameworks don't have any dependents
        await RemoveBatchAsync(packageRecords.Where(p => p.Package.IsFramework));

        stopwatch.Stop();
        Trace.WriteLine($"{nameof(RemovePackagesAsync)}, elapsed: {stopwatch.Elapsed.TotalSeconds} seconds");
    }

    enum Location { FileSystem, OnLine, Resource };

    private static async Task GetVersionsListAsync()
    {
        // This function is only called from code running on the ui thread. That code has a 
        // reentrancy guard so the versions look up dictionary will only ever be empty or full.
        if (sVersionsLookUp.Count > 0)
        {
            return;
        }

        JsonSerializerOptions jsOptions = new JsonSerializerOptions() { IncludeFields = true };

        foreach (Location location in Enum.GetValues<Location>())
        {
            try
            {
                string text = string.Empty;

                switch (location)
                {
                    case Location.FileSystem: text = await ReadAllFileSystemTextAsync(); break;
                    case Location.OnLine: text = await ReadAllOnLineTextAsync(); break;
                    case Location.Resource: text = await ReadAllResourceTextAsync(); break;
                }

                if (!string.IsNullOrEmpty(text))
                {
                    List<VersionRecord>? versions = JsonSerializer.Deserialize<List<VersionRecord>>(text, jsOptions);

                    if (versions is not null)
                    {
                        Debug.Assert(versions.DistinctBy(v => v.Release).Count() == versions.Count, "caution: duplicate package versions detected");

                        foreach (VersionRecord versionRecord in versions)
                        {
                            Debug.Assert(!sVersionsLookUp.ContainsKey(MakeKey(versionRecord.SdkId, versionRecord.Release)));

                            sVersionsLookUp.Add(MakeKey(versionRecord.SdkId, versionRecord.Release), versionRecord);
                        }

                        Trace.WriteLine($"Retrieved {sVersionsLookUp.Count} version records from: {location}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        if (sVersionsLookUp.Count > 0)
        {
            OnVersionsLoaded(new EventArgs());
        }
    }

    private static async Task<string> ReadAllOnLineTextAsync()
    {
        try
        {
            using (HttpClient httpClient = new HttpClient())
            {
                const string path = "https://raw.githubusercontent.com/DHancock/WinAppSdkCleaner/dev/WinAppSdkCleaner/versions.dat";

                using (Stream s = await httpClient.GetStreamAsync(path))
                {
                    using (DeflateStream stream = new DeflateStream(s, CompressionMode.Decompress))
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            return sr.ReadToEnd();
                        }
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

    private static async Task<string> ReadAllResourceTextAsync()
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

    private static async Task<string> ReadAllFileSystemTextAsync()
    {
        try
        {
            string path = Path.Join(AppContext.BaseDirectory, "versions.json");

            if (File.Exists(path)) // probably won't, it isn't installed by default
            {
                return await File.ReadAllTextAsync(path);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return string.Empty;
    }
}