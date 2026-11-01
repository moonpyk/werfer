using System.Text.Json.Serialization;

namespace SlClockify.Settings;

// Source-generated so the settings load and save under NativeAOT, where reflection-based JSON is disabled
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ApplicationSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext;
