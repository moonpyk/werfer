using System.Text.Json;
using System.Text.Json.Serialization;
using Werfer.Clockify.Models;
using Werfer.Clockify.Requests;

namespace Werfer;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
// Raw models
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(List<Project>))]
[JsonSerializable(typeof(TimeEntry))]
[JsonSerializable(typeof(List<TimeEntry>))]
[JsonSerializable(typeof(CreateTimeEntryRequest))]
[JsonSerializable(typeof(StopTimeEntryRequest))]
internal partial class WerferCoreJsonContext : JsonSerializerContext;
