using System.Text.Json.Serialization;
using ActivityPubServer.Interfaces;

namespace ActivityPubServer.Model.ActivityPub;

public class Activity
{
    [JsonPropertyName("@context")] public Uri Context { get;  } = new Uri("https://www.w3.org/ns/activitystreams");

    public Uri? Id { get; set; }
    public string? Type { get; set; }
    public Uri? Actor { get; set; }
    public IActivityChild? Object { get; set; }
}