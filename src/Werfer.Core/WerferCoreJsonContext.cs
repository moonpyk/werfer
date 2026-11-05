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
// Runtime types accepted in CustomFieldEntryRequest.Value (declared as object), others throw NotSupportedException
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(List<string>))]
internal partial class WerferCoreJsonContext : JsonSerializerContext;
