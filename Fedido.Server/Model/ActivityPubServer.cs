using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace Fedido.Server.Model;

public class ActivityPubServer
{
    [JsonIgnore] public ObjectId Id { get; set; } // Needed for storing in MongoDB
    public string ServerDomainName { get; set; }
    public Uri DefaultInbox { get; set; }
}