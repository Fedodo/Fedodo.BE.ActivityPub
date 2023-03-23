namespace Fedodo.BE.ActivityPub.Model.Helpers;

public class SharedInbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Uri SharedInboxUri { get; set; }
}