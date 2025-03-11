using WinAppSdkCleaner.Models;

namespace VersionsGenerator;

internal class Program
{
    private const int error_success = 0;
    private const int error_failure = 1;

    static int Main(string[] args)
    {
        if (args.Length == 2)
        {
            return CheckCompressedVersionsFile(args[0], args[1]);
        }

        return error_failure;
    }

    private static int CheckCompressedVersionsFile(string jsonPath, string dataPath)
    {
        //System.Diagnostics.Debugger.Launch();

        try
        {
            // Always write the data file, it allows the VS fast up to date check to work.
            // That will limit the builds and hence the execution of this generator.
            
            WriteDataFile(dataPath, ReadJsonFile(jsonPath));
            return error_success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Post build event failed with {ex}");
        }

        return error_failure;
    }

    private static void WriteDataFile(string dataPath, List<VersionRecord> versions)
    {
        using (FileStream fs = File.Create(dataPath))
        {
            using (DeflateStream ds = new DeflateStream(fs, CompressionLevel.Optimal))
            {
                JsonSerializer.Serialize(ds, versions, typeof(List<VersionRecord>), VersionRecordListJsonSerializerContext.Default);
            }
        }
    }

    private static List<VersionRecord> ReadJsonFile(string jsonPath)
    {
        using (FileStream fs = File.OpenRead(jsonPath))
        {
            return (List<VersionRecord>?)JsonSerializer.Deserialize(fs, typeof(List<VersionRecord>), VersionRecordListJsonSerializerContext.Default) ?? new();
        }
    }
}
