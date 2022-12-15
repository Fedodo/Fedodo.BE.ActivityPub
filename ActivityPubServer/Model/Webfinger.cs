namespace ActivityPubServer.Model;

public class Webfinger
{
    public Uri? Subject { get; set; }
    public IEnumerable<Link>? Links { get; set; }
}