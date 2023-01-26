using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Fedodo.Server.Model.DTOs;

public class RegisterClientDto
{
    [Required]
    [JsonPropertyName("client_name")]
    public string ClientName { get; set; }

    [Required]
    [JsonPropertyName("redirect_uris")]
    public Uri RedirectUri { get; set; }

    [JsonPropertyName("website")] public Uri? Website { get; set; }

    [JsonPropertyName("scopes")] public string? Scopes { get; set; }

    [JsonPropertyName("client_uri")] public Uri? ClientUri { get; set; }

    [JsonPropertyName("logo_uri")] public Uri? LogoUri { get; set; }

    [JsonPropertyName("policy_uri")] public Uri? PolicyUri { get; set; }
}