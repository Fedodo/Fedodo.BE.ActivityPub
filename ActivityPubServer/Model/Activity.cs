using System.Text.Json.Serialization;
using ActivityPubServer.Interfaces;

namespace ActivityPubServer.Model;

public class Activity
{
    [JsonPropertyName("@context")]
    public Uri? Context { get; set; }

    public Uri? Id { get; set; }
    public string? Type { get; set; }
    public Uri? Actor { get; set; }
    public IActivityChild? Object { get; set; }
}