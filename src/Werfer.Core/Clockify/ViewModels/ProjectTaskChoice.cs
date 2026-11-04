using System.Text.Json.Serialization;
using Werfer.Clockify.Models;

namespace Werfer.Clockify.ViewModels;

/// <summary>
///     Represents a pairing of a project and a task within that project.
/// </summary>
/// <remarks>
///     This record is used to encapsulate a project and its associated task as a single unit.
///     It is commonly used in scenarios where both the project and task information are needed
///     together for processing, such as in time tracking or task management systems.
/// </remarks>
/// <param name="Project">The project to which the task belongs.</param>
/// <param name="Task">The specific task within the project.</param>
public record ProjectTaskChoice(
    [property: JsonPropertyName("p")]Project Project,
    [property: JsonPropertyName("t")]ProjectTask Task
);
