namespace ActivityPubServer.Model;

public class Webfinger
{
    public string? Subject { get; set; }
    public IEnumerable<Link>? Links { get; set; }
}