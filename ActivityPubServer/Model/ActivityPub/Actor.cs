using System.Drawing;
using System.Text.Json.Serialization;

namespace ActivityPubServer.Model.ActivityPub;

public class Actor
{
    [JsonPropertyName("@context")] public IEnumerable<object>? Context { get; set; }

    public Uri? Id { get; set; } // This is the URL of the Actor Document. It is kind of a self reference.
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? PreferredUsername { get; set; }
    public string? Summary { get; set; }
    public Uri? Inbox { get; set; }
    public Uri? Outbox { get; set; }
    public Uri? Followers { get; set; }
    public Uri? Following { get; set; }
    public IEnumerable<Icon>? Icon { get; set; }
    public PublicKeyAP? PublicKey { get; set; }
}