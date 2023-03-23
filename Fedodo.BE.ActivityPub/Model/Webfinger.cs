using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace Fedodo.BE.ActivityPub.Model;

public class Webfinger
{
    [JsonIgnore] public ObjectId Id { get; set; } // Needed for storing in MongoDB

    public string? Subject { get; set; }
    public IEnumerable<WebLink>? Links { get; set; }
}