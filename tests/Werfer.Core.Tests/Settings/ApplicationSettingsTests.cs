using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Werfer.DataProtection;

namespace Werfer.Settings;

public class ApplicationSettingsTests
{
    [Fact]
    public void SaveThenLoad_KeepsTheValues_AndNeverWritesTheKeyInClear()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"werfer-tests-{Guid.NewGuid():N}");

        try
        {
            var saved = Settings(folder);
            saved.ApiKey = "the-api-key";
            saved.MaxProjectChoices = 25;
            saved.DefaultProjectId = "p1";
            saved.DefaultTaskId = "t1";

            Assert.True(saved.TrySave());

            // A new instance, as at the next start: a broken save or load would log the user out silently
            var loaded = Settings(folder).Load();

            Assert.Equal("the-api-key", loaded.ApiKey);
            Assert.Equal(25, loaded.MaxProjectChoices);
            Assert.Equal("p1", loaded.DefaultProjectId);
            Assert.Equal("t1", loaded.DefaultTaskId);
            // Base64 in the file, so decoded: what was written must be the protected bytes, not the key's own
            using var file = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "config.json")));
            Assert.NotEqual(
                Encoding.UTF8.GetBytes("the-api-key"),
                file.RootElement.GetProperty("api_key").GetBytesFromBase64()
            );
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    private static ApplicationSettings Settings(string folder)
    {
        return new ApplicationSettings(folder, new ReversingProvider(), NullLogger<ApplicationSettings>.Instance);
    }

    // Stands in for DPAPI: what reaches the file isn't the plain key, and only Unprotect gets it back
    private sealed class ReversingProvider : IDataProtectionProvider
    {
        public byte[] Protect(byte[] plaintext, byte[] entropy)
        {
            return [.. plaintext.Reverse()];
        }

        public byte[] Unprotect(byte[] protectedData, byte[] entropy)
        {
            return [.. protectedData.Reverse()];
        }
    }
}
