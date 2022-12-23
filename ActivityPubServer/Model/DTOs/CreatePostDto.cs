using System.ComponentModel.DataAnnotations;

namespace ActivityPubServer.Model.DTOs;

public class CreatePostDto
{
    [Required]
    public string To { get; set; }
    public string? Name { get; set; }
    public string? Summary { get; set; }
    public bool? Sensitive { get; set; }
    public Uri? InReplyTo { get; set; }
    public string? Content { get; set; }
    [Required]
    public string Type { get; set; }
    [Required]
    public DateTime Published { get; set; }
}