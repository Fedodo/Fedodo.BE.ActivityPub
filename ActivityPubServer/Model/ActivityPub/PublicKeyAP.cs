namespace ActivityPubServer.Model.ActivityPub;

public class PublicKeyAP
{
    public Uri? Id { get; set; }
    public Uri? Owner { get; set; }
    public string? PublicKeyPem { get; set; }
}