using System.Text.Json;
using ActivityPubServer.Model.DTOs;
using Newtonsoft.Json;

namespace ActivityPubServer.Model.ActivityPub;

public class Activity
{
    [JsonProperty("@context")] public Uri Context { get; } = new("https://www.w3.org/ns/activitystreams");

    [JsonProperty("id")] public Uri Id { get; set; }

    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("actor")] public Uri Actor { get; set; }

    [JsonProperty("object")] public object Object { get; set; }
    
    [JsonProperty("to")] public string To { get; set; }
    
    public Post ExtractPostFromObject()
    {
        var jsonElement = (JsonElement)Object;
        var post = JsonConvert.DeserializeObject<Post>(jsonElement.GetRawText());

        return post;
    }
}