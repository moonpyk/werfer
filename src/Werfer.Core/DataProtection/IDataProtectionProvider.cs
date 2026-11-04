namespace Werfer.DataProtection;

/// <summary>
///     Defines a contract for protecting and unprotecting sensitive data.
/// </summary>
public interface IDataProtectionProvider
{
    /// <summary>
    ///     Protects the specified plaintext data by applying a protection mechanism,
    ///     optionally using the provided entropy to enhance security.
    /// </summary>
    /// <param name="plaintext">The plaintext data to be protected.</param>
    /// <param name="entropy">Additional entropy data that can be used to increase the protection's resilience.</param>
    /// <returns>A byte array containing the protected data.</returns>
    byte[] Protect(byte[] plaintext, byte[] entropy);

    /// <summary>
    ///     Removes the protection applied to the specified data, optionally using the provided entropy to restore the original
    ///     plaintext.
    /// </summary>
    /// <param name="protectedData">The data that has been protected and needs to be restored to its original state.</param>
    /// <param name="entropy">
    ///     Additional entropy data that was used during the protection process. This must match the entropy
    ///     used to protect the data.
    /// </param>
    /// <returns>A byte array containing the original plaintext data.</returns>
    byte[] Unprotect(byte[] protectedData, byte[] entropy);
}
