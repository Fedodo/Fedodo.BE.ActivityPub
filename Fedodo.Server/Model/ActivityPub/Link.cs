using System.Text.Json.Serialization;

namespace Fedodo.Server.Model.ActivityPub;

public class Link
{
    [JsonPropertyName("@context")] public string Context { get; set; } = "https://www.w3.org/ns/activitystreams";
    [JsonPropertyName("type")] public string Type { get; set; } = "Link";
    [JsonPropertyName("href")] public Uri Href { get; set; }
    [JsonPropertyName("hreflang")] public string? Hreflang { get; set; }
    [JsonPropertyName("mediaType")] public string? MediaType { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}