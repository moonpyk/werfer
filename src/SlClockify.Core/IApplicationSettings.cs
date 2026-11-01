namespace SlClockify.Core;

public interface IApplicationSettings
{
    string? ApiKey { get; set; }
    void Save();
}
