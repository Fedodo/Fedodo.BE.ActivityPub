namespace ActivityPubServer.Model.DTOs;

public class ReturnApplicationDto
{
    public string ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public Uri RedirectUri { get; set; }

    public Uri? Website { get; set; }

    public string Name { get; set; }
}