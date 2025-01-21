using WinAppSdkCleaner.Models;

namespace WinAppSdkCleaner
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if ((args.Length == 2) && (args[0] == "/check_versions"))
            {
                return CheckCompressedVersionsFile(args[1]);
            }

            App application = new App();
            application.InitializeComponent();
            return application.Run();
        }

        private static int CheckCompressedVersionsFile(string projectDir)
        {
             //Debugger.Launch();
      
            try
            {
                string jsonPath = Path.Join(projectDir, "versions.json");
                string dataPath = Path.Join(projectDir, "versions.dat");

                List<VersionRecord> jsonVersions = ReadJsonFile(jsonPath);

                if (File.Exists(dataPath))
                {
                    List<VersionRecord> dataVersions = ReadDataFile(dataPath);

                    if (Enumerable.SequenceEqual(jsonVersions, dataVersions))
                    {
                        return 0;
                    }
                }

                WriteDataFile(dataPath, jsonVersions);
                return 0;
            }
            catch (Exception ex)
            {                     
                Console.WriteLine($"ERROR: post build event failed {ex}");
            }

            return 1;
        }

        private static void WriteDataFile(string outputFile, List<VersionRecord> versions)
        {
            using (FileStream fs = File.Create(outputFile))
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
                using (DeflateStream stream = new DeflateStream(fs, CompressionMode.Decompress))
                {
                    return (List<VersionRecord>?)JsonSerializer.Deserialize(stream, typeof(List<VersionRecord>), VersionRecordListJsonSerializerContext.Default) ?? new();
                }
            }
        }

        private static List<VersionRecord> ReadJsonFile(string jsonPath)
        {
            using (FileStream stream = File.OpenRead(jsonPath))
            {
                return (List<VersionRecord>?)JsonSerializer.Deserialize(stream, typeof(List<VersionRecord>), VersionRecordListJsonSerializerContext.Default) ?? new();
            }
        }
    }
}
