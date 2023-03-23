using System.Text.Json.Serialization;

namespace Fedodo.BE.ActivityPub.Model.ActivityPub;

public class Activity
{
    [JsonPropertyName("@context")]
    public object? Context { get; set; } = new List<object>
    {
        "https://www.w3.org/ns/activitystreams"
    };

    [JsonPropertyName("id")] public Uri Id { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("actor")] public Uri Actor { get; set; }
    [JsonPropertyName("object")] public object Object { get; set; }
    [JsonPropertyName("published")] public DateTime Published { get; set; }
    [JsonPropertyName("to")] public IEnumerable<string>? To { get; set; }
    [JsonPropertyName("bto")] public IEnumerable<string>? Bto { get; set; }
    [JsonPropertyName("cc")] public IEnumerable<string>? Cc { get; set; }
    [JsonPropertyName("bcc")] public IEnumerable<string>? Bcc { get; set; }
    [JsonPropertyName("audience")] public IEnumerable<string>? Audience { get; set; }
}