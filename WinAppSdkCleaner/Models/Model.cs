using WinAppSdkCleaner.Utilites;

namespace WinAppSdkCleaner.Models;

internal static class Model
{
    private static readonly Dictionary<int, VersionRecord> sVersionsLookUp = new();

    public static event EventHandler? VersionsLoaded;

    private static void OnVersionsLoaded(EventArgs e)
    {
        VersionsLoaded?.Invoke(null, e);
    }

    private static bool IsMicrosoftPublisher(PackageId id)
    {
        return string.Equals(id.PublisherId, "8wekyb3d8bbwe", StringComparison.Ordinal);
    }

    public static IEnumerable<VersionRecord> VersionsList => sVersionsLookUp.Values;

    public static VersionRecord CategorizePackageVersion(SdkId sdkId, PackageVersion packageVersion)
    {
        if (sVersionsLookUp.TryGetValue(MakeKey(sdkId, packageVersion), out VersionRecord? versionRecord))
        {
            Debug.Assert(versionRecord is not null);
            return versionRecord;
        }

        return new VersionRecord(string.Empty, string.Empty, sdkId, packageVersion);
    }

    private static void AddDependents(Dictionary<string, PackageData> sdkFrameworksLookUpTable, IEnumerable<Package> allPackages)
    {
        Lock lockObject = new();

        Parallel.ForEach(allPackages, package =>
        {
            foreach (Package dependency in package.Dependencies)
            {
                // TryGetValue() is thread safe as long as the dictionary isn't modified by another thread
                if (sdkFrameworksLookUpTable.TryGetValue(dependency.Id.FullName, out PackageData? parentPackageRecord))
                {
                    lock (lockObject)
                    {
                        PackageData dependentPackage = new PackageData(package, new List<PackageData>());
                        parentPackageRecord!.PackagesDependentOnThis.Add(dependentPackage);
                        Debug.Assert(!package.IsFramework);  // framework packages cannot be dependent on other framework packages
                    }
                }
            }
        });
    }

    public static async Task<IEnumerable<SdkData>> GetSDKsAsync()
    {
        Task<List<SdkData>> sdksTask = Task.Run(GetSDKPackages);
        Task versionsTask = Task.Run(GetVersionsListAsync);

        await Task.WhenAll(sdksTask, versionsTask);

        CategorizePackageVersions(sdksTask.Result);

        Trace.WriteLine($"Found {sdksTask.Result.Count} SDKs, all users is {IntegrityLevel.IsElevated}");

        return sdksTask.Result;
    }

    private static int MakeKey(SdkId sdkId, PackageVersion version)
    {
        return ((int)sdkId << 16 | version.Major) ^ (version.Minor << 16 | version.Build);
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

    public static IEnumerable<ISdk> SupportedSdks => [new ProjectReunion(), new WinAppSdk()];

    private static List<SdkData> GetSDKPackages()
    {
        List<SdkData> sdkList = new List<SdkData>();
        Dictionary<string, PackageData> lookUpTable = new Dictionary<string, PackageData>();

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

        foreach (ISdk sdk in SupportedSdks)
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
            CalculateDependentAppCounts(sdkList);
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

    private static void CalculateDependentAppCounts(IEnumerable<SdkData> sdkList)
    {
        foreach (ISdk sdk in SupportedSdks)
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



    private async static Task RemoveBatchAsync(IEnumerable<Package> packages)
    {
        const int cTimeoutPerPackage = 10 * 1000; // milliseconds

        if (packages.Any())
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                int milliSeconds = 0;
                List<Task> tasks = new List<Task>();

                foreach (Package package in packages)
                {
                    tasks.Add(RemoveAsync(package.Id.FullName, cts.Token));
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

    public async static Task RemovePackagesAsync(IEnumerable<Package> packages)
    {
        Trace.WriteLine($"{nameof(RemovePackagesAsync)} entry");
        Stopwatch stopwatch = Stopwatch.StartNew();

        // when removing for all users, any provisioned packages will also be removed
        await RemoveBatchAsync(packages.Where(p => !p.IsFramework));

        // now that the frameworks don't have any dependents
        await RemoveBatchAsync(packages.Where(p => p.IsFramework));

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

        foreach (Location location in Enum.GetValues<Location>())
        {
            try
            {
                List<VersionRecord>? versions = null;

                switch (location)
                {
#if DEBUG
                    case Location.FileSystem: versions = await ReadFromFileSystemAsync(); break;
#endif
                    case Location.OnLine: versions = await ReadFromOnLineAsync(); break;
                    case Location.Resource: versions = ReadFromResources(); break;
                }

                if (versions is not null)
                {
                    // While I could serialize a compressed json dictionary, for backward compatibility
                    // the json array is still needed. The extra complexity isn't worth it.
                    Debug.Assert(versions.Count > 0);

                    sVersionsLookUp.EnsureCapacity(versions.Count);

                    foreach (VersionRecord versionRecord in versions)
                    {
                        bool success = sVersionsLookUp.TryAdd(MakeKey(versionRecord.SdkId, versionRecord.Release), versionRecord);
                        Debug.Assert(success);
                    }

                    Trace.WriteLine($"Retrieved {sVersionsLookUp.Count} version records from: {location}");
                    break;
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

    private static async Task<List<VersionRecord>?> ReadFromOnLineAsync()
    {
        try
        {
            using (HttpClient httpClient = new HttpClient())
            {
                const string path = "https://raw.githubusercontent.com/DHancock/WinAppSdkCleaner/main/WinAppSdkCleaner/versions.dat";

                using (Stream s = await httpClient.GetStreamAsync(path))
                {
                    using (DeflateStream stream = new DeflateStream(s, CompressionMode.Decompress))
                    {
                        return JsonSerializer.Deserialize(stream, VersionRecordListJsonSerializerContext.Default.ListVersionRecord);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return null;
    }

    private static List<VersionRecord>? ReadFromResources()
    {
        try
        {
            using (Stream? rs = typeof(App).Assembly.GetManifestResourceStream("WinAppSdkCleaner.versions.dat"))
            {
                if (rs is not null)
                {
                    using (DeflateStream ds = new DeflateStream(rs, CompressionMode.Decompress))
                    {
                        return JsonSerializer.Deserialize(ds, VersionRecordListJsonSerializerContext.Default.ListVersionRecord);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return null;
    }

    private static async Task<List<VersionRecord>?> ReadFromFileSystemAsync()
    {
        try
        {
            string path = Path.Join(AppContext.BaseDirectory, "versions.dat");

            if (File.Exists(path))
            {
                await using (FileStream fs = File.OpenRead(path))
                {
                    using (DeflateStream ds = new DeflateStream(fs, CompressionMode.Decompress))
                    {
                        return JsonSerializer.Deserialize(ds, VersionRecordListJsonSerializerContext.Default.ListVersionRecord);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return null;
    }
}
