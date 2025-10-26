namespace WinAppSdkCleaner.ViewModels;

// some what overkill just to save the window's position, but easily extendable
internal sealed partial class Settings
{
    private static readonly Lazy<Settings> lazy = new Lazy<Settings>(Load);

    public static Settings Instance => lazy.Value;

    public RectInt32 RestoreBounds { get; set; } = default;
    public bool SortAscending { get; set; } = true;

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

    private static Settings Load()
    {
        string path = GetSettingsFilePath();

        try
        {
            using (FileStream fs = File.OpenRead(path))
            {
                Settings? settings = JsonSerializer.Deserialize(fs, SettingsJsonContext.Default.Settings);

                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message);
        }

        return new Settings();
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
