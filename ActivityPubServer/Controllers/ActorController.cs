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
    private readonly IMongoDbRepository _mongoDbRepository;

    public ActorController(ILogger<ActorController> logger, IInMemRepository repository, IMongoDbRepository mongoDbRepository)
    {
        _logger = logger;
        _repository = repository;
        _mongoDbRepository = mongoDbRepository;
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

    [HttpPost]
    public ActionResult AddActor()
    {
        _mongoDbRepository.Create(new Actor(), "test", "testCollection");
        
        return Ok();
    }
}