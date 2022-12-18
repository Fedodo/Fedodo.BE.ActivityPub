using System.ComponentModel.DataAnnotations;

namespace ActivityPubServer.Model.DTOs;

public class CreateActorDto
{
    [Required] public string? Type { get; set; }

    [Required] public string? Name { get; set; }

    public string? PreferredUsername { get; set; }
    public string? Summary { get; set; }

    [Required] public string? Password { get; set; }
}