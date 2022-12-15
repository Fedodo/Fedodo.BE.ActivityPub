using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace ActivityPubServer.Model;

public class Actor
{
    [JsonPropertyName("@context")]
    public IEnumerable<string>? Context { get; set; }

    public Uri? Id { get; set; } // This is the URL of the Actor Document. It is kind of a self reference.
    public string? Type { get; set; }
    public string? PreferredUsername { get; set; }
    public Uri? Inbox { get; set; }
    public PublicKeyAP? PublicKey { get; set; }
}