using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Werfer.Clockify.Models;

[PublicAPI]
public class TimeEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("billable")]
    public bool Billable { get; set; }

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("taskId")]
    public string? TaskId { get; set; }

    [JsonPropertyName("timeInterval")]
    public TimeInterval? TimeInterval { get; set; }

    [JsonPropertyName("tagIds")]
    public List<string>? TagIds { get; set; }

    /// <summary>
    ///     Werfer helper, not part of Clockify's API: entries with the same key show as one history line. Clockify sends "" or
    ///     null for no description, and both give the same key
    /// </summary>
    [JsonIgnore]
    public (string? ProjectId, string? TaskId, string? Description) WerferGroupingKey =>
    (
        ProjectId,
        TaskId,
        string.IsNullOrEmpty(Description) ? null : Description
    );
}
