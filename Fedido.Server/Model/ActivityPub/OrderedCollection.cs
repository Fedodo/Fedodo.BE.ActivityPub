using System.Text.Json.Serialization;

namespace Fedido.Server.Model.ActivityPub;

public class OrderedCollection<T>
{
    [JsonPropertyName("@context")] public string Context { get; set; } = "https://www.w3.org/ns/activitystreams";

    [JsonPropertyName("summary")] public string? Summary { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; } = "OrderedCollection";

    [JsonPropertyName("totalItems")] public int TotalItems => OrderedItems.Count();

    [JsonPropertyName("orderedItems")] public IEnumerable<T> OrderedItems { get; set; }
}