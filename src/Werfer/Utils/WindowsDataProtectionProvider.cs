using System.Runtime.Versioning;
using System.Security.Cryptography;
using Werfer.DataProtection;

namespace Werfer.Utils;

/// <summary>
///     Provides Windows-specific data protection using the Windows Data Protection API for the current user.
/// </summary>
/// <remarks>
///     This implementation protects and unprotects sensitive data through the operating system's user-scoped
///     cryptographic services. Data protected by this provider is tied to the current Windows user context and
///     is intended to be decrypted only by the same user account.
/// </remarks>
[SupportedOSPlatform("windows")]
public class WindowsDataProtectionProvider : IDataProtectionProvider
{
    /// <inheritdoc />
    public byte[] Protect(byte[] plaintext, byte[] entropy)
    {
        return ProtectedData.Protect(
            plaintext,
            DataProtectionScope.CurrentUser,
            entropy
        );
    }

    /// <inheritdoc />
    public byte[] Unprotect(byte[] protectedData, byte[] entropy)
    {
        return ProtectedData.Unprotect(
            protectedData,
            DataProtectionScope.CurrentUser,
            entropy
        );
    }
}
