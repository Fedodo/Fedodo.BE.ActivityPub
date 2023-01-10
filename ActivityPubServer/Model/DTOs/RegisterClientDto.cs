using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ActivityPubServer.Model.DTOs;

public class RegisterClientDto
{
    [Required]
    [JsonPropertyName("client_name")]
    public string ClientName { get; set; }

    [Required]
    [JsonPropertyName("redirect_uris")]
    public Uri RedirectUri { get; set; }
}