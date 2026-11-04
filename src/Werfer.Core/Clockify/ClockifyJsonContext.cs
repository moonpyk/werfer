using System.Text.Json;
using System.Text.Json.Serialization;
using Werfer.Clockify.Models;
using Werfer.Clockify.Requests;
using Werfer.Clockify.ViewModels;

namespace Werfer.Clockify;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
// Raw models
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(List<Workspace>))]
[JsonSerializable(typeof(Client))]
[JsonSerializable(typeof(List<Client>))]
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(List<Project>))]
[JsonSerializable(typeof(TimeEntry))]
[JsonSerializable(typeof(List<TimeEntry>))]
[JsonSerializable(typeof(CreateTimeEntryRequest))]
[JsonSerializable(typeof(StopTimeEntryRequest))]
// ViewModels
[JsonSerializable(typeof(ProjectTaskChoice))]
[JsonSerializable(typeof(List<ProjectTaskChoice>))]
// Runtime types accepted in CustomFieldEntryRequest.Value (declared as object), others throw NotSupportedException
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(List<string>))]
internal partial class ClockifyJsonContext : JsonSerializerContext;
