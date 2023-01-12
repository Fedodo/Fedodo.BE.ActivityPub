using System.Text.Json.Serialization;

namespace Fedido.Server.Model.DTOs;

public class ReturnApplicationDto
{
    [JsonPropertyName("client_id")] public string ClientId { get; set; }

    [JsonPropertyName("client_secret")] public string? ClientSecret { get; set; }

    [JsonPropertyName("redirect_uri")] public Uri RedirectUri { get; set; }

    [JsonPropertyName("website")] public Uri? Website { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }
}