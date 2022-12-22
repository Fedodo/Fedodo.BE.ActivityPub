using System.Text.Json.Serialization;

namespace ActivityPubServer.Model.ActivityPub;

public class OrderedCollection
{
    [JsonPropertyName("@context")] public string Context { get; set; } = "https://www.w3.org/ns/activitystreams";
    public string? Summary { get; set; }
    public string Type { get; set; } = "OrderedCollection";

    public int TotalItems => OrderedItems.Count();
    public IEnumerable<Post> OrderedItems { get; set; }
}