using System.Text.Json;
using CommonExtensions;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;

namespace Fedodo.Server.APIs;

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