using ActivityPubServer.Interfaces;

namespace ActivityPubServer.Model.ActivityPub;

public class Reply : Post
{
    public Uri? InReplyTo { get; set; }
}