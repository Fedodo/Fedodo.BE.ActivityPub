using System.Text.Json;
using CommonExtensions;
using Fedodo.BE.ActivityPub.Interfaces.APIs;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;

namespace Fedodo.BE.ActivityPub.APIs;

public class ActorApi : IActorAPI
{
    private readonly ILogger<ActorApi> _logger;

    public ActorApi(ILogger<ActorApi> logger)
    {
        _logger = logger;
    }

    public async Task<Actor?> GetActor(Uri actorId)
    {
        HttpClient http = new();
        http.DefaultRequestHeaders.Add("Accept", "application/ld+json");

        var httpResponse = await http.GetAsync(actorId);

        if (httpResponse.IsSuccessStatusCode)
            try
            {
                var actor = await httpResponse.Content.ReadFromJsonAsync<Actor>();

                return actor.IsNotNull() ? actor : null;
            }
            catch (JsonException e)
            {
                _logger.LogWarning("Parsing to actor failed");

                return null;
            }

        var responseText = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogWarning($"An error occured getting an actor: {responseText}");

        return null;
    }
}