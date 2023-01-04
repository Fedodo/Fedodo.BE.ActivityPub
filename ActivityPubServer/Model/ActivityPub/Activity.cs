using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActivityPubServer.Model.ActivityPub;

public class Activity
{
    [JsonPropertyName("@context")]
    public IEnumerable<object>? Context { get; set; } = new List<object>()
    {
        "https://www.w3.org/ns/activitystreams"
    };

    [JsonPropertyName("id")] public Uri Id { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("actor")] public Uri Actor { get; set; }

    [JsonPropertyName("object")] public object Object { get; set; }

    [JsonPropertyName("to")] public IEnumerable<string>? To { get; set; }

    public T ExtractItemFromObject<T>()
    {
        var jsonElement = (JsonElement)Object;
        var item = jsonElement.Deserialize<T>();

        return item;
    }

    public string ExtractStringFromObject()
    {
        var jsonElement = (JsonElement)Object;
        var rawText = jsonElement.GetRawText();
        var text = rawText.Remove(0, 1).Remove(rawText.Length - 2, 1);

        return text;
    }
}