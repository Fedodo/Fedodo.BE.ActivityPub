using System.Text.Json.Serialization;

namespace Fedodo.Server.Model.ActivityPub;

public class OrderedPagedCollection
{
    [JsonPropertyName("@context")] public string Context { get; set; } = "https://www.w3.org/ns/activitystreams";
    [JsonPropertyName("id")] public Uri Id { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = "OrderedCollection";
    [JsonPropertyName("totalItems")] public long TotalItems { get; set; }
    [JsonPropertyName("first")] public Uri First { get; set; }
    [JsonPropertyName("last")] public Uri Last { get; set; }
}