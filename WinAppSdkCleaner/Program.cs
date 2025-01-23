using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if ((args.Length == 3) && (args[0] == "/check_versions"))
            {
                return CheckCompressedVersionsFile(args[1], args[2]);
            }

            App application = new App();
            application.InitializeComponent();
            return application.Run();
        }

        private static int CheckCompressedVersionsFile(string jsonPath, string dataPath)
        {
            const int error_success = 0;
            const int error_failure = 1;

             //Debugger.Launch();
      
            try
            {
                List<VersionRecord> jsonVersions = ReadJsonFile(jsonPath);

                if (File.Exists(dataPath))
                {
                    List<VersionRecord> dataVersions = ReadDataFile(dataPath);

                    if (Enumerable.SequenceEqual(jsonVersions, dataVersions))
                    {
                        return error_success;
                    }
                }

                WriteDataFile(dataPath, jsonVersions);
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

        private static List<VersionRecord> ReadDataFile(string dataPath)
        {
            using (FileStream fs = File.OpenRead(dataPath))
            {
                using (DeflateStream ds = new DeflateStream(fs, CompressionMode.Decompress))
                {
                    return (List<VersionRecord>?)JsonSerializer.Deserialize(ds, typeof(List<VersionRecord>), VersionRecordListJsonSerializerContext.Default) ?? new();
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
}
