namespace ActivityPubServer.Interfaces;

public interface IActivityChild
{
    public Uri? Id { get; set; }
    public string? Type { get; set; }
    public DateTime Published { get; set; }
    public Uri? AttributedTo { get; set; }
}