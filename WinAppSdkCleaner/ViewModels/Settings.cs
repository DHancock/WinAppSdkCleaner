namespace WinAppSdkCleaner.ViewModels;

// some what overkill just to save the window's position, but easily extendable
internal sealed partial class Settings
{
    private static readonly Lazy<Settings> lazy = new Lazy<Settings>(() => 
    {
        Settings s = new Settings();
        s.Load();
        return s; 
    });

    public static Settings Instance => lazy.Value;

    public RectInt32 RestoreBounds { get; set; } = default;
    
    public Settings()  // required by json code generator
    {
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(App.GetAppDataPath());

            using (FileStream fs = File.Create(GetSettingsFilePath()))
            {
                JsonSerializer.Serialize(fs, this, SettingsJsonContext.Default.Settings);
            }
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }

    private void Load()
    {
        string path = GetSettingsFilePath();

        try
        {
            using (FileStream fs = File.OpenRead(path))
            {
                Settings? settings = JsonSerializer.Deserialize(fs, SettingsJsonContext.Default.Settings);

                if (settings is not null)
                {
                    RestoreBounds = settings.RestoreBounds;
                }
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message);
        }
    }

    private static string GetSettingsFilePath()
    {
        return Path.Join(App.GetAppDataPath(), "settings.json");
    }
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(Settings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext
{
}
