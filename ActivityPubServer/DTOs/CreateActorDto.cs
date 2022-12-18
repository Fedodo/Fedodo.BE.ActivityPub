namespace ActivityPubServer.DTOs;

public class CreateActorDto
{
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? PreferredUsername { get; set; }
    public string? Summary { get; set; }
}