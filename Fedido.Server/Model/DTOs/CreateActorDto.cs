using System.ComponentModel.DataAnnotations;

namespace Fedido.Server.Model.DTOs;

public class CreateActorDto
{
    [Required] public string? Type { get; set; }

    public string? Name { get; set; }

    [Required] public string? PreferredUsername { get; set; }

    public string? Summary { get; set; }

    [Required] public string? Password { get; set; }
}