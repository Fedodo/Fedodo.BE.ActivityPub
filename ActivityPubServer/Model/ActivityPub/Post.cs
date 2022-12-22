using ActivityPubServer.Interfaces;

namespace ActivityPubServer.Model.ActivityPub;

public class Post : IActivityChild
{
    public string? To { get; set; }
    public string Name { get; set; }
    public string? Content { get; set; }
    public Uri? Id { get; set; }
    public string? Type { get; set; }
    public DateTime Published { get; set; }
    public Uri? AttributedTo { get; set; }
    public string Summary { get; set; }
    public bool Sensitive { get; set; }
}