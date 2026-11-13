namespace Werfer;

/// <summary>
///     Settings held in memory, nothing saved.
/// </summary>
internal sealed class FakeApplicationSettings : IApplicationSettings
{
    public string? ApiKey { get; set; }
    public int MaxProjectChoices { get; set; } = 10;
    public bool Topmost { get; set; }
    public bool DefaultChoiceEnabled { get; set; }
    public string? DefaultProjectId { get; set; }
    public string? DefaultTaskId { get; set; }
    public bool ConfirmClose { get; set; }
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public DayOfWeek WeekStart { get; set; } = DayOfWeek.Monday;

    public bool TrySave()
    {
        return true;
    }
}
