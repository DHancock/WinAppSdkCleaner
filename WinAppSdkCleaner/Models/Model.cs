using WinAppSdkCleaner.Utilities;

namespace WinAppSdkCleaner.Models;

internal static class Model
{
    public static ISdk[] SupportedSdk => [new ProjectReunion(), new WinAppSdk()];

    private static readonly Dictionary<int, VersionRecord> sVersionsLookUp = new();
    private static readonly Dictionary<int, VersionRecord> sSingletonLookUp = new();

    static Model()
    {
        LoadVersions();
    }

    public static VersionRecord CategorizePackageVersion(SdkId sdkId, PackageVersion packageVersion, bool isSingleton)
    {
        if (isSingleton && sSingletonLookUp.TryGetValue(MakeKey(sdkId, packageVersion), out VersionRecord? singletonVersionRecord))
        {
            return singletonVersionRecord;
        }

        if (sVersionsLookUp.TryGetValue(MakeKey(sdkId, packageVersion), out VersionRecord? versionRecord))
        {
            return versionRecord;
        }

        // synthesize a version record
        return new VersionRecord(string.Empty, string.Empty, sdkId, packageVersion, default);
    }

    private static void AddDependents(Dictionary<string, PackageData> sdkFrameworksLookUpTable, IEnumerable<Package> allPackages)
    {
        Lock lockObject = new();

        Parallel.ForEach(allPackages, package =>
        {
            foreach (Package dependency in package.Dependencies)
            {
                if (sdkFrameworksLookUpTable.TryGetValue(dependency.Id.FullName, out PackageData? parentFramework))
                {
                    PackageData dependentPackage = new PackageData(package);

                    lock (lockObject)
                    {
                        parentFramework.Dependents.Add(dependentPackage);
                    }
                }
            }
        });
    }

    public static async Task<IEnumerable<SdkData>> GetSDKsAsync()
    {
        List<SdkData> sdkList = await Task.Run(GetSDKPackages);

        UpdateSdkVersions(sdkList);

        Trace.WriteLine($"Found {sdkList.Count} SDKs");

        return sdkList;
    }

    private static int MakeKey(SdkId sdkId, PackageVersion version)
    {
        /*
        PackageVersion.GetHashCode() cannot be used because the following package versions generate the
        same hash code, namely 6293. Two Reunion packages have these versions.

        int a = new PackageVersion(8000, 167, 1906, 0).GetHashCode();
        int b = new PackageVersion(8004, 256, 1745, 0).GetHashCode();

        Debug.Assert(a != b);
        */

        return HashCode.Combine(sdkId, version.Major, version.Minor, version.Build, version.Revision);
    }

    private static void UpdateSdkVersions(List<SdkData> sdkList)
    {
        foreach (SdkData sdk in sdkList)
        {
            int key = MakeKey(sdk.Sdk.Id, sdk.PackageVersion);

            if (!sVersionsLookUp.TryGetValue(key, out VersionRecord? versionRecord))
            {
                // Either synthesize for packages that have the same version as the frameworks
                // or if possible infer the version record when the sdk is using semantic versioning

                if ((sdk.PackageVersion.Major < 1000) && (sdk.Sdk.Id == SdkId.WinAppSdk)) 
                {
                    // Use the new WinAppSdk's semantic versioning. Assumes that:
                    // a) they won't be servicing < WinAppSdk 1.0 releases (reasonably safe)
                    // b) the singleton package version will always be Major + 8000 (may be ok, difficult to tell) 
                    //
                    // While the versions file will still need updating for backwards compatibility, users
                    // of this version going forward won't need it unless they service a non sematic sdk release

                    PackageVersion singletonVersion = sdk.PackageVersion with { Major = (ushort)(sdk.PackageVersion.Major + 8000) };
                    string semanticStr = VersionRecord.GetVersionStr(sdk.PackageVersion);
                    string versionTag = ExtractFrameworkVersionTag(sdk.FrameworkPackages[0].Package);

                    versionRecord = new(semanticStr, versionTag, sdk.Sdk.Id, sdk.PackageVersion, singletonVersion);

                    bool success = sSingletonLookUp.TryAdd(MakeKey(versionRecord.SdkId, versionRecord.Singleton), versionRecord);
                    Debug.Assert(success);
                }
                else
                {
                    Debug.Fail("failed to find non sematic sdk version in versions resource");
                    versionRecord = new("", ExtractFrameworkVersionTag(sdk.FrameworkPackages[0].Package), sdk.Sdk.Id, sdk.PackageVersion, default);
                }

                sVersionsLookUp.Add(key, versionRecord);
            }

            sdk.Version = versionRecord;
        }
    }

    public static string ExtractFrameworkVersionTag(Package package)
    {
        Debug.Assert(package.IsFramework);
        ReadOnlySpan<char> fullName = package.Id.FullName.AsSpan();

        string[] tags = { "preview", "experimental" };

        foreach (string tag in tags)
        {
            int index = fullName.IndexOf(tag, StringComparison.OrdinalIgnoreCase);

            if (index > 0)
            {
                int versionPart = fullName.Slice(index + tag.Length).IndexOf('_');
                Debug.Assert(versionPart > 0);

                if (versionPart > 0)
                {
                    StringBuilder sb = new(16);
                    sb.Append(tag);
                    sb.Append(' ');
                    sb.Append(fullName.Slice(index + tag.Length, versionPart));

                    return sb.ToString();
                }

                return tag;
            }
        }

        return string.Empty;
    }

    private static List<SdkData> GetSDKPackages()
    {
        List<SdkData> sdkList = new();
        Dictionary<string, PackageData> lookUpTable = new();

        PackageManager packageManager = new();
        IEnumerable<Package> allPackages;

        if (IntegrityLevel.IsElevated)
        {
            allPackages = packageManager.FindPackages();
        }
        else
        {
            allPackages = packageManager.FindPackagesForUser(string.Empty);
        }

        foreach (ISdk sdk in SupportedSdk)
        {
            IEnumerable<IGrouping<PackageVersion, Package>> query;

            query = from package in allPackages
                    where package.IsFramework && (package.SignatureKind != PackageSignatureKind.System) && sdk.IsMatch(package.Id)
                    group package by package.Id.Version;

            foreach (IGrouping<PackageVersion, Package> group in query)
            {
                List<PackageData> packageList = new();

                foreach (Package package in group)  // assumes that the x86 and x64 framework packages have the same version
                {
                    // check that it's not a staged package
                    if (IntegrityLevel.IsElevated && !IsInstalled(package, packageManager.FindUsers(package.Id.FullName)))
                    {
                        continue;
                    }

                    PackageData packageData = new PackageData(package);
                    packageList.Add(packageData);

                    lookUpTable[package.Id.FullName] = packageData; // used to find dependents
                }

                if (packageList.Count > 0)
                {
                    sdkList.Add(new SdkData(sdk, packageVersion: group.Key, packageList));
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
        foreach (ISdk sdk in SupportedSdk)
        {
            foreach (SdkData sdkData in sdkList)
            {
                if (sdkData.Sdk.Id == sdk.Id)
                {
                    sdkData.OtherAppsCount = IdentifyOtherApps(sdk, sdkData.FrameworkPackages);
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

            if (!sdk.IsMatch(packageData.Package.Id))  // must be a leaf node
            {
                count += 1;
            }
            else if (packageData.Dependents.Count > 0)
            {
                count += IdentifyOtherApps(sdk, packageData.Dependents);
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

    private static void LoadVersions()
    {
#if !DEBUG
        BuildStaticLookUpTables();
#else
        List<VersionRecord>? versions = ReadFromFileSystem();

        if (versions is not null)
        {
            // The json array is still needed for backward compatibility
            Debug.Assert(versions.Count > 0);
            Debug.WriteLine($"Found {versions.Count} version records");

            //SaveTxt(versions);

            foreach (VersionRecord versionRecord in versions)
            {
                bool success = sVersionsLookUp.TryAdd(MakeKey(versionRecord.SdkId, versionRecord.Release), versionRecord);
                Debug.Assert(success);

                if (versionRecord.Singleton != default)
                {
                    // from WinAppSdk "2.0.0 preview 2" and "2.0.0 experimental 7" the singleton package has it's own package version
                    // presumably to avoid any package versioning problems related to the new semantic versioning scheme
                    success = sSingletonLookUp.TryAdd(MakeKey(versionRecord.SdkId, versionRecord.Singleton), versionRecord);
                    Debug.Assert(success);
                }
            }
        } 
#endif
    }

    private static void BuildStaticLookUpTables()
    {
        sVersionsLookUp.EnsureCapacity(135);
        sSingletonLookUp.EnsureCapacity(2);

        VersionRecord vr = new VersionRecord("0.1.0", "", SdkId.Reunion, new PackageVersion(0, 12012, 9000, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.5.0", "prerelease", SdkId.Reunion, new PackageVersion(0, 52103, 9000, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.5.0", "", SdkId.Reunion, new PackageVersion(0, 52103, 25000, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.5.5", "", SdkId.Reunion, new PackageVersion(0, 52104, 15000, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.5.6", "", SdkId.Reunion, new PackageVersion(0, 52104, 23000, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.5.7", "", SdkId.Reunion, new PackageVersion(0, 52105, 10000, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.5.9", "", SdkId.Reunion, new PackageVersion(0, 52107, 26000, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.0", "preview", SdkId.Reunion, new PackageVersion(8000, 146, 628, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.0", "", SdkId.Reunion, new PackageVersion(8000, 167, 1906, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.1", "", SdkId.Reunion, new PackageVersion(8001, 186, 2159, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.2", "", SdkId.Reunion, new PackageVersion(8002, 222, 1805, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.3", "", SdkId.Reunion, new PackageVersion(8003, 236, 617, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.4", "", SdkId.Reunion, new PackageVersion(8004, 256, 1745, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.5", "", SdkId.Reunion, new PackageVersion(8005, 278, 2204, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.6", "preview 1", SdkId.Reunion, new PackageVersion(8006, 312, 210, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.6", "", SdkId.Reunion, new PackageVersion(8006, 337, 137, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.7", "preview 1", SdkId.Reunion, new PackageVersion(8007, 392, 1938, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.7", "", SdkId.Reunion, new PackageVersion(8007, 444, 1739, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.8", "", SdkId.Reunion, new PackageVersion(8008, 474, 6, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.10", "", SdkId.Reunion, new PackageVersion(8010, 517, 1632, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.11", "", SdkId.Reunion, new PackageVersion(8011, 562, 1742, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("0.8.12", "", SdkId.Reunion, new PackageVersion(8012, 576, 2303, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.Reunion, vr.Release), vr);
        vr = new VersionRecord("1.0.0", "experimental 1", SdkId.WinAppSdk, new PackageVersion(0, 218, 840, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.0.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(0, 258, 2116, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.0.0", "preview 2", SdkId.WinAppSdk, new PackageVersion(0, 272, 1934, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.0.0", "preview 3", SdkId.WinAppSdk, new PackageVersion(0, 297, 2018, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.0.0", "", SdkId.WinAppSdk, new PackageVersion(0, 319, 455, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.0.1", "", SdkId.WinAppSdk, new PackageVersion(1, 440, 209, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.0.2", "", SdkId.WinAppSdk, new PackageVersion(2, 460, 358, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.0.3", "", SdkId.WinAppSdk, new PackageVersion(3, 469, 1654, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.0.4", "", SdkId.WinAppSdk, new PackageVersion(4, 528, 1755, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.1.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(1000, 447, 2307, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.1.0", "preview 2", SdkId.WinAppSdk, new PackageVersion(1000, 468, 658, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.1.0", "preview 3", SdkId.WinAppSdk, new PackageVersion(1000, 485, 2229, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.1.0", "", SdkId.WinAppSdk, new PackageVersion(1000, 516, 2156, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.1.1", "", SdkId.WinAppSdk, new PackageVersion(1001, 524, 1918, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.1.2", "", SdkId.WinAppSdk, new PackageVersion(1002, 543, 1943, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.1.3", "", SdkId.WinAppSdk, new PackageVersion(1003, 565, 600, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.1.4", "", SdkId.WinAppSdk, new PackageVersion(1004, 584, 2120, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.1.5", "", SdkId.WinAppSdk, new PackageVersion(1005, 616, 1651, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.0", "experimental 1", SdkId.WinAppSdk, new PackageVersion(2000, 572, 710, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.0", "experimental 2", SdkId.WinAppSdk, new PackageVersion(2000, 616, 1852, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(2000, 609, 1413, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.0", "preview 2", SdkId.WinAppSdk, new PackageVersion(2000, 638, 7, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.0", "", SdkId.WinAppSdk, new PackageVersion(2000, 677, 1750, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.1", "", SdkId.WinAppSdk, new PackageVersion(2000, 684, 1510, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.2", "", SdkId.WinAppSdk, new PackageVersion(2000, 707, 2303, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.3", "", SdkId.WinAppSdk, new PackageVersion(2000, 747, 1945, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.4", "", SdkId.WinAppSdk, new PackageVersion(2000, 777, 2143, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.2.5", "", SdkId.WinAppSdk, new PackageVersion(2000, 802, 31, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.3.0", "experimental 1", SdkId.WinAppSdk, new PackageVersion(3000, 763, 701, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.3.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(3000, 788, 1817, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.3.0", "", SdkId.WinAppSdk, new PackageVersion(3000, 820, 152, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.3.1", "", SdkId.WinAppSdk, new PackageVersion(3000, 851, 1712, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.3.2", "", SdkId.WinAppSdk, new PackageVersion(3000, 882, 2207, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.3.3", "", SdkId.WinAppSdk, new PackageVersion(3000, 934, 1904, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.0", "experimental 1", SdkId.WinAppSdk, new PackageVersion(4000, 868, 150, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(4000, 908, 1749, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.0", "preview 2", SdkId.WinAppSdk, new PackageVersion(4000, 952, 1501, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.0", "", SdkId.WinAppSdk, new PackageVersion(4000, 964, 11, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.1", "", SdkId.WinAppSdk, new PackageVersion(4000, 986, 611, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.2", "", SdkId.WinAppSdk, new PackageVersion(4000, 1010, 1349, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.3", "", SdkId.WinAppSdk, new PackageVersion(4000, 1049, 117, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.4", "", SdkId.WinAppSdk, new PackageVersion(4000, 1082, 2259, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.5", "", SdkId.WinAppSdk, new PackageVersion(4000, 1136, 2333, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.6", "", SdkId.WinAppSdk, new PackageVersion(4000, 1227, 1637, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.4.7", "", SdkId.WinAppSdk, new PackageVersion(4000, 1309, 2056, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.0", "experimental 1", SdkId.WinAppSdk, new PackageVersion(5000, 1066, 33, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.0", "experimental 2", SdkId.WinAppSdk, new PackageVersion(5000, 23, 1950, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(5000, 35, 2034, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.0", "", SdkId.WinAppSdk, new PackageVersion(5001, 58, 448, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.1", "", SdkId.WinAppSdk, new PackageVersion(5001, 70, 1338, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.2", "", SdkId.WinAppSdk, new PackageVersion(5001, 95, 533, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.3", "", SdkId.WinAppSdk, new PackageVersion(5001, 119, 156, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.4", "", SdkId.WinAppSdk, new PackageVersion(5001, 159, 55, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.5", "", SdkId.WinAppSdk, new PackageVersion(5001, 178, 1908, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.6", "", SdkId.WinAppSdk, new PackageVersion(5001, 214, 1843, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.7", "", SdkId.WinAppSdk, new PackageVersion(5001, 275, 500, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.8", "", SdkId.WinAppSdk, new PackageVersion(5001, 311, 2039, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.5.9", "", SdkId.WinAppSdk, new PackageVersion(5001, 373, 1736, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.0", "experimental 1", SdkId.WinAppSdk, new PackageVersion(6000, 152, 7, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.0", "experimental 2", SdkId.WinAppSdk, new PackageVersion(6000, 183, 650, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(6000, 219, 2254, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.0", "preview 2", SdkId.WinAppSdk, new PackageVersion(6000, 234, 357, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.0", "", SdkId.WinAppSdk, new PackageVersion(6000, 242, 101, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.1", "", SdkId.WinAppSdk, new PackageVersion(6000, 266, 2241, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.2", "", SdkId.WinAppSdk, new PackageVersion(6000, 311, 13, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.3", "", SdkId.WinAppSdk, new PackageVersion(6000, 318, 2304, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.4", "", SdkId.WinAppSdk, new PackageVersion(6000, 373, 1641, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.5", "", SdkId.WinAppSdk, new PackageVersion(6000, 401, 2352, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.6", "", SdkId.WinAppSdk, new PackageVersion(6000, 424, 1611, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.7", "", SdkId.WinAppSdk, new PackageVersion(6000, 457, 2140, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.8", "", SdkId.WinAppSdk, new PackageVersion(6000, 486, 517, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.6.9", "", SdkId.WinAppSdk, new PackageVersion(6000, 519, 329, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.0", "experimental 1", SdkId.WinAppSdk, new PackageVersion(7000, 319, 425, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.0", "experimental 2", SdkId.WinAppSdk, new PackageVersion(7000, 374, 1654, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.0", "experimental 3", SdkId.WinAppSdk, new PackageVersion(7000, 392, 2319, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(7000, 405, 208, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.0", "", SdkId.WinAppSdk, new PackageVersion(7000, 435, 154, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.1", "", SdkId.WinAppSdk, new PackageVersion(7000, 456, 1632, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.2", "", SdkId.WinAppSdk, new PackageVersion(7000, 498, 2246, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.3", "", SdkId.WinAppSdk, new PackageVersion(7000, 522, 1444, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.4", "", SdkId.WinAppSdk, new PackageVersion(7000, 617, 2103, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.5", "", SdkId.WinAppSdk, new PackageVersion(7000, 652, 1806, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.6", "", SdkId.WinAppSdk, new PackageVersion(7000, 676, 1651, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.7", "", SdkId.WinAppSdk, new PackageVersion(7000, 744, 1258, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.8", "", SdkId.WinAppSdk, new PackageVersion(7000, 770, 750, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.7.9", "", SdkId.WinAppSdk, new PackageVersion(7000, 785, 2325, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.0", "experimental 1", SdkId.WinAppSdk, new PackageVersion(8000, 466, 237, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.0", "experimental 2", SdkId.WinAppSdk, new PackageVersion(8000, 500, 1427, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.0", "experimental 3", SdkId.WinAppSdk, new PackageVersion(8000, 526, 1808, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.0", "experimental 4", SdkId.WinAppSdk, new PackageVersion(8000, 548, 2012, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(8000, 591, 1127, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.0", "", SdkId.WinAppSdk, new PackageVersion(8000, 616, 304, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.1", "", SdkId.WinAppSdk, new PackageVersion(8000, 625, 330, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.2", "", SdkId.WinAppSdk, new PackageVersion(8000, 642, 119, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.3", "", SdkId.WinAppSdk, new PackageVersion(8000, 675, 1142, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.4", "", SdkId.WinAppSdk, new PackageVersion(8000, 731, 1532, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.5", "", SdkId.WinAppSdk, new PackageVersion(8000, 770, 947, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.6", "", SdkId.WinAppSdk, new PackageVersion(8000, 806, 2252, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.7", "", SdkId.WinAppSdk, new PackageVersion(8000, 836, 2153, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.8", "", SdkId.WinAppSdk, new PackageVersion(8000, 859, 21, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.9", "", SdkId.WinAppSdk, new PackageVersion(8000, 879, 2017, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.10", "", SdkId.WinAppSdk, new PackageVersion(8000, 921, 1539, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.11", "", SdkId.WinAppSdk, new PackageVersion(8000, 946, 1701, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("1.8.12", "", SdkId.WinAppSdk, new PackageVersion(8000, 994, 2142, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("2.0.0", "experimental 1", SdkId.WinAppSdk, new PackageVersion(0, 638, 1631, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("2.0.0", "experimental 2", SdkId.WinAppSdk, new PackageVersion(0, 673, 119, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("2.0.0", "experimental 3", SdkId.WinAppSdk, new PackageVersion(0, 676, 658, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("2.0.0", "experimental 4", SdkId.WinAppSdk, new PackageVersion(0, 738, 2207, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("2.0.0", "experimental 5", SdkId.WinAppSdk, new PackageVersion(0, 770, 2319, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("2.0.0", "experimental 6", SdkId.WinAppSdk, new PackageVersion(0, 799, 456, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("2.0.0", "experimental 7", SdkId.WinAppSdk, new PackageVersion(2, 0, 0, 7), new PackageVersion(8002, 0, 0, 7));
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        sSingletonLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Singleton), vr);
        vr = new VersionRecord("2.0.0", "preview 1", SdkId.WinAppSdk, new PackageVersion(0, 772, 1552, 0), default);
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        vr = new VersionRecord("2.0.0", "preview 2", SdkId.WinAppSdk, new PackageVersion(2, 0, 0, 2), new PackageVersion(8002, 0, 0, 2));
        sVersionsLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Release), vr);
        sSingletonLookUp.Add(MakeKey(SdkId.WinAppSdk, vr.Singleton), vr);
    }



    private static List<VersionRecord>? ReadFromFileSystem()
    {
        try
        {
            string path = Path.Join(AppContext.BaseDirectory, "versions.dat");

            using (FileStream fs = File.OpenRead(path))
            {
                using (DeflateStream ds = new DeflateStream(fs, CompressionMode.Decompress))
                {
                    return JsonSerializer.Deserialize(ds, VersionRecordListJsonSerializerContext.Default.ListVersionRecord);
                }
            }
        }
        catch (FileNotFoundException)
        {
            Debug.WriteLine("file system versions.dat not found");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }

        return null;
    }

    public static async Task<Version> GetCurrentReleaseVersionAsync()
    {
        Version? release = null;

        try
        {
            using (HttpClient httpClient = new())
            {
                const string path = "https://raw.githubusercontent.com/DHancock/WinAppSdkCleaner/main/WinAppSdkCleaner/appversion.json";

                await using (Stream s = await httpClient.GetStreamAsync(path))
                {
                    release = await JsonSerializer.DeserializeAsync(s, VersionJsonSerializerContext.Default.Version);
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return release ?? new Version();
    }


    private static void SaveTxt(List<VersionRecord> versions)
    {
        StringBuilder sb = new StringBuilder();

        string path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "temp.txt");

        sb.AppendLine($"sVersionsLookUp.EnsureCapacity({versions.Count});");
        sb.AppendLine($"sSingletonLookUp.EnsureCapacity({versions.Count(x => x.Singleton != default)});");
        sb.AppendLine();
        sb.AppendLine("VersionRecord vr;");
        sb.AppendLine();

        foreach (VersionRecord vr in versions)
        {
            string singleton = (vr.Singleton == default) ? "default" :  $"new PackageVersion({vr.Singleton.Major}, {vr.Singleton.Minor}, {vr.Singleton.Build}, {vr.Singleton.Revision})";

            sb.AppendLine($"vr = new VersionRecord(\"{vr.SemanticVersion}\", \"{vr.VersionTag}\", SdkId.{vr.SdkId}, new PackageVersion({vr.Release.Major}, {vr.Release.Minor}, {vr.Release.Build}, {vr.Release.Revision}), {singleton});");
            sb.AppendLine($"sVersionsLookUp.Add(MakeKey(SdkId.{vr.SdkId}, vr.Release), vr);");

            if (vr.Singleton != default)
            {
                sb.AppendLine($"sSingletonLookUp.Add(MakeKey(SdkId.{vr.SdkId}, vr.Singleton), vr);");
            }
        }


        File.WriteAllText(path, sb.ToString());
    }
}
