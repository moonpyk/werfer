using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SlClockify.Core;

namespace SlClockify.Settings;

public class ApplicationSettings : IApplicationSettings
{
    private readonly byte[] _dataProtectionEntropy =
    [
        ..
        "ccgbslclockify2026"u8
    ];

    private const string SaveFileName
#if DEBUG
        = ".slclockify.dbg.json";
#else
        = ".slclockify.json";
#endif

    // Declared before Current: static initializers run in order and Current reads it
    private static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        SaveFileName
    );

    private static readonly Lazy<ApplicationSettings> LazyCurrent =
        new(() =>
        {
            if (!File.Exists(FilePath))
            {
                return new ApplicationSettings();
            }

            return JsonSerializer
                .Deserialize(File.ReadAllText(FilePath), SettingsJsonContext.Default.ApplicationSettings)!
                .LoadSecureStrings();
        });

    public static ApplicationSettings Current { get; } = LazyCurrent.Value;

    [JsonConstructor]
    internal ApplicationSettings()
    {
        // Laissé vide intentionnellement
    }

    [JsonPropertyName("api_key")]
    public byte[]? CryptedApiKey { get; set; }

    [JsonIgnore]
    public string? ApiKey
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            if (string.IsNullOrEmpty(field))
            {
                CryptedApiKey = null;
            }
            else
            {
                CryptedApiKey = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(field),
                    _dataProtectionEntropy,
                    DataProtectionScope.CurrentUser
                );
            }
        }
    }

    private ApplicationSettings LoadSecureStrings()
    {
        if (CryptedApiKey?.Length > 0 && string.IsNullOrEmpty(ApiKey))
        {
            ApiKey = Encoding.UTF8.GetString(
                ProtectedData.Unprotect(
                    CryptedApiKey,
                    _dataProtectionEntropy,
                    DataProtectionScope.CurrentUser
                )
            );
        }

        return this;
    }

    public void Save()
    {
        File.WriteAllText(
            FilePath,
            JsonSerializer.Serialize(this, SettingsJsonContext.Default.ApplicationSettings) + Environment.NewLine
        );
    }
}
