using CommonExtensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;

namespace Fedido.Server.APIs;

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
        {
            var actor = await httpResponse.Content.ReadFromJsonAsync<Actor>();

            return actor.IsNotNull() ? actor : null;
        }

        var responseText = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogWarning($"An error occured getting an actor: {responseText}");

        return null;
    }
}