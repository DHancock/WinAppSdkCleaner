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

                JsonSerializerOptions jsOptions = new JsonSerializerOptions() { IncludeFields = true, WriteIndented = false };

                List<Models.VersionRecord> jsonVersions = ReadJsonFile(jsonPath, jsOptions);

                if (File.Exists(dataPath))
                {
                    List<Models.VersionRecord> dataVersions = ReadDataFile(dataPath, jsOptions);

                    if (jsonVersions.Count == dataVersions.Count)
                    {
                        bool equal = true;

                        for (int index = 0; index < jsonVersions.Count; index++)
                        {
                            if ((jsonVersions[index] != dataVersions[index]) && !jsonVersions.Contains(dataVersions[index]))
                            {
                                equal = false;
                                break;
                            }
                        }

                        if (equal)
                        {
                            return 0;
                        }
                    }
                }

                WriteDataFile(dataPath, jsonVersions, jsOptions);
                return 0;
            }
            catch (Exception ex)
            {                     
                Console.WriteLine($"ERROR: post build event failed {ex}");
            }

            return 1;
        }

        private static void WriteDataFile(string outputFile, List<Models.VersionRecord> versions, JsonSerializerOptions jsOptions)
        {
            using (FileStream fs = File.Create(outputFile))
            {
                MemoryStream ms = new MemoryStream();
                JsonSerializer.Serialize(ms, versions, jsOptions);

                using (DeflateStream ds = new DeflateStream(fs, CompressionLevel.Optimal))
                {
                    ms.WriteTo(ds);
                }
            }
        }

        private static List<Models.VersionRecord> ReadDataFile(string dataPath, JsonSerializerOptions jsOptions)
        {
            using (FileStream fs = File.OpenRead(dataPath))
            {
                using (DeflateStream stream = new DeflateStream(fs, CompressionMode.Decompress))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        string text = sr.ReadToEnd();
                        return JsonSerializer.Deserialize<List<Models.VersionRecord>>(text, jsOptions) ?? new();
                    }
                }
            }
        }

        private static List<Models.VersionRecord> ReadJsonFile(string jsonPath, JsonSerializerOptions jsOptions)
        {
            string text = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<List<Models.VersionRecord>>(text, jsOptions) ?? new();
        }
    }
}
