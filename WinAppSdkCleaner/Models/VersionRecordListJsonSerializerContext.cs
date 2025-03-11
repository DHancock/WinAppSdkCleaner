namespace WinAppSdkCleaner.Models;

[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = false)]
[JsonSerializable(typeof(List<VersionRecord>))]
internal sealed partial class VersionRecordListJsonSerializerContext : JsonSerializerContext
{
}
