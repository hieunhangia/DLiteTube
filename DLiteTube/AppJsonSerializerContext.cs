using System.Text.Json.Serialization;
using DLiteTube.Models;

namespace DLiteTube;

[JsonSerializable(typeof(Setting))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;