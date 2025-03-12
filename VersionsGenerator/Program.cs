using WinAppSdkCleaner.Models;

namespace VersionsGenerator;

internal class Program
{
    static int Main(string[] args)
    {
        const int error_success = 0;
        const int error_failure = 1;

        try
        {
            UpdateCompressedVersionsDataFile(args[0], args[1]);
            return error_success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Post build event failed with {ex}");
        }

        return error_failure;
    }

    private static void UpdateCompressedVersionsDataFile(string jsonPath, string dataPath)
    {
        //System.Diagnostics.Debugger.Launch();

        // Always write the data file, it allows VS's fast up to date check to work.
        // That will limit the builds and hence the execution of this generator.
        // Github doesn't only rely on the file's modifcation date, it checks the file's contents.
        
        WriteDataFile(dataPath, ReadJsonFile(jsonPath));
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
