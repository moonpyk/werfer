using System.Runtime.Versioning;
using System.Security.Cryptography;
using Werfer.DataProtection;

namespace Werfer.Utils;

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
