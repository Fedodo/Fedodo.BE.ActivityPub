using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Newtonsoft.Json;

namespace ActivityPubServer.Model.DTOs;

public class CreateActivityDto
{
    [JsonProperty("@context")] public Uri Context { get; } = new("https://www.w3.org/ns/activitystreams");

    [Required] [JsonProperty("type")] public string Type { get; set; }

    [Required] [JsonProperty("object")] public object Object { get; set; }

    [JsonProperty("to")] public string To { get; set; }

    public CreatePostDto ExtractCreatePostDtoFromObject()
    {
        var jsonElement = (JsonElement)Object;
        var createPostDto = JsonConvert.DeserializeObject<CreatePostDto>(jsonElement.GetRawText());

        return createPostDto;
    }

    public string ExtractStringFromObject()
    {
        var jsonElement = (JsonElement)Object;
        var rawText = jsonElement.GetRawText();
        var text = rawText.Remove(0, 1).Remove(rawText.Length - 2, 1);

        return text;
    }
}