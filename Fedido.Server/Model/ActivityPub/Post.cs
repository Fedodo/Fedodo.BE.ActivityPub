using System.Text.Json.Serialization;

namespace Fedido.Server.Model.ActivityPub;

public class Post
{
    [JsonPropertyName("to")] public IEnumerable<string>? To { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("sensitive")] public bool? Sensitive { get; set; }
    [JsonPropertyName("inReplyTo")] public Uri? InReplyTo { get; set; }
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("id")] public Uri Id { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("published")] public DateTime Published { get; set; }
    [JsonPropertyName("attributedTo")] public Uri AttributedTo { get; set; }
}