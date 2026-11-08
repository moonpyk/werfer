using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Werfer.DataProtection;

namespace Werfer.Settings;

/// <summary>
///     Represents the global settings of the application.
/// </summary>
public partial class ApplicationSettings : IApplicationSettings
{
    private const string SaveFileName = "config.json";

    private readonly byte[] _dataProtectionEntropy =
    [
        ..
        "ccgbwerfer2026"u8
    ];

    private ILogger _logger = NullLogger.Instance;
    private PersistedSettings _persisted = new();

    private ApplicationSettings()
    {
        // Intentionally left empty
    }

    private static string FilePath { get; } = Path.Combine(
        App.ApplicationPath,
        SaveFileName
    );

    /// <summary>
    ///     Provides a singleton instance of the <see cref="ApplicationSettings" /> class.
    ///     This instance is used to access and manage global application settings in a centralized way.
    /// </summary>
    public static ApplicationSettings Current { get; } = new();

    private IDataProtectionProvider DataProtectionProvider { get; set; } = PassThroughDataProtectionProvider.Instance;

    /// <inheritdoc />
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
                _persisted.CryptedApiKey = null;
            }
            else
            {
                _persisted.CryptedApiKey = DataProtectionProvider.Protect(
                    Encoding.UTF8.GetBytes(field),
                    _dataProtectionEntropy
                );
            }
        }
    }

    /// <inheritdoc />
    public int MaxProjectChoices
    {
        get => _persisted.MaxProjectChoices;
        set => _persisted.MaxProjectChoices = value;
    }

    /// <inheritdoc />
    public bool Topmost
    {
        get => _persisted.Topmost;
        set => _persisted.Topmost = value;
    }

    /// <inheritdoc />
    public int? WindowX
    {
        get => _persisted.WindowX;
        set => _persisted.WindowX = value;
    }

    /// <inheritdoc />
    public int? WindowY
    {
        get => _persisted.WindowY;
        set => _persisted.WindowY = value;
    }

    /// <inheritdoc />
    public double? WindowWidth
    {
        get => _persisted.WindowWidth;
        set => _persisted.WindowWidth = value;
    }

    /// <inheritdoc />
    public double? WindowHeight
    {
        get => _persisted.WindowHeight;
        set => _persisted.WindowHeight = value;
    }

    /// <inheritdoc />
    public DayOfWeek WeekStart
    {
        get => _persisted.WeekStart ?? CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        set => _persisted.WeekStart = value;
    }

    /// <inheritdoc />
    public bool TrySave()
    {
        try
        {
            Directory.CreateDirectory(
                App.ApplicationPath
            );

            var sb = new StringBuilder();
            sb.Append(JsonSerializer.Serialize(
                _persisted,
                SettingsJsonContext.Default.PersistedSettings
            ));
            sb.AppendLine();

            File.WriteAllText(
                FilePath,
                sb.ToString()
            );

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not save settings to {FilePath}", FilePath);
        }

        return false;
    }

    /// <summary>
    ///     Loads the application settings from the persisted storage. If the settings file
    ///     does not exist, it returns the current instance without any changes. In case of
    ///     an error during the loading process, the error is logged, and the current instance
    ///     is returned.
    /// </summary>
    /// <return>
    ///     The current instance of <see cref="ApplicationSettings" /> with loaded values
    ///     or default values if the settings file is missing or could not be loaded.
    /// </return>
    public ApplicationSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return this;
        }

        try
        {
            // One assignment swaps every persisted value at once
            _persisted = JsonSerializer.Deserialize(
                File.ReadAllText(FilePath),
                SettingsJsonContext.Default.PersistedSettings
            ) ?? throw new JsonException("Settings file holds null");

            return LoadSecureStrings();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "While loading settings");
            return this;
        }
    }

    /// <summary>
    ///     Sets the logger used from now on, call it before Load so load errors are logged.
    /// </summary>
    public ApplicationSettings WithLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    /// <summary>
    ///     Decrypts and loads secure string properties, such as the API key, from the persisted
    ///     settings into the current instance. If the encrypted data exists and the corresponding
    ///     property in memory is not already set, the method decrypts and assigns the value.
    /// </summary>
    /// <return>
    ///     The current instance of <see cref="ApplicationSettings" /> with updated secure string
    ///     properties decrypted and loaded into memory.
    /// </return>
    private ApplicationSettings LoadSecureStrings()
    {
        if (_persisted.CryptedApiKey?.Length > 0 && string.IsNullOrEmpty(ApiKey))
        {
            ApiKey = Encoding.UTF8.GetString(
                DataProtectionProvider.Unprotect(
                    _persisted.CryptedApiKey,
                    _dataProtectionEntropy
                )
            );
        }

        return this;
    }

    /// <summary>
    ///     Configures the application settings to use the specified data protection provider.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="provider" /> parameter is null.
    /// </exception>
    public ApplicationSettings WithDataProtectionProvider(IDataProtectionProvider provider)
    {
        DataProtectionProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        return this;
    }

    // Everything written to the settings file, and nothing else
    private class PersistedSettings
    {
        [JsonPropertyName("api_key")]
        public byte[]? CryptedApiKey { get; set; }

        [JsonPropertyName("projects:max_choices")]
        [DefaultValue(10)]
        public int MaxProjectChoices { get; set; } = 10;

        [JsonPropertyName("window:topmost")]
        [DefaultValue(false)]
        public bool Topmost { get; set; }

        [JsonPropertyName("window:x")]
        public int? WindowX { get; set; }

        [JsonPropertyName("window:y")]
        public int? WindowY { get; set; }

        [JsonPropertyName("window:width")]
        public double? WindowWidth { get; set; }

        [JsonPropertyName("window:height")]
        public double? WindowHeight { get; set; }

        // Null until picked in the settings, so a culture change still applies
        [JsonPropertyName("history:week_start")]
        public DayOfWeek? WeekStart { get; set; }
    }

    // Nested so it can see the private PersistedSettings; source-generated because reflection-based JSON is off under NativeAOT
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(PersistedSettings))]
    private partial class SettingsJsonContext : JsonSerializerContext;
}
