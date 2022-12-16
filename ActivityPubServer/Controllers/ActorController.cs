using ActivityPubServer.Interfaces;
using ActivityPubServer.Model;
using CommonExtensions;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("Actor")]
public class ActorController : ControllerBase
{
    private readonly ILogger<ActorController> _logger;
    private readonly IInMemRepository _repository;

    public ActorController(ILogger<ActorController> logger, IInMemRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    [HttpGet("{actorId}")]
    public ActionResult<Actor> GetActor(Guid actorId)
    {
        _logger.LogTrace($"Entered {nameof(GetActor)} in {nameof(ActorController)}");

        var actor = _repository.GetActor(actorId);

        if (actor.IsNull())
        {
            _logger.LogWarning($"{nameof(actor)} is null");
            return BadRequest($"No actor found for id: {actorId}");
        }
        
        return Ok(actor);
    }
}