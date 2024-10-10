namespace WinAppSdkCleaner.Models;

internal sealed partial class Settings : ApplicationSettingsBase
{
    public static Settings Data = new Settings();

    private Settings() : base()
    {
    }

    // window state 

    [UserScopedSetting]
    [DefaultSettingValue("True")]
    public bool IsFirstRun
    {
        get => Getter<bool>();
        private set => Setter(value);
    }


    [UserScopedSetting]
    [DefaultSettingValue("Normal")]
    public WindowState WindowState
    {
        get => Getter<WindowState>();
        set => Setter(value);
    }


    [UserScopedSetting]
    [DefaultSettingValue("0,0,0,0")]
    public Rect RestoreBounds
    {
        get => Getter<Rect>();
        set => Setter(value);
    }

    private T Getter<T>([CallerMemberName] string name = "") => (T)this[name];

    private void Setter(object value, [CallerMemberName] string name = "") => this[name] = value;

    public override void Save()
    {
        IsFirstRun = false;
        base.Save();
    }
}

