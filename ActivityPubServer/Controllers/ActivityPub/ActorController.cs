using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using CommonExtensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ActivityPubServer.Controllers.ActivityPub;

[Route("Actor")]
public class ActorController : ControllerBase
{
    private readonly ILogger<ActorController> _logger;
    private readonly IMongoDbRepository _repository;

    public ActorController(ILogger<ActorController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet("{actorId:guid}")]
    public async Task<ActionResult<Actor>> GetActor(Guid actorId)
    {
        _logger.LogTrace($"Entered {nameof(GetActor)} in {nameof(ActorController)}");

        var filterDefinitionBuilder = Builders<Actor>.Filter;
        var filter = filterDefinitionBuilder.Eq(i => i.Id,
            new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{actorId}"));

        var actor = await _repository.GetSpecificItem(filter, "ActivityPub", "Actors");

        if (actor.IsNull())
        {
            _logger.LogWarning($"{nameof(actor)} is null");
            return BadRequest($"No actor found for id: {actorId}");
        }

        return Ok(actor);
    }
}