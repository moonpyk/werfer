using System.Text.Json.Serialization;
using Werfer.Clockify.Models;

namespace Werfer.Clockify.ViewModels;

/// <summary>
///     A project and one of its tasks: what a time entry is started on.
/// </summary>
/// <remarks>
///     The user picks one in the project box.
/// </remarks>
/// <param name="Project">The project to which the task belongs.</param>
/// <param name="Task">The specific task within the project.</param>
public record ProjectTaskChoice(
    [property: JsonPropertyName("p")]
    Project Project,
    [property: JsonPropertyName("t")]
    ProjectTask Task
);
