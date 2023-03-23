using System.Text.Json.Serialization;

namespace Fedodo.BE.ActivityPub.Model.ActivityPub;

public class Collection<T>
{
    [JsonPropertyName("@context")] public string Context { get; set; } = "https://www.w3.org/ns/activitystreams";
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = "Collection";
    [JsonPropertyName("totalItems")] public int TotalItems => Items.Count();
    [JsonPropertyName("items")] public IEnumerable<T> Items { get; set; }
}