namespace Werfer.DataProtection;

/// <summary>
///     Provides a no-op implementation of the <see cref="IDataProtectionProvider" />
///     interface, where data protection and unprotection operations simply pass data through
///     without applying any transformations or security mechanisms.
/// </summary>
/// <remarks>
///     shortcut: outside Windows the API key is stored in clear, protected only by file mode 600, since every Linux
///     distribution/desktop environment protects secrets its own way; use the desktop's keyring (Secret Service on
///     Linux, Keychain on macOS) if users ask for it.
/// </remarks>
public class PassThroughDataProtectionProvider : IDataProtectionProvider
{
    private PassThroughDataProtectionProvider()
    {
    }

    /// <summary>
    ///     The only instance: it holds no state, registered as the data protection provider outside Windows
    /// </summary>
    public static IDataProtectionProvider Instance { get; } = new PassThroughDataProtectionProvider();

    /// <inheritdoc />
    public byte[] Protect(byte[] plaintext, byte[] entropy)
    {
        return plaintext;
    }

    /// <inheritdoc />
    public byte[] Unprotect(byte[] protectedData, byte[] entropy)
    {
        return protectedData;
    }
}
