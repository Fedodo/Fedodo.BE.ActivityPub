using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace Fedodo.Server.Model;

public class Webfinger
{
    [JsonIgnore] public ObjectId Id { get; set; } // Needed for storing in MongoDB

    public string? Subject { get; set; }
    public IEnumerable<Link>? Links { get; set; }
}